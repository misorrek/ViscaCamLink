// Copyright 2020 Jon Skeet. All rights reserved.
// Adapted from jskeet/DemoCode (Apache-2.0); modified by Max Groiser.

namespace ViscaCamLink.Visca;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

public abstract partial class ViscaClientBase(ILogger logger) : IViscaClient
{
    private const int ResponseTypeBitShift = 4;
    private const int ResponseTypeAcknowledged = 4;
    private const int ResponseTypeCompleted = 5;
    private const int ResponseTypeError = 6;
    private const int MinimumResponseLength = 2;
    private const byte CameraNumberMask = 0x0f;
    private const byte ReplyAddressOffset = 0x08;
    private const byte ReplyAddressShift = 4;

    private readonly SemaphoreSlim _sendReceiveLock = new(1);
    private readonly object _pendingSendsLock = new();
    private readonly List<PendingSend> _pendingSends = [];
    private bool _isProcessingSends;

    protected ILogger Logger { get; } = logger;

    public abstract void Dispose();

    public abstract bool? IsConnected();

    public abstract Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default);

    async Task<ViscaPacket> IViscaClient.SendAsync(ViscaPacket request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return await Task.FromCanceled<ViscaPacket>(cancellationToken).ConfigureAwait(false);
        }

        var (motionKind, isStop) = GetMotionKind(request);
        var waiter = new SendWaiter(cancellationToken);
        var startProcessing = false;

        lock (_pendingSendsLock)
        {
            var pending = motionKind == MotionKind.None
                ? null
                : _pendingSends.FirstOrDefault(send => send.MotionKind == motionKind && (!send.IsStop || isStop));

            if (pending is not null)
            {
                _pendingSends.Remove(pending);
                pending.Packet = request;
                pending.IsStop = isStop;
                pending.Waiters.Add(waiter);
                _pendingSends.Insert(isStop ? 0 : _pendingSends.Count, pending);
            }
            else
            {
                var send = new PendingSend(request, motionKind, isStop, waiter);
                if (isStop)
                {
                    _pendingSends.Insert(0, send);
                }
                else
                {
                    _pendingSends.Add(send);
                }
            }

            if (!_isProcessingSends)
            {
                _isProcessingSends = true;
                startProcessing = true;
            }
        }

        if (startProcessing)
        {
            _ = ProcessPendingSendsAsync();
        }

        return await waiter.Task.ConfigureAwait(false);
    }

    protected abstract Task ConnectAsync(CancellationToken cancellationToken);

    protected abstract void Disconnect();

    protected abstract Task SendPacketAsync(ViscaPacket packet, CancellationToken cancellationToken);

    protected abstract Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken);

    protected async Task ExecuteWithSendReceiveLockAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await _sendReceiveLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _sendReceiveLock.Release();
        }
    }

    protected void ExecuteWithSendReceiveLock(Action action)
    {
        _sendReceiveLock.Wait();

        try
        {
            action();
        }
        finally
        {
            _sendReceiveLock.Release();
        }
    }

    protected T ExecuteWithSendReceiveLock<T>(Func<T> action)
    {
        _sendReceiveLock.Wait();

        try
        {
            return action();
        }
        finally
        {
            _sendReceiveLock.Release();
        }
    }

    private async Task ProcessPendingSendsAsync()
    {
        while (true)
        {
            PendingSend send;

            lock (_pendingSendsLock)
            {
                if (_pendingSends.Count == 0)
                {
                    _isProcessingSends = false;
                    return;
                }

                send = _pendingSends[0];
                _pendingSends.RemoveAt(0);
            }

            if (!send.TryGetCancellationToken(out var cancellationToken))
            {
                send.DisposeCancellationRegistrations();
                continue;
            }

            try
            {
                var response = await SendReceiveAsync(send.Packet, cancellationToken).ConfigureAwait(false);
                send.Complete(response);
            }
            catch (Exception exception)
            {
                if (send.IsStop && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var response = await SendReceiveAsync(send.Packet, cancellationToken).ConfigureAwait(false);
                        send.Complete(response);
                        continue;
                    }
                    catch (Exception retryException)
                    {
                        exception = retryException;
                    }
                }

                send.Fail(exception);
            }
        }
    }

    private async Task<ViscaPacket> SendReceiveAsync(ViscaPacket request, CancellationToken cancellationToken)
    {
        await _sendReceiveLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        var shouldDisconnect = true;

        try
        {
            await SendPacketAsync(request, cancellationToken).ConfigureAwait(false);

            LogPacketSent(Logger, request);
            var expectedReplyAddress = (byte)((((request[0] & CameraNumberMask) + ReplyAddressOffset) << ReplyAddressShift));
            using var timeoutSource = IsContinuousMotion(request)
                ? new CancellationTokenSource(ViscaProtocol.ContinuousCommandTimeout)
                : null;
            using var linkedCancellation = timeoutSource is null
                ? null
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            var receiveCancellationToken = linkedCancellation?.Token ?? cancellationToken;

            while (true)
            {
                var response = await ReceivePacketAsync(receiveCancellationToken).ConfigureAwait(false);

                LogPacketReceived(Logger, response);

                if (response.Length < MinimumResponseLength)
                {
                    throw new ViscaProtocolException($"Received packet of length {response.Length} from VISCA endpoint");
                }

                if (response[0] != expectedReplyAddress)
                {
                    LogUnmatchedReply(Logger, response[0], expectedReplyAddress);
                    continue;
                }

                var responseType = response[ViscaProtocol.ResponseTypeByteIndex] >> ResponseTypeBitShift;

                switch (responseType)
                {
                    case ResponseTypeAcknowledged:
                        continue;
                    case ResponseTypeCompleted:
                        shouldDisconnect = false;
                        return response;
                    case ResponseTypeError:
                        throw new ViscaResponseException($"Error returned from VISCA endpoint. Error data: {response}");
                    default:
                        throw new ViscaProtocolException($"Invalid packet returned from VISCA endpoint. Error data: {response}");
                }
            }
        }
        finally
        {
            try
            {
                if (shouldDisconnect)
                {
                    Disconnect();
                }
            }
            finally
            {
                _sendReceiveLock.Release();
            }
        }
    }

    private static (MotionKind Kind, bool IsStop) GetMotionKind(ViscaPacket request)
    {
        if (request.Length == 9 &&
            request[1] == ViscaProtocol.CommandPrefix &&
            request[2] == ViscaProtocol.CategoryPanTilt &&
            request[3] == ViscaProtocol.CmdContinuousPanTilt)
        {
            return (MotionKind.PanTilt, request[6] == ViscaProtocol.DirectionStop && request[7] == ViscaProtocol.DirectionStop);
        }

        if (request.Length == 6 &&
            request[1] == ViscaProtocol.CommandPrefix &&
            request[2] == ViscaProtocol.CategoryCamera &&
            request[3] == ViscaProtocol.CmdZoomVariable)
        {
            return (MotionKind.Zoom, request[4] == ViscaProtocol.ZoomStop);
        }

        return (MotionKind.None, false);
    }

    private static bool IsContinuousMotion(ViscaPacket request) =>
        GetMotionKind(request).Kind != MotionKind.None;

    private enum MotionKind
    {
        None,
        PanTilt,
        Zoom
    }

    private sealed class PendingSend(ViscaPacket packet, MotionKind motionKind, bool isStop, SendWaiter waiter)
    {
        public ViscaPacket Packet { get; set; } = packet;

        public MotionKind MotionKind { get; } = motionKind;

        public bool IsStop { get; set; } = isStop;

        public List<SendWaiter> Waiters { get; } = [waiter];

        public bool TryGetCancellationToken(out CancellationToken cancellationToken)
        {
            var waiter = Waiters.LastOrDefault(candidate => !candidate.Task.IsCompleted);
            cancellationToken = waiter?.CancellationToken ?? default;
            return waiter is not null;
        }

        public void Complete(ViscaPacket response)
        {
            foreach (var waiter in Waiters)
            {
                waiter.TrySetResult(response);
            }
        }

        public void Fail(Exception exception)
        {
            foreach (var waiter in Waiters)
            {
                waiter.TrySetException(exception);
            }
        }

        public void DisposeCancellationRegistrations()
        {
            foreach (var waiter in Waiters)
            {
                waiter.DisposeCancellationRegistration();
            }
        }
    }

    private sealed class SendWaiter
    {
        private readonly TaskCompletionSource<ViscaPacket> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenRegistration _cancellationRegistration;

        public SendWaiter(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            _cancellationRegistration = cancellationToken.Register(
                static state =>
                {
                    var (completionSource, token) = ((TaskCompletionSource<ViscaPacket>, CancellationToken))state!;
                    completionSource.TrySetCanceled(token);
                },
                (_completionSource, cancellationToken));
        }

        public CancellationToken CancellationToken { get; }

        public Task<ViscaPacket> Task => _completionSource.Task;

        public void TrySetResult(ViscaPacket response)
        {
            _completionSource.TrySetResult(response);
            _cancellationRegistration.Dispose();
        }

        public void TrySetException(Exception exception)
        {
            _completionSource.TrySetException(exception);
            _cancellationRegistration.Dispose();
        }

        public void DisposeCancellationRegistration() => _cancellationRegistration.Dispose();
    }

    [LoggerMessage(EventId = 1201, Level = LogLevel.Trace, Message = "Sent VISCA packet: {Packet}")]
    private static partial void LogPacketSent(ILogger logger, ViscaPacket packet);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Trace, Message = "Received VISCA packet: {Packet}")]
    private static partial void LogPacketReceived(ILogger logger, ViscaPacket packet);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Warning, Message = "Discarding VISCA reply from address 0x{ReplyAddress:X2}; expected 0x{ExpectedAddress:X2}")]
    private static partial void LogUnmatchedReply(ILogger logger, byte replyAddress, byte expectedAddress);
}
