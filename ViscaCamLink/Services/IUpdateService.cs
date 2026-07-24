namespace ViscaCamLink.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IUpdateService
{
    event EventHandler<UpdateInfo>? UpdateAvailable;

    Task CheckForUpdateAsync(CancellationToken cancellationToken = default);

    Task DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}
