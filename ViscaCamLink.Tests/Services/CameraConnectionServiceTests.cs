namespace ViscaCamLink.Tests.Services;

using FluentAssertions;

using Moq;

using ViscaCamLink.Services;
using ViscaCamLink.Util;
using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public sealed class CameraConnectionServiceTests
{
    private static readonly TimeSpan FastInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(3);

    private readonly Mock<IViscaController> _viscaController = new();
    private readonly Mock<ISettingsService> _settings = new();

    public CameraConnectionServiceTests()
    {
        _settings.Setup(s => s.Ip).Returns("192.168.1.100");
        _settings.Setup(s => s.Port).Returns(5678);
    }

    private CameraConnectionService CreateService(TimeSpan? interval = null) =>
        new(_viscaController.Object, _settings.Object, interval);

    // ── Existing tests ────────────────────────────────────────────────────────

    [Fact]
    public void Status_InitiallyFailed()
    {
        using var svc = CreateService();
        svc.Status.Should().Be(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task ReconnectAsync_WhenConnectionSucceeds_SetsStatusToOk()
    {
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), "192.168.1.100", 5678))
            .Returns(Task.CompletedTask);

        using var svc = CreateService();
        await svc.ReconnectAsync();

        svc.Status.Should().Be(ConnectionStatus.Ok);
    }

    [Fact]
    public async Task ReconnectAsync_WhenConnectionFails_SetsStatusToFailed()
    {
        _viscaController.Setup(v => v.Connected).Returns(false);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        using var svc = CreateService();
        await svc.ReconnectAsync();

        svc.Status.Should().Be(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task ReconnectAsync_WhenReconnectThrows_SetsStatusToFailed()
    {
        _viscaController.Setup(v => v.Connected).Returns((bool?)null);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("network error"));

        using var svc = CreateService();
        await svc.ReconnectAsync();

        svc.Status.Should().Be(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task ReconnectAsync_RaisesConnectionStatusChanged_InOrder()
    {
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        using var svc = CreateService();
        var statuses = new List<ConnectionStatus>();
        svc.ConnectionStatusChanged += (_, s) => statuses.Add(s);

        await svc.ReconnectAsync();

        statuses.Should().Equal(ConnectionStatus.Working, ConnectionStatus.Ok);
    }

    [Fact]
    public async Task ReconnectAsync_UsesSettingsForIpAndPort()
    {
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), "192.168.1.100", 5678))
            .Returns(Task.CompletedTask)
            .Verifiable();

        using var svc = CreateService();
        await svc.ReconnectAsync();

        _viscaController.Verify();
    }

    [Fact]
    public void CommitConnectionSettings_UpdatesSettings()
    {
        using var svc = CreateService();
        svc.CommitConnectionSettings("10.0.0.1", 1234);

        _settings.VerifySet(s => s.Ip = "10.0.0.1");
        _settings.VerifySet(s => s.Port = 1234);
    }

    // ── Health-check tests ────────────────────────────────────────────────────

    [Fact]
    public async Task HealthCheck_WhenProbeFails_SetsStatusToFailed()
    {
        // Arrange: connect successfully, then make GetPowerStatus fail.
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        using var svc = CreateService(FastInterval);
        await svc.ReconnectAsync();

        var failedTcs = new TaskCompletionSource<ConnectionStatus>();
        svc.ConnectionStatusChanged += (_, s) => failedTcs.TrySetResult(s);

        // Act: wait for the health check to fire.
        var result = await failedTcs.Task.WaitAsync(WaitTimeout);

        // Assert
        result.Should().Be(ConnectionStatus.Failed);
        svc.Status.Should().Be(ConnectionStatus.Failed);
    }

    [Fact]
    public async Task HealthCheck_WhenProbeSucceedsAfterFailure_RestoresStatusToOk()
    {
        // Arrange: connect, fail once, then succeed on subsequent probes.
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        int callCount = 0;
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                if (callCount++ == 0) throw new InvalidOperationException("transient error");
                return PowerStatus.On;
            });

        using var svc = CreateService(FastInterval);
        await svc.ReconnectAsync();

        var statusChanges = new List<ConnectionStatus>();
        var recoveredTcs = new TaskCompletionSource();
        svc.ConnectionStatusChanged += (_, s) =>
        {
            statusChanges.Add(s);
            if (s == ConnectionStatus.Ok && statusChanges.Count > 1)
                recoveredTcs.TrySetResult();
        };

        // Act: wait for Failed → Ok recovery.
        await recoveredTcs.Task.WaitAsync(WaitTimeout);

        // Assert
        statusChanges.Should().ContainInOrder(ConnectionStatus.Failed, ConnectionStatus.Ok);
        svc.Status.Should().Be(ConnectionStatus.Ok);
    }

    [Fact]
    public async Task HealthCheck_WhenProbeSucceeds_DoesNotFireStatusChanged()
    {
        // Arrange: probe always succeeds.
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);

        using var svc = CreateService(FastInterval);
        await svc.ReconnectAsync();

        int extraEvents = 0;
        svc.ConnectionStatusChanged += (_, _) => extraEvents++;

        // Act: let several probes fire.
        await Task.Delay(FastInterval * 4);

        // Assert: status remains Ok with no extra events.
        extraEvents.Should().Be(0);
        svc.Status.Should().Be(ConnectionStatus.Ok);
    }

    [Fact]
    public async Task ReconnectAsync_WhenCalledAgain_CancelsExistingMonitor()
    {
        // Arrange: first connect succeeds; probes succeed.
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);

        using var svc = CreateService(FastInterval);
        await svc.ReconnectAsync();

        // Act: reconnect again — should not throw, and status should reach Ok.
        await svc.ReconnectAsync();

        svc.Status.Should().Be(ConnectionStatus.Ok);
    }

    [Fact]
    public async Task Dispose_StopsMonitor_NoFurtherStatusChanges()
    {
        _viscaController.Setup(v => v.Connected).Returns(true);
        _viscaController
            .Setup(v => v.Reconnect(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection lost"));

        var svc = CreateService(FastInterval);
        await svc.ReconnectAsync();

        int eventsAfterDispose = 0;
        svc.Dispose();
        svc.ConnectionStatusChanged += (_, _) => eventsAfterDispose++;

        // Let a potential probe interval pass.
        await Task.Delay(FastInterval * 4);

        eventsAfterDispose.Should().Be(0);
    }
}
