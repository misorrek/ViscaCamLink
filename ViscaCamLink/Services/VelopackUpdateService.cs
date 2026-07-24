namespace ViscaCamLink.Services;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Velopack;
using Velopack.Sources;



public class VelopackUpdateService : IUpdateService
{
    private const string RepoUrl = "https://github.com/misorrek/ViscaCamLink";

    private readonly UpdateManager _updateManager = new(new GithubSource(RepoUrl, accessToken: null, prerelease: false));

    private Velopack.UpdateInfo? _pendingUpdate;

    public event EventHandler<UpdateInfo>? UpdateAvailable;

    public bool HasPendingUpdate => _pendingUpdate is not null;

    public async Task CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var newVersion = await _updateManager.CheckForUpdatesAsync().ConfigureAwait(false);

            if (newVersion is null)
            {
                return;
            }

            _pendingUpdate = newVersion;

            var targetVersion = Version.Parse(newVersion.TargetFullRelease.Version.ToString());
            var updateInfo = new UpdateInfo(
                Version: targetVersion,
                ReleaseNotes: newVersion.TargetFullRelease.NotesMarkdown ?? string.Empty,
                InstallerAssetUrl: string.Empty,
                PortableAssetUrl: null,
                HtmlUrl: $"{RepoUrl}/releases/tag/v{targetVersion}");

            UpdateAvailable?.Invoke(this, updateInfo);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // A failed update check must never disturb the running app; the next start checks again.
        }
    }

    /// <summary>
    /// Downloads the pending update (delta if available) and applies it, restarting the application.
    /// Requires a preceding <see cref="CheckForUpdateAsync"/> that raised <see cref="UpdateAvailable"/>.
    /// </summary>
    public async Task DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_pendingUpdate is null)
        {
            throw new InvalidOperationException($"No pending update. Call {nameof(CheckForUpdateAsync)} first.");
        }

        await _updateManager
            .DownloadUpdatesAsync(_pendingUpdate, percent => progress?.Report(percent), cancellationToken)
            .ConfigureAwait(false);

        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }
}
