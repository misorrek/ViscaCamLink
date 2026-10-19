namespace ViscaCamLink.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public partial class CameraConnectionService(
    IViscaController viscaController,
    ISettingsService settingsService,
    IPresetService presetService,
    ILogger<CameraConnectionService> logger,
    TimeSpan? healthCheckInterval = null) : ICameraConnectionService, IDisposable
{
    private static readonly TimeSpan DefaultHealthCheckInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SwitchDelay = TimeSpan.FromSeconds(1);

    private readonly TimeSpan _healthCheckInterval = healthCheckInterval ?? DefaultHealthCheckInterval;

    private CancellationTokenSource _healthCheckCancellation = new();
    private CancellationTokenSource _pendingConnectionCancellation = new();

    public event EventHandler<ConnectionStatus>? ConnectionStatusChanged;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Failed;

    public bool IsSwitchingCameraProfile { get; private set; }

    public void Dispose()
    {
        _healthCheckCancellation.Cancel();
        _healthCheckCancellation.Dispose();
        _pendingConnectionCancellation.Cancel();
        _pendingConnectionCancellation.Dispose();
    }

    public async Task ReconnectAsync()
    {
        var pendingConnection = StartNewPendingConnection();
        var cancellationToken = pendingConnection.Token;

        try
        {
            StopHealthCheck();
            IsSwitchingCameraProfile = true;
            OnConnectionStatusChanged(ConnectionStatus.Working);

            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (_pendingConnectionCancellation == pendingConnection)
            {
                IsSwitchingCameraProfile = false;
            }
        }
    }

    public async Task SwitchCameraProfileAsync(CameraProfile camera)
    {
        var pendingConnection = StartNewPendingConnection();
        var cancellationToken = pendingConnection.Token;

        try
        {
            StopHealthCheck();
            LogSwitchingCameraProfile(logger, camera.Name, camera.Ip, camera.Port);

            settingsService.SetActiveCameraProfile(camera.Id);
            presetService.SwitchCameraProfile(camera.Id);

            await Task.Delay(SwitchDelay, cancellationToken).ConfigureAwait(false);

            IsSwitchingCameraProfile = true;
            OnConnectionStatusChanged(ConnectionStatus.Working);

            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Superseded by a newer switch/reconnect request; don't update the connection status.
        }
        finally
        {
            if (_pendingConnectionCancellation == pendingConnection)
            {
                IsSwitchingCameraProfile = false;
            }
        }
    }

    public void CommitConnectionSettings(string ip, int port)
    {
        var active = settingsService.ActiveCameraProfile;

        if (active is null)
        {
            return;
        }

        var updatedProfile = new CameraProfile
        {
            Id = active.Id,
            Name = active.Name,
            Ip = ip,
            Port = port,
        };

        settingsService.UpdateCameraProfile(updatedProfile);
    }

    private CancellationTokenSource StartNewPendingConnection()
    {
        var oldCancellation = Interlocked.Exchange(ref _pendingConnectionCancellation, new CancellationTokenSource());

        oldCancellation.Cancel();
        oldCancellation.Dispose();

        return _pendingConnectionCancellation;
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var camera = settingsService.ActiveCameraProfile;
        var ip = camera?.Ip ?? CameraProfile.DefaultIp;
        var port = camera?.Port ?? CameraProfile.DefaultPort;

        try
        {
            await viscaController.ReconnectAsync(ip, port, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Connection failures are reflected via the Connected property below; the transport already logged the error.
            LogConnectFailed(logger, ip, port, exception);
        }

        var connected = viscaController.Connected.GetValueOrDefault();

        OnConnectionStatusChanged(connected ? ConnectionStatus.Ok : ConnectionStatus.Failed);

        if (connected)
        {
            StartHealthCheck();
        }
    }

    private void StartHealthCheck()
    {
        _ = MonitorConnectionAsync(_healthCheckCancellation.Token);
    }

    private void StopHealthCheck()
    {
        var oldCancellation = _healthCheckCancellation;
        _healthCheckCancellation = new CancellationTokenSource();

        oldCancellation.Cancel();
        oldCancellation.Dispose();
    }

    private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_healthCheckInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await CheckConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown — the health check was stopped by StopHealthCheck() or Dispose().
        }
    }

    private async Task CheckConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            timeoutCancellation.CancelAfter(HealthCheckTimeout);

            await viscaController.GetPowerStatusAsync(timeoutCancellation.Token).ConfigureAwait(false);

            if (Status == ConnectionStatus.Failed)
            {
                LogHealthCheckRecovered(logger);
                OnConnectionStatusChanged(ConnectionStatus.Ok);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Any failed health check means the connection is gone. Only the transition is logged
            // above Debug so a camera that stays offline does not flood the log.
            if (Status != ConnectionStatus.Failed)
            {
                LogHealthCheckFailed(logger, exception);
                OnConnectionStatusChanged(ConnectionStatus.Failed);
            }
            else
            {
                LogHealthCheckStillFailing(logger, exception.Message);
            }
        }
    }

    private void OnConnectionStatusChanged(ConnectionStatus status)
    {
        if (Status != status)
        {
            LogConnectionStatusChanged(logger, Status, status);
        }

        Status = status;
        ConnectionStatusChanged?.Invoke(this, status);
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Connection status changed from {OldStatus} to {NewStatus}")]
    private static partial void LogConnectionStatusChanged(ILogger logger, ConnectionStatus oldStatus, ConnectionStatus newStatus);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Switching to camera profile '{Name}' ({Ip}:{Port})")]
    private static partial void LogSwitchingCameraProfile(ILogger logger, string name, string ip, int port);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Debug, Message = "Connecting to {Ip}:{Port} failed")]
    private static partial void LogConnectFailed(ILogger logger, string ip, int port, Exception exception);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Warning, Message = "Connection health check failed; camera is unreachable")]
    private static partial void LogHealthCheckFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Debug, Message = "Connection health check still failing: {Reason}")]
    private static partial void LogHealthCheckStillFailing(ILogger logger, string reason);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Connection health check succeeded again; connection recovered")]
    private static partial void LogHealthCheckRecovered(ILogger logger);
}
