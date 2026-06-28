namespace ViscaCamLink.Visca;

public class TcpSendLock(TimeSpan? postSendDelay)
{
    private readonly SemaphoreSlim semaphore = new(1);

    public Task AcquireAsync(CancellationToken cancellationToken) =>
        semaphore.WaitAsync(cancellationToken);

    public Task WaitPostSendDelayAsync(CancellationToken cancellationToken) =>
        postSendDelay is TimeSpan delay
            ? Task.Delay(delay, cancellationToken)
            : Task.CompletedTask;

    public void Release() => semaphore.Release();
}
