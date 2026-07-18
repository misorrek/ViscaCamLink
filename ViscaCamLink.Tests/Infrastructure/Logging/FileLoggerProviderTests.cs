namespace ViscaCamLink.Tests.Infrastructure.Logging;

using System;
using System.IO;

using Microsoft.Extensions.Logging;

using Shouldly;

using ViscaCamLink.Infrastructure.Logging;

using Xunit;

public sealed class FileLoggerProviderTests : IDisposable
{
    private readonly string _logDirectory = Path.Combine(Path.GetTempPath(), $"viscacamlink_logs_{Guid.NewGuid():N}");
    private readonly MutableTimeProvider _timeProvider = new(new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero));
    private readonly FileLoggerProvider _provider;

    public FileLoggerProviderTests()
    {
        _provider = new FileLoggerProvider(_logDirectory, _timeProvider);
    }

    public void Dispose()
    {
        _provider.Dispose();

        if (Directory.Exists(_logDirectory))
        {
            Directory.Delete(_logDirectory, recursive: true);
        }
    }

    [Fact]
    public void CreateLogger_Success()
    {
        var logger = _provider.CreateLogger("TestCategory");

        logger.Log(LogLevel.Information, new EventId(1), "Hello", null, (state, _) => state);

        _provider.Dispose();

        var logFilePath = Path.Combine(_logDirectory, "viscacamlink-2026-01-02.log");

        File.Exists(logFilePath).ShouldBeTrue();
        File.ReadAllText(logFilePath).ShouldContain("[INF] TestCategory: Hello");
    }

    [Fact]
    public void CreateLogger_WhenDateChanges_WritesToNewFile()
    {
        var logger = _provider.CreateLogger("TestCategory");

        logger.Log(LogLevel.Information, new EventId(1), "Day one", null, (state, _) => state);

        _timeProvider.Now = _timeProvider.Now.AddDays(1);

        logger.Log(LogLevel.Information, new EventId(1), "Day two", null, (state, _) => state);

        _provider.Dispose();

        File.Exists(Path.Combine(_logDirectory, "viscacamlink-2026-01-02.log")).ShouldBeTrue();
        File.Exists(Path.Combine(_logDirectory, "viscacamlink-2026-01-03.log")).ShouldBeTrue();
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
