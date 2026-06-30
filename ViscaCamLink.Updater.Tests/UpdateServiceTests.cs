namespace ViscaCamLink.Updater.Tests;

using Shouldly;

using Moq;

using ViscaCamLink.Services;
using ViscaCamLink.Updater;

public sealed class UpdateServiceTests
{
    private static readonly Version CurrentVersion = new(1, 0, 0);
    private static readonly UpdateInfo AvailableUpdate = new(
        Version: new Version(2, 0, 0),
        ReleaseNotes: "New features",
        InstallerAssetUrl: "https://example.com/setup.exe",
        PortableAssetUrl: "https://example.com/portable.zip",
        HtmlUrl: "https://github.com/example/releases/v2.0.0");

    private readonly Mock<IGitHubUpdateChecker> _checker = new();

    [Fact]
    public async Task StartAsync_WhenUpdateAvailable_RaisesUpdateAvailableEvent()
    {
        _checker.Setup(c => c.CheckAsync(CurrentVersion, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AvailableUpdate);

        var service = new UpdateService(_checker.Object, CurrentVersion);
        UpdateInfo? receivedInfo = null;
        service.UpdateAvailable += (_, info) => receivedInfo = info;

        await service.StartAsync();

        receivedInfo.ShouldBe(AvailableUpdate);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyUpToDate_DoesNotRaiseEvent()
    {
        _checker.Setup(c => c.CheckAsync(CurrentVersion, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UpdateInfo?)null);

        var service = new UpdateService(_checker.Object, CurrentVersion);
        var raised = false;
        service.UpdateAvailable += (_, _) => raised = true;

        await service.StartAsync();

        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_WhenCheckerReturnsNull_DoesNotRaiseEvent()
    {
        _checker.Setup(c => c.CheckAsync(It.IsAny<Version>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UpdateInfo?)null);

        var service = new UpdateService(_checker.Object, CurrentVersion);
        var raised = false;
        service.UpdateAvailable += (_, _) => raised = true;

        await service.StartAsync();

        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_PassesCurrentVersionToChecker()
    {
        _checker.Setup(c => c.CheckAsync(CurrentVersion, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UpdateInfo?)null);

        var service = new UpdateService(_checker.Object, CurrentVersion);

        await service.StartAsync();

        _checker.Verify(c => c.CheckAsync(CurrentVersion, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ForwardsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _checker.Setup(c => c.CheckAsync(CurrentVersion, cts.Token))
                .ReturnsAsync((UpdateInfo?)null);

        var service = new UpdateService(_checker.Object, CurrentVersion);

        await service.StartAsync(cts.Token);

        _checker.Verify(c => c.CheckAsync(CurrentVersion, cts.Token), Times.Once);
    }
}
