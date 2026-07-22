namespace ViscaCamLink.Tests.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Services;
using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class CameraConnectionServiceTests
{
    private static readonly TimeSpan FastInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(3);

    private readonly Mock<IViscaController> _viscaController = new();
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<IPresetService> _presetService = new();
    private readonly CameraProfile _activeCamera = new() { Name = "Cam", Ip = "192.168.1.100", Port = 5678 };

    public CameraConnectionServiceTests()
    {
        _settings.Setup(s => s.ActiveCameraProfile).Returns(_activeCamera);
    }

    [Fact]
    public void Status_WhenNeverConnected_ReturnsFailed()
    {
        using var service = CreateSut();

        service.Status.ShouldBe(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task ReconnectAsync_Success()
    {
        _viscaController.Setup(v => v.Connected).Returns(true);

        using var service = CreateSut();
        var statuses = new List<ConnectionStatus>();

        service.ConnectionStatusChanged += (_, status) => statuses.Add(status);

        await service.ReconnectAsync();

        statuses.ShouldBe([ConnectionStatus.Working, ConnectionStatus.Ok]);
        service.Status.ShouldBe(ConnectionStatus.Ok);
        _viscaController.Verify(v => v.Reconnect(It.IsAny<CancellationToken>(), "192.168.1.100", 5678), Times.Once);
    }

    [Fact]
    public async Task ReconnectAsync_WhenConnectionFails_SetsStatusToFailed()
    {
        _viscaController.Setup(v => v.Connected).Returns(false);

        using var service = CreateSut();

        await service.ReconnectAsync();

        service.Status.ShouldBe(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task ReconnectAsync_WhenReconnectThrows_SetsStatusToFailed()
    {
        _viscaController.Setup(v => v.Connected).Returns((bool?)null);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("network error"));

        using var service = CreateSut();

        await service.ReconnectAsync();

        service.Status.ShouldBe(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task ReconnectAsync_WhenCalledTwice_RestartsMonitorAndKeepsStatusOk()
    {
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);

        using var service = CreateSut(FastInterval);

        await service.ReconnectAsync();
        await service.ReconnectAsync();

        service.Status.ShouldBe(ConnectionStatus.Ok);
    }

    [Fact]
    public void CommitConnectionSettings_Success()
    {
        using var service = CreateSut();

        service.CommitConnectionSettings("10.0.0.1", 1234);

        _settings.Verify(s => s.UpdateCameraProfile(It.Is<CameraProfile>(p =>
            p.Id == _activeCamera.Id &&
            p.Ip == "10.0.0.1" &&
            p.Port == 1234)), Times.Once);
    }

    [Fact]
    public async Task HealthCheck_Success()
    {
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);

        using var service = CreateSut(FastInterval);

        await service.ReconnectAsync();

        var extraEvents = 0;

        service.ConnectionStatusChanged += (_, _) => extraEvents++;

        await Task.Delay(FastInterval * 4);

        extraEvents.ShouldBe(0);
        service.Status.ShouldBe(ConnectionStatus.Ok);
    }

    [Fact]
    public async Task HealthCheck_WhenProbeFails_SetsStatusToFailed()
    {
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        using var service = CreateSut(FastInterval);

        await service.ReconnectAsync();

        var failedSource = new TaskCompletionSource<ConnectionStatus>();

        service.ConnectionStatusChanged += (_, status) => failedSource.TrySetResult(status);

        var result = await failedSource.Task.WaitAsync(WaitTimeout);

        result.ShouldBe(ConnectionStatus.Failed);
        service.Status.ShouldBe(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task HealthCheck_WhenProbeSucceedsAfterFailure_RestoresStatusToOk()
    {
        var probeCount = 0;

        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (probeCount++ == 0)
                {
                    throw new InvalidOperationException("transient error");
                }

                return PowerStatus.On;
            });

        using var service = CreateSut(FastInterval);

        await service.ReconnectAsync();

        var statusChanges = new List<ConnectionStatus>();
        var recoveredSource = new TaskCompletionSource();

        service.ConnectionStatusChanged += (_, status) =>
        {
            statusChanges.Add(status);

            if (status == ConnectionStatus.Ok && statusChanges.Count > 1)
            {
                recoveredSource.TrySetResult();
            }
        };

        await recoveredSource.Task.WaitAsync(WaitTimeout);

        statusChanges.ShouldBe([ConnectionStatus.Failed, ConnectionStatus.Ok]);
        service.Status.ShouldBe(ConnectionStatus.Ok);
    }

    [Fact]
    public async Task Dispose_Success()
    {
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        var service = CreateSut(FastInterval);

        await service.ReconnectAsync();

        var eventsAfterDispose = 0;

        service.Dispose();

        service.ConnectionStatusChanged += (_, _) => eventsAfterDispose++;

        await Task.Delay(FastInterval * 4);

        eventsAfterDispose.ShouldBe(0);
    }

    private CameraConnectionService CreateSut(TimeSpan? healthCheckInterval = null)
    {
        return new CameraConnectionService(_viscaController.Object, _settings.Object, _presetService.Object, healthCheckInterval);
    }
}
