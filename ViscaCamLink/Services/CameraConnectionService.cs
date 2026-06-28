namespace ViscaCamLink.Services;

using ViscaCamLink.Util;
using ViscaCamLink.Visca;

public sealed class CameraConnectionService : ICameraConnectionService, IDisposable
{
    /// <summary>How often to probe the camera when connected.</summary>
    private static readonly TimeSpan DefaultHealthCheckInterval = TimeSpan.FromSeconds(5);

    /// <summary>Per-probe network timeout. Must be shorter than <see cref="DefaultHealthCheckInterval"/>.</summary>
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(3);

    private readonly IViscaController _viscaController;
    private readonly ISettingsService _settings;
    private readonly TimeSpan _healthCheckInterval;

    // Replaced atomically in StopMonitoring(); only cancelled/disposed in Dispose().
    private CancellationTokenSource _monitorCts = new();

    public CameraConnectionService(
        IViscaController viscaController,
        ISettingsService settings,
        TimeSpan? healthCheckInterval = null)
    {
        _viscaController = viscaController;
        _settings = settings;
        _healthCheckInterval = healthCheckInterval ?? DefaultHealthCheckInterval;
    }

    public event EventHandler<ConnectionStatus>? ConnectionStatusChanged;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Failed;

    public async Task ReconnectAsync()
    {
        StopMonitoring();

        OnConnectionStatusChanged(ConnectionStatus.Working);

        try
        {
            using var cts = new CancellationTokenSource();
            await _viscaController.Reconnect(cts.Token, _settings.Ip, _settings.Port).ConfigureAwait(false);
        }
        catch
        {
            // Connection failures are reflected via Connected property below
        }

        var connected = _viscaController.Connected.GetValueOrDefault();
        OnConnectionStatusChanged(connected ? ConnectionStatus.Ok : ConnectionStatus.Failed);

        if (connected)
        {
            StartMonitoring();
        }
    }

    public void CommitConnectionSettings(string ip, int port)
    {
        _settings.Ip = ip;
        _settings.Port = port;
    }

    public void Dispose()
    {
        _monitorCts.Cancel();
        _monitorCts.Dispose();
    }

    /// <summary>
    /// Cancels the current monitor and replaces the token source so the next
    /// <see cref="StartMonitoring"/> call gets a fresh, uncancelled token.
    /// </summary>
    private void StopMonitoring()
    {
        var old = _monitorCts;
        _monitorCts = new CancellationTokenSource();
        old.Cancel();
        old.Dispose();
    }

    private void StartMonitoring()
    {
        _ = MonitorConnectionAsync(_monitorCts.Token);
    }

    /// <summary>
    /// Background loop: sends a lightweight VISCA inquiry every
    /// <see cref="_healthCheckInterval"/> to verify the connection is alive.
    /// When the probe fails, status switches to <see cref="ConnectionStatus.Failed"/>;
    /// on the next successful probe (TCP auto-reconnect may help here) it recovers to Ok.
    /// </summary>
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
            // Normal shutdown — monitor stopped by StopMonitoring() or Dispose().
        }
    }

    private async Task CheckConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(HealthCheckTimeout);
            await _viscaController.GetPowerStatus(cts.Token).ConfigureAwait(false);

            // TcpViscaClient may have silently auto-reconnected at the TCP layer; surface the recovery.
            if (Status == ConnectionStatus.Failed)
            {
                OnConnectionStatusChanged(ConnectionStatus.Ok);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Propagate monitor shutdown.
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
