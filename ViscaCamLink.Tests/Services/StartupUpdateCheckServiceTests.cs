namespace ViscaCamLink.Tests.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Services;

using Xunit;

public sealed class StartupUpdateCheckServiceTests
{
    private readonly Mock<IUpdateService> _updateService = new();

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
        _updateService.Verify(s => s.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenCancelledDuringDelay_DoesNotStartUpdateService()
    {
        using var cancellationSource = new CancellationTokenSource();
        var service = CreateSut(delayAsync: (_, cancellationToken) => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));

        cancellationSource.Cancel();

        await service.RunAsync(cancellationSource.Token);

        _updateService.Verify(s => s.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenUpdateServiceThrows_ReportsExceptionWithoutThrowing()
    {
        var thrown = new InvalidOperationException("update failure");
        Exception? reported = null;

        _updateService
            .Setup(s => s.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(thrown);

        var service = CreateSut(reportException: exception => reported = exception);
        Task act() => service.RunAsync();

        await Should.NotThrowAsync(act);

        reported.ShouldBeSameAs(thrown);
    }

    private StartupUpdateCheckService CreateSut(
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Action<Exception>? reportException = null)
    {
        return new StartupUpdateCheckService(
            _updateService.Object,
            TimeSpan.FromMilliseconds(10),
            delayAsync ?? ((_, _) => Task.CompletedTask),
            reportException);
    }
}
