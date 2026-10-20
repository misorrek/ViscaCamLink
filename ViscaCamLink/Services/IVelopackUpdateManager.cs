namespace ViscaCamLink.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

using Velopack;

/// <summary>
/// Thin wrapper around Velopack's <see cref="UpdateManager"/> so <see cref="VelopackUpdateService"/> can be tested.
/// </summary>
public interface IVelopackUpdateManager
{
    bool IsInstalled { get; }

    Version? CurrentVersion { get; }

    VelopackAsset? UpdatePendingRestart { get; }

    Task<Velopack.UpdateInfo?> CheckForUpdatesAsync();

    Task DownloadUpdatesAsync(Velopack.UpdateInfo updateInfo, Action<int> progress, CancellationToken cancellationToken);

    void WaitExitThenApplyUpdates(VelopackAsset toApply);
}
