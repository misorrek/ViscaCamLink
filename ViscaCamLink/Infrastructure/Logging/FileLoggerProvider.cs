namespace ViscaCamLink.Infrastructure.Logging;

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.Extensions.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    public const int RetentionDays = 14;

    private const string LogFilePrefix = "viscacamlink-";
    private const string LogFileExtension = ".log";
    private const string LogFileDateFormat = "yyyy-MM-dd";

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
        var today = _timeProvider.GetLocalNow().Date;
        var fileDate = today.ToString(LogFileDateFormat, CultureInfo.InvariantCulture);
        var path = Path.Combine(_logDirectory, $"{LogFilePrefix}{fileDate}{LogFileExtension}");

        if (path == _currentLogPath)
        {
            return;
        }

        _writer?.Dispose();
        _currentLogPath = path;
        _writer = new StreamWriter(path, append: true, Encoding.UTF8) { AutoFlush = true };

        DeleteExpiredLogFiles(today);
    }

    private void DeleteExpiredLogFiles(DateTime today)
    {
        var oldestRetainedDate = today.AddDays(-(RetentionDays - 1));

        foreach (var filePath in Directory.EnumerateFiles(_logDirectory, $"{LogFilePrefix}*{LogFileExtension}"))
        {
            var datePart = Path.GetFileNameWithoutExtension(filePath)[LogFilePrefix.Length..];
            var isDatedLogFile = DateTime.TryParseExact(
                datePart,
                LogFileDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var logFileDate);

            if (!isDatedLogFile || logFileDate >= oldestRetainedDate)
            {
                continue;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Retention is best effort; a locked old log file is retried on the next day change or start.
            }
        }
    }
}
