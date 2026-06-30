namespace ViscaCamLink.Util;

using System.IO;
using System.Threading;

using Microsoft.Extensions.Logging;

internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly Lock _lock = new();
    private readonly TimeProvider _timeProvider;

    private string _currentLogPath = string.Empty;
    private StreamWriter? _writer;

    internal FileLoggerProvider(string logDirectory, TimeProvider? timeProvider = null)
    {
        _logDirectory = logDirectory;
        _timeProvider = timeProvider ?? TimeProvider.System;
        Directory.CreateDirectory(logDirectory);
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, WriteEntry, _timeProvider);

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
        var today = _timeProvider.GetLocalNow().ToString("yyyy-MM-dd");
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
    private readonly TimeProvider _timeProvider;

    internal FileLogger(string categoryName, Action<string> writeEntry, TimeProvider timeProvider)
    {
        _categoryName = categoryName;
        _writeEntry = writeEntry;
        _timeProvider = timeProvider;
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

        var timestamp = _timeProvider.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss.fff");
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
