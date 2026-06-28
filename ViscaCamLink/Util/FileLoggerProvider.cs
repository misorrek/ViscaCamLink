namespace ViscaCamLink.Util;

using System.IO;

using Microsoft.Extensions.Logging;

internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    private string _currentLogPath = string.Empty;
    private StreamWriter? _writer;

    internal FileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(logDirectory);
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, WriteEntry);

    private void WriteEntry(string message)
    {
        lock (_lock)
        {
            EnsureWriter();
            _writer!.WriteLine(message);
        }
    }

    private void EnsureWriter()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var path = Path.Combine(_logDirectory, $"viscacamlink-{today}.log");

        if (path == _currentLogPath)
            return;

        _writer?.Dispose();
        _currentLogPath = path;
        _writer = new StreamWriter(path, append: true, System.Text.Encoding.UTF8) { AutoFlush = true };
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}

internal sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Action<string> _writeEntry;

    internal FileLogger(string categoryName, Action<string> writeEntry)
    {
        _categoryName = categoryName;
        _writeEntry = writeEntry;
    }

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
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
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
        var entry = $"{timestamp} [{level}] {_categoryName}: {message}";

        if (exception != null)
            entry += Environment.NewLine + exception;

        _writeEntry(entry);
    }
}
