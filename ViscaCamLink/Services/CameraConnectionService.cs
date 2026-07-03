namespace ViscaCamLink.Services;

using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public sealed class CameraConnectionService(
    IViscaController viscaController,
    ISettingsService settingsService,
    IPresetService presetService,
    TimeSpan? healthCheckInterval = null) : ICameraConnectionService, IDisposable
{
    private static readonly TimeSpan DefaultHealthCheckInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(3);

    private readonly TimeSpan _healthCheckInterval = healthCheckInterval ?? DefaultHealthCheckInterval;

    private CancellationTokenSource _healthCheckCts = new();

    public event EventHandler<ConnectionStatus>? ConnectionStatusChanged;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Failed;

    public bool IsSwitchingCameraProfile { get; private set; }

    public void Dispose()
    {
        _healthCheckCts.Cancel();
        _healthCheckCts.Dispose();
    }

    public async Task ReconnectAsync()
    {
        StopHealthCheck();
        OnConnectionStatusChanged(ConnectionStatus.Working);

        await Connect();
    }

    public async Task SwitchCameraProfileAsync(CameraProfile camera)
    {
        IsSwitchingCameraProfile = true;

        try
        {
            StopHealthCheck();
            OnConnectionStatusChanged(ConnectionStatus.Working);

            settingsService.SetActiveCameraProfile(camera.Id);
            presetService.SwitchCameraProfile(camera.Id);

            await Connect();
        }
        finally
        {
            IsSwitchingCameraProfile = false;
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

    private async Task Connect()
    {
        try
        {
            var camera = settingsService.ActiveCameraProfile;
            var ip = camera?.Ip ?? CameraProfile.DefaultIp;
            var port = camera?.Port ?? CameraProfile.DefaultPort;

            using var cts = new CancellationTokenSource();

            await viscaController.Reconnect(cts.Token, ip, port).ConfigureAwait(false);
        }
        catch
        {
            // Connection failures are reflected via Connected property below
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
        _ = MonitorConnectionAsync(_healthCheckCts.Token);
    }

    private void StopHealthCheck()
    {
        var oldCts = _healthCheckCts;
        _healthCheckCts = new CancellationTokenSource();

        oldCts.Cancel();
        oldCts.Dispose();
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
            // Normal shutdown — health check stopped by StopMonitoring() or Dispose().
        }
    }

    private async Task CheckConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            cts.CancelAfter(HealthCheckTimeout);

            await viscaController.GetPowerStatus(cts.Token).ConfigureAwait(false);

            if (Status == ConnectionStatus.Failed)
            {
                OnConnectionStatusChanged(ConnectionStatus.Ok);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Propagate health check shutdown.
        }
        catch
        {
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
