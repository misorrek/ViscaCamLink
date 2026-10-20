namespace ViscaCamLink.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

using Velopack;
using Velopack.Sources;

public class VelopackUpdateManager : IVelopackUpdateManager
{
    public const string RepositoryUrl = "https://github.com/misorrek/ViscaCamLink";

    private readonly UpdateManager _updateManager = new(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));

    public bool IsInstalled => _updateManager.IsInstalled;

    public Version? CurrentVersion => _updateManager.CurrentVersion is { } version
        ? new Version(version.Major, version.Minor, version.Patch)
        : null;

    public VelopackAsset? UpdatePendingRestart => _updateManager.UpdatePendingRestart;

    public Task<Velopack.UpdateInfo?> CheckForUpdatesAsync() => _updateManager.CheckForUpdatesAsync();

    public Task DownloadUpdatesAsync(Velopack.UpdateInfo updateInfo, Action<int> progress, CancellationToken cancellationToken) =>
        _updateManager.DownloadUpdatesAsync(updateInfo, progress, cancellationToken);

    // Not silent, so the updater can show its own progress and prompts (e.g. a missing runtime); restarts the app afterwards.
    public void WaitExitThenApplyUpdates(VelopackAsset toApply) =>
        _updateManager.WaitExitThenApplyUpdates(toApply, silent: false, restart: true);
}
