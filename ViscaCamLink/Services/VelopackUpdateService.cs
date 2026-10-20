namespace ViscaCamLink.Services;

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

public partial class VelopackUpdateService(
    IVelopackUpdateManager updateManager,
    Action requestShutdown,
    ILogger<VelopackUpdateService> logger) : IUpdateService
{
    private const string ReleaseTagUrlFormat = "{0}/releases/tag/v{1}";

    private Velopack.UpdateInfo? _pendingUpdate;

    public event EventHandler<UpdateInfo>? UpdateAvailable;

    public Version? InstalledVersion => updateManager.CurrentVersion;

    /// <summary>
    /// Checks GitHub for a newer release. Failures propagate to <see cref="StartupUpdateCheckService"/>,
    /// which logs them once as a Warning.
    /// </summary>
    public async Task CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (!updateManager.IsInstalled)
        {
            LogUpdateCheckSkippedNotInstalled(logger);
            return;
        }

        if (updateManager.UpdatePendingRestart is { } pendingRestart)
        {
            // VelopackApp.Run() applies a downloaded update on the next start (SetAutoApplyOnStartup), so only record it.
            LogUpdatePendingRestart(logger, pendingRestart.Version.ToString());
        }

        LogUpdateCheckStarted(logger, updateManager.CurrentVersion);

        cancellationToken.ThrowIfCancellationRequested();

        var newVersion = await updateManager.CheckForUpdatesAsync().ConfigureAwait(false);

        if (newVersion is null)
        {
            LogNoUpdateAvailable(logger);
            return;
        }

        _pendingUpdate = newVersion;

        var release = newVersion.TargetFullRelease;
        var targetVersion = new Version(release.Version.Major, release.Version.Minor, release.Version.Patch);
        var updateInfo = new UpdateInfo(
            Version: targetVersion,
            ReleaseNotes: release.NotesMarkdown ?? string.Empty,
            HtmlUrl: string.Format(CultureInfo.InvariantCulture, ReleaseTagUrlFormat, VelopackUpdateManager.RepositoryUrl, targetVersion));

        LogUpdateAvailable(logger, targetVersion);

        UpdateAvailable?.Invoke(this, updateInfo);
    }

    public async Task DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_pendingUpdate is null)
        {
            throw new InvalidOperationException($"No pending update. Call {nameof(CheckForUpdateAsync)} first.");
        }

        var targetVersion = _pendingUpdate.TargetFullRelease.Version.ToString();

        LogDownloadStarted(logger, targetVersion);

        try
        {
            await updateManager
                .DownloadUpdatesAsync(_pendingUpdate, percent => progress?.Report(percent), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LogDownloadCancelled(logger, targetVersion);
            throw;
        }

        LogApplyingUpdate(logger, targetVersion);

        // WaitExitThenApplyUpdates instead of ApplyUpdatesAndRestart: the app shuts down normally, so App.OnClosed
        // persists settings and window placement and disposes services before the updater replaces the files.
        updateManager.WaitExitThenApplyUpdates(_pendingUpdate.TargetFullRelease);
        requestShutdown();
    }

    [LoggerMessage(EventId = 4010, Level = LogLevel.Debug, Message = "Update check skipped: the app is not installed by Velopack (debug or unpackaged run)")]
    private static partial void LogUpdateCheckSkippedNotInstalled(ILogger logger);

    [LoggerMessage(EventId = 4011, Level = LogLevel.Information, Message = "Checking for updates (installed version {InstalledVersion})")]
    private static partial void LogUpdateCheckStarted(ILogger logger, Version? installedVersion);

    [LoggerMessage(EventId = 4012, Level = LogLevel.Information, Message = "No update available")]
    private static partial void LogNoUpdateAvailable(ILogger logger);

    [LoggerMessage(EventId = 4013, Level = LogLevel.Information, Message = "Update {Version} available")]
    private static partial void LogUpdateAvailable(ILogger logger, Version version);

    [LoggerMessage(EventId = 4014, Level = LogLevel.Information, Message = "Downloaded update {Version} is pending and will be applied on the next start")]
    private static partial void LogUpdatePendingRestart(ILogger logger, string version);

    [LoggerMessage(EventId = 4020, Level = LogLevel.Information, Message = "Downloading update {Version}")]
    private static partial void LogDownloadStarted(ILogger logger, string version);

    [LoggerMessage(EventId = 4021, Level = LogLevel.Information, Message = "Download of update {Version} cancelled")]
    private static partial void LogDownloadCancelled(ILogger logger, string version);

    [LoggerMessage(EventId = 4030, Level = LogLevel.Information, Message = "Applying update {Version}: the updater waits for the app to exit, shutting down")]
    private static partial void LogApplyingUpdate(ILogger logger, string version);
}
