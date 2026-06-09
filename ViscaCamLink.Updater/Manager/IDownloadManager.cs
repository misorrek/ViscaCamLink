namespace ViscaCamLink.Updater.Manager;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IDownloadManager
{
    Task<String?> Download(String downloadUrl, String? downloadPath, IProgress<Double> downloadProgress, CancellationToken cancellationToken);
}
