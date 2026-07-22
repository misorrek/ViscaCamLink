namespace ViscaCamLink.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Services;
using ViscaCamLink.Updater;

using Xunit;

public sealed class UpdateServiceTests
{
    private static readonly Version CurrentVersion = new(1, 2, 3);

    private readonly Mock<IGitHubUpdateChecker> _checker = new();
    private readonly UpdateService _updateService;

    public UpdateServiceTests()
    {
        _updateService = new UpdateService(_checker.Object, CurrentVersion);
    }

    [Fact]
    public async Task StartAsync_Success()
    {
        var updateInfo = new UpdateInfo(new Version(2, 0, 0), "notes", "installer.exe", null, "https://example.test/release");

        _checker
            .Setup(c => c.CheckAsync(CurrentVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateInfo);

        UpdateInfo? received = null;

        _updateService.UpdateAvailable += (_, info) => received = info;

        await _updateService.StartAsync();

        received.ShouldBeSameAs(updateInfo);
    }

    [Fact]
    public async Task StartAsync_WhenNoUpdateIsAvailable_DoesNotRaiseUpdateAvailable()
    {
        _checker
            .Setup(c => c.CheckAsync(CurrentVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateInfo?)null);

        var raised = false;

        _updateService.UpdateAvailable += (_, _) => raised = true;

        await _updateService.StartAsync();

        raised.ShouldBeFalse();
    }
}
