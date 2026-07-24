namespace ViscaCamLink.Infrastructure.Logging;

using System;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.Extensions.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _writeLock = new();

    private string _currentLogPath = string.Empty;
    private StreamWriter? _writer;

    public FileLoggerProvider(string logDirectory, TimeProvider? timeProvider = null)
    {
        _logDirectory = logDirectory;
        _timeProvider = timeProvider ?? TimeProvider.System;

        Directory.CreateDirectory(logDirectory);
    }

    public void Dispose()
    {
        lock (_writeLock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, WriteEntry, _timeProvider);

    private void WriteEntry(string message)
    {
        lock (_writeLock)
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
        {
            return;
        }

        _writer?.Dispose();
        _currentLogPath = path;
        _writer = new StreamWriter(path, append: true, Encoding.UTF8) { AutoFlush = true };
    }
}
