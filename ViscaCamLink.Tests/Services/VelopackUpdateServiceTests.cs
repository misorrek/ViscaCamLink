namespace ViscaCamLink.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shouldly;

using Velopack;

using ViscaCamLink.Services;

using Xunit;

public sealed class VelopackUpdateServiceTests
{
    private static readonly VelopackAsset TargetRelease = new()
    {
        Version = SemanticVersion.Parse("2.1.0"),
        NotesMarkdown = "release notes",
    };

    private static readonly Velopack.UpdateInfo AvailableUpdate = new(TargetRelease, false, null!, []);

    private readonly Mock<IVelopackUpdateManager> _updateManager = new();
    private readonly VelopackUpdateService _service;

    private int _shutdownRequests;

    public VelopackUpdateServiceTests()
    {
        _updateManager.SetupGet(manager => manager.IsInstalled).Returns(true);
        _updateManager.SetupGet(manager => manager.CurrentVersion).Returns(new Version(1, 0, 0));

        _service = new VelopackUpdateService(
            _updateManager.Object,
            () => _shutdownRequests++,
            NullLogger<VelopackUpdateService>.Instance);
    }

    [Fact]
    public void InstalledVersion_Success()
    {
        _service.InstalledVersion.ShouldBe(new Version(1, 0, 0));
    }

    [Fact]
    public async Task CheckForUpdateAsync_Success()
    {
        ViscaCamLink.Services.UpdateInfo? raisedUpdate = null;

        _updateManager.Setup(manager => manager.CheckForUpdatesAsync()).ReturnsAsync(AvailableUpdate);
        _service.UpdateAvailable += (_, updateInfo) => raisedUpdate = updateInfo;

        await _service.CheckForUpdateAsync();

        raisedUpdate.ShouldNotBeNull();
        raisedUpdate.Version.ShouldBe(new Version(2, 1, 0));
        raisedUpdate.ReleaseNotes.ShouldBe("release notes");
        raisedUpdate.HtmlUrl.ShouldBe($"{VelopackUpdateManager.RepositoryUrl}/releases/tag/v2.1.0");
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenNoUpdateAvailable_DoesNotRaiseUpdateAvailable()
    {
        var raised = false;

        _updateManager.Setup(manager => manager.CheckForUpdatesAsync()).ReturnsAsync((Velopack.UpdateInfo?)null);
        _service.UpdateAvailable += (_, _) => raised = true;

        await _service.CheckForUpdateAsync();

        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenNotInstalled_SkipsCheck()
    {
        _updateManager.SetupGet(manager => manager.IsInstalled).Returns(false);

        await _service.CheckForUpdateAsync();

        _updateManager.Verify(manager => manager.CheckForUpdatesAsync(), Times.Never);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_Success()
    {
        _updateManager.Setup(manager => manager.CheckForUpdatesAsync()).ReturnsAsync(AvailableUpdate);

        await _service.CheckForUpdateAsync();
        await _service.DownloadAndApplyAsync();

        _updateManager.Verify(
            manager => manager.DownloadUpdatesAsync(AvailableUpdate, It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _updateManager.Verify(manager => manager.WaitExitThenApplyUpdates(TargetRelease), Times.Once);
        _shutdownRequests.ShouldBe(1);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_WhenNoPendingUpdate_Throws()
    {
        var act = () => _service.DownloadAndApplyAsync();

        await Should.ThrowAsync<InvalidOperationException>(act);
        _shutdownRequests.ShouldBe(0);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_WhenProgressReported_ForwardsProgress()
    {
        var progress = new Mock<IProgress<int>>();

        _updateManager.Setup(manager => manager.CheckForUpdatesAsync()).ReturnsAsync(AvailableUpdate);
        _updateManager
            .Setup(manager => manager.DownloadUpdatesAsync(AvailableUpdate, It.IsAny<Action<int>>(), It.IsAny<CancellationToken>()))
            .Callback<Velopack.UpdateInfo, Action<int>, CancellationToken>((_, reportProgress, _) => reportProgress(42))
            .Returns(Task.CompletedTask);

        await _service.CheckForUpdateAsync();
        await _service.DownloadAndApplyAsync(progress.Object);

        progress.Verify(reporter => reporter.Report(42), Times.Once);
    }

    [Fact]
    public async Task DownloadAndApplyAsync_WhenCancelled_ForwardsTokenAndDoesNotApply()
    {
        using var cancellation = new CancellationTokenSource();

        _updateManager.Setup(manager => manager.CheckForUpdatesAsync()).ReturnsAsync(AvailableUpdate);
        _updateManager
            .Setup(manager => manager.DownloadUpdatesAsync(AvailableUpdate, It.IsAny<Action<int>>(), cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        await _service.CheckForUpdateAsync();
        await cancellation.CancelAsync();

        var act = () => _service.DownloadAndApplyAsync(null, cancellation.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
        _updateManager.Verify(manager => manager.WaitExitThenApplyUpdates(It.IsAny<VelopackAsset>()), Times.Never);
        _shutdownRequests.ShouldBe(0);
    }
}
