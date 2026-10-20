namespace ViscaCamLink.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IUpdateService
{
    event EventHandler<UpdateInfo>? UpdateAvailable;

    Version? InstalledVersion { get; }

    Task CheckForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the pending update, hands it to the Velopack updater and requests a normal application shutdown.
    /// The updater applies the update once the process has exited and restarts the app.
    /// </summary>
    Task DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}
