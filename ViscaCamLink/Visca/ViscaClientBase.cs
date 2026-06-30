namespace ViscaCamLink.Visca;

using Microsoft.Extensions.Logging;

public abstract partial class ViscaClientBase(ILogger logger) : IViscaClient
{
    private const int ResponseTypeBitShift = 4;
    private const int ResponseTypeAcknowledged = 4;
    private const int ResponseTypeCompleted = 5;
    private const int ResponseTypeError = 6;
    private const int MinimumResponseLength = 2;

    public abstract void Dispose();
    public abstract bool? IsConnected();
    public abstract Task Reconnect(CancellationToken cancellationToken, string? host = null, int? port = null);

    protected abstract Task SendPacketAsync(ViscaPacket packet, CancellationToken cancellationToken);
    protected abstract Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken);
    protected abstract Task ConnectAsync(CancellationToken cancellationToken);
    protected abstract void Disconnect();

    protected ILogger Logger { get; } = logger;

    private readonly SemaphoreSlim sendReceiveLock = new(1);

    async Task<ViscaPacket> IViscaClient.SendAsync(ViscaPacket request, CancellationToken cancellationToken)
    {
        var shouldDisconnect = true;

        await sendReceiveLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await SendPacketAsync(request, cancellationToken).ConfigureAwait(false);

            LogPacketSent(Logger, request);

            while (true)
            {
                var response = await ReceivePacketAsync(cancellationToken).ConfigureAwait(false);

                LogPacketReceived(Logger, response);

                if (response.Length < MinimumResponseLength)
                {
                    throw new ViscaProtocolException($"Received packet of length {response.Length} from VISCA endpoint");
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
                sendReceiveLock.Release();
            }
        }
    }

    [LoggerMessage(EventId = 1201, Level = LogLevel.Trace, Message = "Sent VISCA packet: {Packet}")]
    private static partial void LogPacketSent(ILogger logger, ViscaPacket packet);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Trace, Message = "Received VISCA packet: {Packet}")]
    private static partial void LogPacketReceived(ILogger logger, ViscaPacket packet);
}
