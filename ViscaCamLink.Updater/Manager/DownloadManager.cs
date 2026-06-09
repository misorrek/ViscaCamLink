namespace ViscaCamLink.Updater.Manager;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ViscaCamLink.Updater.Factories;

public class DownloadManager : IDownloadManager
{    
    public DownloadManager(IApplicationUpdaterClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    private readonly IApplicationUpdaterClientFactory _clientFactory;

    public async Task<String?> Download(String downloadUrl, String? downloadPath, IProgress<Double> downloadProgress, CancellationToken cancellationToken)
    {
        var client = _clientFactory.CreateClient(downloadUrl);
        var tempFilePath = CreateTempFileAndGetPath(downloadPath);
        var downloadFileName = await client.DownloadFileAndGetFileNameAsync(tempFilePath, downloadProgress, cancellationToken);
        
        if (String.IsNullOrEmpty(downloadFileName))
        {
            //TODO Errorhandling
            return null;
        }

        var destinationFilePath = GetDestinationFilePath(downloadFileName, downloadPath);

        if (File.Exists(destinationFilePath))
        {
            File.Delete(destinationFilePath);
        }

        File.Move(tempFilePath, destinationFilePath);

        return destinationFilePath;       
    }

    private String CreateTempFileAndGetPath(String? path)
    {
        String? tempFilePath;

        if (String.IsNullOrEmpty(path))
        {
            tempFilePath = Path.GetRandomFileName();
        }
        else
        {
            tempFilePath = Path.Combine(path, $"{Guid.NewGuid()}.tmp");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        return tempFilePath;
    }

    private String GetDestinationFilePath(String downloadFileName, String? dowloadPath)
    {
        var destinationPath = String.IsNullOrEmpty(dowloadPath) ? Path.GetTempPath() : dowloadPath;

        return Path.Combine(destinationPath, downloadFileName);
    }
}
