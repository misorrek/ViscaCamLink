namespace ViscaCamLink.Tests.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

using ViscaCamLink.Services;

using Xunit;

public sealed class StartupUpdateCheckServiceTests
{
    private readonly Mock<IUpdateService> _updateService = new();
    private readonly Mock<ILogger<StartupUpdateCheckService>> _logger = new();

    public StartupUpdateCheckServiceTests()
    {
        _logger.Setup(logger => logger.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    public async Task RunAsync_Success()
    {
        var delayCalls = new List<TimeSpan>();
        var service = CreateSut(delayAsync: (delay, _) =>
        {
            delayCalls.Add(delay);
            return Task.CompletedTask;
        });

        await service.RunAsync();

        delayCalls.ShouldHaveSingleItem().ShouldBe(TimeSpan.FromMilliseconds(10));
        _updateService.Verify(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenCancelledDuringDelay_DoesNotStartUpdateService()
    {
        using var cancellationSource = new CancellationTokenSource();
        var service = CreateSut(delayAsync: (_, cancellationToken) => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));

        cancellationSource.Cancel();

        await service.RunAsync(cancellationSource.Token);

        _updateService.Verify(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenUpdateServiceThrows_LogsWarningWithoutThrowing()
    {
        var thrown = new InvalidOperationException("update failure");

        _updateService
            .Setup(s => s.CheckForUpdateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(thrown);

        var service = CreateSut();
        Task act() => service.RunAsync();

        await Should.NotThrowAsync(act);

        _logger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.Is<EventId>(eventId => eventId.Id == 4001),
                It.IsAny<It.IsAnyType>(),
                thrown,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private StartupUpdateCheckService CreateSut(Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        return new StartupUpdateCheckService(
            _updateService.Object,
            _logger.Object,
            TimeSpan.FromMilliseconds(10),
            delayAsync ?? ((_, _) => Task.CompletedTask));
    }
}
