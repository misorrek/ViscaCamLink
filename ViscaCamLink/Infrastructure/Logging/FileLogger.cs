namespace ViscaCamLink.Infrastructure.Logging;

using Microsoft.Extensions.Logging;

public sealed class FileLogger(string categoryName, Action<string> writeEntry, TimeProvider timeProvider) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var timestamp = timeProvider.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss.fff");
        var level = logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };

        var message = formatter(state, exception);
        var entry = $"{timestamp} [{level}] {categoryName}: {message}";

        if (exception != null)
        {
            entry += Environment.NewLine + exception;
        }

        writeEntry(entry);
    }
}
