namespace ViscaCamLink.Updater.Util;

using System;
using System.Threading;
using System.Threading.Tasks;

using ViscaCamLink.Updater.Common;

public interface IApplicationUpdaterClient
{
    Task<UpdateXml?> GetUpdateXmlAsync(CancellationToken cancellationToken);

    Task<String?> DownloadFileAndGetFileNameAsync(String fileDestinationPath, IProgress<Double> progress, CancellationToken cancellationToken);
}
