namespace ViscaCamLink.Updater.Util;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ViscaCamLink.Updater.Common;
using ViscaCamLink.Updater.Util.Extensions;

public class ApplicationUpdaterClient : IApplicationUpdaterClient
{
    public ApplicationUpdaterClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    private HttpClient HttpClient { get; }

    public async Task<UpdateXml?> GetUpdateXmlAsync(CancellationToken cancellationToken)
    {
        try
        {
            var responseString = await HttpClient.GetStringAsync(String.Empty, cancellationToken);

            var xmlSerializer = new XmlSerializer(typeof(UpdateXml));
            var xmlReader = new XmlTextReader(new StringReader(responseString))
            {
                XmlResolver = null
            };
            var xml = xmlSerializer.Deserialize(xmlReader);

            if (xml != null) 
            { 
                return (UpdateXml)xml;
            }
        }
        catch(HttpRequestException exception)
        {
            //TODO Logging
        }

        return null;
    }

    public async Task<String?> DownloadFileAndGetFileNameAsync(String fileDestinationPath, IProgress<Double> progress, CancellationToken cancellationToken)
    {
        try
        {
            using (var destinationFile = new FileStream(fileDestinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var contentHeaders = await HttpClient.DownloadFileAndGetContentHeaderAsync(String.Empty, destinationFile, progress, cancellationToken);

                return String.IsNullOrEmpty(contentHeaders.ContentDisposition?.FileName)
                    ? Path.GetFileName(HttpClient.BaseAddress?.LocalPath)
                    : contentHeaders.ContentDisposition.FileName;

                //TODO Throw exception if filename is empty?
            }
        }
        catch (HttpRequestException exception) 
        {
            //TODO Logging
        }

        return null;
    }
}
