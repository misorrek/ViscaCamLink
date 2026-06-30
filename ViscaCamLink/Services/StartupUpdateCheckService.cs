namespace ViscaCamLink.Services;

using System.Diagnostics;

public sealed class StartupUpdateCheckService : IStartupUpdateCheckService
{
    private static readonly TimeSpan DefaultStartupDelay = TimeSpan.FromSeconds(5);

    private readonly IUpdateService _updateService;
    private readonly TimeSpan _startupDelay;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private readonly Action<Exception>? _reportException;
    private readonly TimeProvider _timeProvider;

    public StartupUpdateCheckService(IUpdateService updateService)
        : this(
            updateService,
            DefaultStartupDelay,
            Task.Delay,
            exception => Debug.WriteLine($"Startup update check failed: {exception}"),
            TimeProvider.System)
    {
    }

    public StartupUpdateCheckService(
        IUpdateService updateService,
        TimeSpan startupDelay,
        Func<TimeSpan, CancellationToken, Task> delayAsync,
        Action<Exception>? reportException = null,
        TimeProvider? timeProvider = null)
    {
        _updateService = updateService;
        _startupDelay = startupDelay;
        _delayAsync = delayAsync;
        _reportException = reportException;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_timeProvider == TimeProvider.System)
            {
                await _delayAsync(_startupDelay, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await Task.Delay(_startupDelay, _timeProvider, cancellationToken).ConfigureAwait(false);
            }

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