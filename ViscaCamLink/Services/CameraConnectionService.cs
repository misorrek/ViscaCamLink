namespace ViscaCamLink.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public class CameraConnectionService(
    IViscaController viscaController,
    ISettingsService settingsService,
    IPresetService presetService,
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
        try
        {
            var camera = settingsService.ActiveCameraProfile;
            var ip = camera?.Ip ?? CameraProfile.DefaultIp;
            var port = camera?.Port ?? CameraProfile.DefaultPort;

            await viscaController.ReconnectAsync(ip, port, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Connection failures are reflected via the Connected property below.
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
                OnConnectionStatusChanged(ConnectionStatus.Ok);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Any failed health check means the connection is gone.
            if (Status != ConnectionStatus.Failed)
            {
                OnConnectionStatusChanged(ConnectionStatus.Failed);
            }
        }
    }

    private void OnConnectionStatusChanged(ConnectionStatus status)
    {
        Status = status;
        ConnectionStatusChanged?.Invoke(this, status);
    }
}
