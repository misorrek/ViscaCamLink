namespace ViscaCamLink.Tests.Visca;

using FluentAssertions;

using ViscaCamLink.Visca;

public sealed class TcpSendLockTests
{
    [Fact]
    public async Task AcquireAndRelease_AllowsSubsequentAcquire()
    {
        var sendLock = new TcpSendLock(postSendDelay: null);

        await sendLock.AcquireAsync(CancellationToken.None);
        sendLock.Release();

        var secondAcquire = sendLock.AcquireAsync(CancellationToken.None);
        secondAcquire.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireAsync_WhenAlreadyHeld_BlocksUntilReleased()
    {
        var sendLock = new TcpSendLock(postSendDelay: null);

        await sendLock.AcquireAsync(CancellationToken.None);

        var secondAcquire = sendLock.AcquireAsync(CancellationToken.None);
        secondAcquire.IsCompleted.Should().BeFalse();

        sendLock.Release();
        await secondAcquire;
    }

    [Fact]
    public async Task AcquireAsync_Cancelled_ThrowsOperationCanceledException()
    {
        var sendLock = new TcpSendLock(postSendDelay: null);
        await sendLock.AcquireAsync(CancellationToken.None);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sendLock.AcquireAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();

        sendLock.Release();
    }

    [Fact]
    public async Task WaitPostSendDelayAsync_NoDelay_CompletesImmediately()
    {
        var sendLock = new TcpSendLock(postSendDelay: null);

        var task = sendLock.WaitPostSendDelayAsync(CancellationToken.None);

        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task WaitPostSendDelayAsync_WithDelay_WaitsApproximateDelay()
    {
        var sendLock = new TcpSendLock(postSendDelay: TimeSpan.FromMilliseconds(50));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await sendLock.WaitPostSendDelayAsync(CancellationToken.None);
        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(30);
    }

    [Fact]
    public async Task WaitPostSendDelayAsync_Cancelled_ThrowsOperationCanceledException()
    {
        var sendLock = new TcpSendLock(postSendDelay: TimeSpan.FromSeconds(10));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sendLock.WaitPostSendDelayAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
