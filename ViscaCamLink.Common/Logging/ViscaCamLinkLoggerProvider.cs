namespace ViscaCamLink.Common.Logging;

using System;
using System.Collections.Concurrent;
using System.IO;

using Microsoft.Extensions.Logging;

[ProviderAlias("ViscaCamLink")]
public sealed class ViscaCamLinkLoggerProvider : ILoggerProvider
{
    public ViscaCamLinkLoggerProvider()
    {
        _loggers = new ConcurrentDictionary<string, ViscaCamLinkLogger>(StringComparer.OrdinalIgnoreCase);
        _logFilesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ViscaCamLink", "Logs"); //TODO dynamisch in config ordner?

        Directory.CreateDirectory(_logFilesDirectory);
    }

    private readonly ConcurrentDictionary<String, ViscaCamLinkLogger> _loggers;

    private readonly String _logFilesDirectory;

    public ILogger CreateLogger(String categoryName)
    {
        return _loggers.GetOrAdd(categoryName, new ViscaCamLinkLogger(Path.Combine(_logFilesDirectory, $"{categoryName}.log")));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
