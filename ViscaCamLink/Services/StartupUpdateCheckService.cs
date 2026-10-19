namespace ViscaCamLink.Services;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

public partial class StartupUpdateCheckService(
    IUpdateService updateService,
    ILogger<StartupUpdateCheckService> logger,
    TimeSpan? startupDelay = null,
    Func<TimeSpan, CancellationToken, Task>? delayAsync = null) : IStartupUpdateCheckService
{
    private static readonly TimeSpan DefaultStartupDelay = TimeSpan.FromSeconds(5);

    private readonly TimeSpan _startupDelay = startupDelay ?? DefaultStartupDelay;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync = delayAsync ?? Task.Delay;

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
            // A failed startup update check must never disturb the running app; the next start checks again.
            LogStartupUpdateCheckFailed(logger, exception);
        }
    }

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning, Message = "Startup update check failed")]
    private static partial void LogStartupUpdateCheckFailed(ILogger logger, Exception exception);
}
