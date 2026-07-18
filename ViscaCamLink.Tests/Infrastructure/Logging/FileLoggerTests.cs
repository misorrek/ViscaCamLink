namespace ViscaCamLink.Tests.Infrastructure.Logging;

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Shouldly;

using ViscaCamLink.Infrastructure.Logging;

using Xunit;

public sealed class FileLoggerTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);

    private readonly List<string> _entries = [];
    private readonly FileLogger _logger;

    public FileLoggerTests()
    {
        _logger = new FileLogger("TestCategory", _entries.Add, new FixedTimeProvider(FixedTime));
    }

    [Theory]
    [InlineData(LogLevel.Trace, "TRC")]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Information, "INF")]
    [InlineData(LogLevel.Warning, "WRN")]
    [InlineData(LogLevel.Error, "ERR")]
    [InlineData(LogLevel.Critical, "CRT")]
    public void Log_Success(LogLevel logLevel, string expectedLevelCode)
    {
        _logger.Log(logLevel, new EventId(1), "Hello", null, (state, _) => state);

        _entries.ShouldHaveSingleItem();
        _entries[0].ShouldBe($"2026-01-02 03:04:05.678 [{expectedLevelCode}] TestCategory: Hello");
    }

    [Fact]
    public void Log_WhenExceptionIsGiven_AppendsExceptionOnNewLine()
    {
        var exception = new InvalidOperationException("boom");

        _logger.Log(LogLevel.Error, new EventId(1), "Failed", exception, (state, _) => state);

        _entries.ShouldHaveSingleItem();
        _entries[0].ShouldStartWith("2026-01-02 03:04:05.678 [ERR] TestCategory: Failed");
        _entries[0].ShouldContain(Environment.NewLine + exception);
    }

    [Fact]
    public void Log_WhenLogLevelIsNone_DoesNotWrite()
    {
        _logger.Log(LogLevel.None, new EventId(1), "Hidden", null, (state, _) => state);

        _entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(LogLevel.Trace, true)]
    [InlineData(LogLevel.Critical, true)]
    [InlineData(LogLevel.None, false)]
    public void IsEnabled_Success(LogLevel logLevel, bool expected)
    {
        _logger.IsEnabled(logLevel).ShouldBe(expected);
    }

    [Fact]
    public void BeginScope_ReturnsNull()
    {
        _logger.BeginScope("scope").ShouldBeNull();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
