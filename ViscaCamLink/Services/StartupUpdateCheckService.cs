namespace ViscaCamLink.Services;

using System.Diagnostics;

public sealed class StartupUpdateCheckService : IStartupUpdateCheckService
{
    private static readonly TimeSpan DefaultStartupDelay = TimeSpan.FromSeconds(5);

    private readonly IUpdateService _updateService;
    private readonly TimeSpan _startupDelay;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly Action<Exception>? _reportException;

    public StartupUpdateCheckService(IUpdateService updateService)
        : this(
            updateService,
            DefaultStartupDelay,
            Task.Delay,
            exception => Debug.WriteLine($"Startup update check failed: {exception}"))
    {
    }

    public StartupUpdateCheckService(
        IUpdateService updateService,
        TimeSpan startupDelay,
        Func<TimeSpan, CancellationToken, Task> delayAsync,
        Action<Exception>? reportException = null)
    {
        _updateService = updateService;
        _startupDelay = startupDelay;
        _delayAsync = delayAsync;
        _reportException = reportException;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _delayAsync(_startupDelay, cancellationToken).ConfigureAwait(false);
            await _updateService.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception ex)
        {
            _reportException?.Invoke(ex);
        }
    }
}