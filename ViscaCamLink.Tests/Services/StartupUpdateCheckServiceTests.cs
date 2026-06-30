namespace ViscaCamLink.Tests.Services;

using Shouldly;

using Moq;

using ViscaCamLink.Services;

public sealed class StartupUpdateCheckServiceTests
{
    private readonly Mock<IUpdateService> _updateService = new();

    [Fact]
    public async Task RunAsync_AfterDelay_StartsUpdateService()
    {
        var delayCalls = new List<TimeSpan>();
        var service = CreateService(delayAsync: (delay, _) =>
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
        using var cts = new CancellationTokenSource();
        var service = CreateService(delayAsync: (_, cancellationToken) => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));

        cts.Cancel();
        await service.RunAsync(cts.Token);

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

        var service = CreateService(reportException: exception => reported = exception);

        var act = () => service.RunAsync();

        await Should.NotThrowAsync(act);
        reported.ShouldBeSameAs(thrown);
    }

    private StartupUpdateCheckService CreateService(
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        Action<Exception>? reportException = null) =>
        new(
            _updateService.Object,
            TimeSpan.FromMilliseconds(10),
            delayAsync ?? ((_, _) => Task.CompletedTask),
            reportException);
}