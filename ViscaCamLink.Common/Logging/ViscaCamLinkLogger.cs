namespace ViscaCamLink.Common.Logging;

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Extensions.Logging;

public class ViscaCamLinkLogger : ILogger
{
    public ViscaCamLinkLogger(String logFilePath)
    {
        _logFilePath = logFilePath;
    }

    private readonly String _logFilePath;

    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public Boolean IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel, 
        EventId eventId, 
        TState state, 
        Exception? exception, 
        Func<TState, Exception?, String> formatter)
    {       
        if (!IsEnabled(logLevel))
        {
            return;
        }

        using (var logStream = File.Open(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
        using (var logStreamWriter = new StreamWriter(logStream))
        {
            logStreamWriter.WriteLine($"[{DateTime.Now}][{logLevel}] {formatter(state, exception)}");
        }
    }
}
