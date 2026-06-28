namespace ViscaCamLink.Services;

using System.Net.Http;
using System.Reflection;

using Velopack;
using Velopack.Sources;

using AppUpdateInfo = ViscaCamLink.Updater.UpdateInfo;

/// <summary>
/// Update service implementation using Velopack's UpdateManager with GitHub Releases as the source.
/// This is an alternative to the standard <see cref="UpdateService"/> (which uses <see cref="IGitHubUpdateChecker"/>).
/// Velopack offers delta updates, in-process apply, and silent restart — no separate installer download needed.
/// </summary>
public sealed class VelopackUpdateService : IUpdateService
{
    private const string RepoUrl = "https://github.com/misorrek/ViscaCamLink";

    private readonly UpdateManager _updateManager;
    private Velopack.UpdateInfo? _pendingUpdate;

    public VelopackUpdateService()
    {
        _updateManager = new UpdateManager(new GithubSource(RepoUrl, null, false));
    }

    public event EventHandler<AppUpdateInfo>? UpdateAvailable;

    /// <summary>
    /// Whether there is a pending update that has been checked but not yet applied.
    /// </summary>
    public bool HasPendingUpdate => _pendingUpdate is not null;

    public async Task StartAsync(CancellationToken ct = default)
    {
        try
        {
            var newVersion = await _updateManager.CheckForUpdatesAsync().ConfigureAwait(false);

            if (newVersion is null)
                return;

            _pendingUpdate = newVersion;

            var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
            var targetVersion = Version.Parse(newVersion.TargetFullRelease.Version.ToString());

            var info = new AppUpdateInfo(
                Version: targetVersion,
                ReleaseNotes: newVersion.TargetFullRelease.NotesMarkdown ?? string.Empty,
                InstallerAssetUrl: string.Empty,
                PortableAssetUrl: null,
                HtmlUrl: $"{RepoUrl}/releases/tag/v{targetVersion}");

            UpdateAvailable?.Invoke(this, info);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Silent failure — same behaviour as the GitHub API checker
        }
    }

    /// <summary>
    /// Downloads the pending update (delta if available) and applies it, restarting the application.
    /// Call this after <see cref="StartAsync"/> has raised <see cref="UpdateAvailable"/>.
    /// </summary>
    public async Task DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (_pendingUpdate is null)
            throw new InvalidOperationException("No pending update. Call StartAsync first.");

        await _updateManager.DownloadUpdatesAsync(_pendingUpdate, progress: i => progress?.Report(i)).ConfigureAwait(false);
        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }
}
