namespace ViscaCamLink.Services;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class StartupUpdateCheckService(
    IUpdateService updateService,
    TimeSpan? startupDelay = null,
    Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
    Action<Exception>? reportException = null) : IStartupUpdateCheckService
{
    private static readonly TimeSpan DefaultStartupDelay = TimeSpan.FromSeconds(5);

    private readonly TimeSpan _startupDelay = startupDelay ?? DefaultStartupDelay;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync = delayAsync ?? Task.Delay;
    private readonly Action<Exception> _reportException = reportException ?? (exception => Debug.WriteLine($"Startup update check failed: {exception}"));

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _delayAsync(_startupDelay, cancellationToken).ConfigureAwait(false);
            await updateService.CheckForUpdateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            _reportException(exception);
        }
    }
}
