namespace ViscaCamLink.ViewModels;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

public abstract partial class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected static async Task RunSafeAsync(Func<Task> operation, ILogger logger, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            LogAsyncOperationFailed(logger, operationName, exception);
        }
    }

    /// <summary>
    /// Awaits a camera operation task, swallowing any exceptions.
    /// Camera failures are best-effort; persistent failures surface through connection status events.
    /// </summary>
    protected static async Task<bool> TryCameraOperation(Task operation)
    {
        try
        {
            await operation;
            return true;
        }
        catch (Exception)
        {
            // Intentionally swallowed — camera operations are best-effort.
            // Persistent failures are surfaced via ConnectionStatusChanged events.
            // Not logged here to avoid double logging: ViscaController logs every failed command at Debug,
            // and services log user actions (power, presets) with context at Error before rethrowing.
            return false;
        }
    }

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [LoggerMessage(EventId = 2901, Level = LogLevel.Error, Message = "Async command or handler {Operation} failed")]
    private static partial void LogAsyncOperationFailed(ILogger logger, string operation, Exception exception);
}
