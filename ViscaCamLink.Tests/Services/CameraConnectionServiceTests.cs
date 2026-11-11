namespace ViscaCamLink.Tests.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

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
        _viscaController.Verify(v => v.ReconnectAsync("192.168.1.100", 5678, It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(v => v.ReconnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network error"));

        using var service = CreateSut();

        await service.ReconnectAsync();

        service.Status.ShouldBe(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task ReconnectAsync_WhenCalledTwice_RestartsMonitorAndKeepsStatusOk()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        var probeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                probeStarted.TrySetResult();
                return Task.FromResult(PowerStatus.On);
            });

        using var service = CreateSut(fakeTimeProvider, FastInterval);

        await service.ReconnectAsync();
        await service.ReconnectAsync();

        fakeTimeProvider.Advance(FastInterval);
        await probeStarted.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);

        service.Status.ShouldBe(ConnectionStatus.Ok);
        _viscaController.Verify(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReconnectAsync_WhenSuperseded_DoesNotThrow()
    {
        var firstAttemptStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reconnectCount = 0;

        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.ReconnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((string _, int _, CancellationToken cancellationToken) =>
            {
                if (Interlocked.Increment(ref reconnectCount) == 1)
                {
                    firstAttemptStarted.TrySetResult();

                    return Task.Delay(Timeout.Infinite, cancellationToken);
                }

                return Task.CompletedTask;
            });

        using var service = CreateSut();
        var firstReconnect = service.ReconnectAsync();

        await firstAttemptStarted.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
        await service.ReconnectAsync();
        await Should.NotThrowAsync(() => firstReconnect);

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
        var fakeTimeProvider = new FakeTimeProvider();
        var probeCount = 0;
        var probeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                probeCount++;
                probeStarted.TrySetResult();
                return Task.FromResult(PowerStatus.On);
            });

        using var service = CreateSut(fakeTimeProvider, FastInterval);

        await service.ReconnectAsync();

        var extraEvents = 0;

        service.ConnectionStatusChanged += (_, _) => extraEvents++;

        for (var index = 0; index < 4; index++)
        {
            var probeCompleted = probeStarted.Task;
            fakeTimeProvider.Advance(FastInterval);
            await probeCompleted.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
            probeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        extraEvents.ShouldBe(0);
        probeCount.ShouldBe(4);
        service.Status.ShouldBe(ConnectionStatus.Ok);
    }

    [Fact]
    public async Task HealthCheck_WhenProbeFails_SetsStatusToFailed()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        var probeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                probeStarted.TrySetResult();
                return Task.FromException<PowerStatus>(new InvalidOperationException("connection lost"));
            });

        using var service = CreateSut(fakeTimeProvider, FastInterval);

        await service.ReconnectAsync();

        var failedSource = new TaskCompletionSource<ConnectionStatus>();

        service.ConnectionStatusChanged += (_, status) => failedSource.TrySetResult(status);

        fakeTimeProvider.Advance(FastInterval);
        await probeStarted.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);

        var result = await failedSource.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);

        result.ShouldBe(ConnectionStatus.Failed);
        service.Status.ShouldBe(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task HealthCheck_WhenProbeSucceedsAfterFailure_RestoresStatusToOk()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        var probeCount = 0;
        var probeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                probeStarted.TrySetResult();

                if (probeCount++ == 0)
                {
                    return Task.FromException<PowerStatus>(new InvalidOperationException("transient error"));
                }

                return Task.FromResult(PowerStatus.On);
            });

        using var service = CreateSut(fakeTimeProvider, FastInterval);

        await service.ReconnectAsync();

        var statusChanges = new List<ConnectionStatus>();
        var failedSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recoveredSource = new TaskCompletionSource();

        service.ConnectionStatusChanged += (_, status) =>
        {
            statusChanges.Add(status);

            if (status == ConnectionStatus.Failed)
            {
                failedSource.TrySetResult();
            }

            if (status == ConnectionStatus.Ok && statusChanges.Count > 1)
            {
                recoveredSource.TrySetResult();
            }
        };

        fakeTimeProvider.Advance(FastInterval);
        await probeStarted.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
        await failedSource.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);

        probeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fakeTimeProvider.Advance(FastInterval);
        await probeStarted.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
        await recoveredSource.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);

        statusChanges.ShouldBe([ConnectionStatus.Failed, ConnectionStatus.Ok]);
        service.Status.ShouldBe(ConnectionStatus.Ok);
    }

    [Fact]
    public async Task Dispose_Success()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        var probeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var probeCount = 0;

        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                probeCount++;
                probeStarted.TrySetResult();
                return Task.FromException<PowerStatus>(new InvalidOperationException("connection lost"));
            });

        var service = CreateSut(fakeTimeProvider, FastInterval);

        await service.ReconnectAsync();

        fakeTimeProvider.Advance(FastInterval);
        await probeStarted.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);

        service.Dispose();
        fakeTimeProvider.Advance(FastInterval * 4);

        probeCount.ShouldBe(1);
    }

    [Fact]
    public async Task SwitchCameraProfileAsync_WhenSuperseded_ConnectsOnlyTheLatestProfile()
    {
        var fakeTimeProvider = new FakeTimeProvider();
        var firstCamera = new CameraProfile { Name = "First", Ip = "192.168.1.101", Port = 5678 };
        var latestCamera = new CameraProfile { Name = "Latest", Ip = "192.168.1.102", Port = 5678 };
        var activeCamera = _activeCamera;
        var connectStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _settings.Setup(s => s.ActiveCameraProfile).Returns(() => activeCamera);
        _settings
            .Setup(s => s.SetActiveCameraProfile(It.IsAny<Guid>()))
            .Callback<Guid>(profileId => activeCamera = profileId == firstCamera.Id ? firstCamera : latestCamera);
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.ReconnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                connectStarted.TrySetResult();
                return Task.CompletedTask;
            });

        using var service = CreateSut(fakeTimeProvider);
        var firstSwitch = service.SwitchCameraProfileAsync(firstCamera);
        var latestSwitch = service.SwitchCameraProfileAsync(latestCamera);

        await firstSwitch.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
        fakeTimeProvider.Advance(TimeSpan.FromSeconds(1));
        await connectStarted.Task.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);
        await latestSwitch.WaitAsync(WaitTimeout, TestContext.Current.CancellationToken);

        _viscaController.Verify(
            v => v.ReconnectAsync(latestCamera.Ip, latestCamera.Port, It.IsAny<CancellationToken>()),
            Times.Once);
        _viscaController.Verify(
            v => v.ReconnectAsync(firstCamera.Ip, firstCamera.Port, It.IsAny<CancellationToken>()),
            Times.Never);
        service.Status.ShouldBe(ConnectionStatus.Ok);
        service.IsSwitchingCameraProfile.ShouldBeFalse();
    }

    private CameraConnectionService CreateSut(TimeProvider? timeProvider = null, TimeSpan? healthCheckInterval = null)
    {
        return new CameraConnectionService(
            _viscaController.Object,
            _settings.Object,
            _presetService.Object,
            NullLogger<CameraConnectionService>.Instance,
            healthCheckInterval,
            timeProvider);
    }
}
