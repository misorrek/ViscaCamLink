namespace ViscaCamLink.Visca;

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

    private readonly SemaphoreSlim _sendReceiveLock = new(1);

    protected ILogger Logger { get; } = logger;

    public abstract void Dispose();

    public abstract bool? IsConnected();

    public abstract Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default);

    async Task<ViscaPacket> IViscaClient.SendAsync(ViscaPacket request, CancellationToken cancellationToken)
    {
        var shouldDisconnect = true;

        await _sendReceiveLock.WaitAsync(cancellationToken).ConfigureAwait(false);

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
                _sendReceiveLock.Release();
            }
        }
    }

    protected abstract Task ConnectAsync(CancellationToken cancellationToken);

    protected abstract void Disconnect();

    protected abstract Task SendPacketAsync(ViscaPacket packet, CancellationToken cancellationToken);

    protected abstract Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Trace, Message = "Sent VISCA packet: {Packet}")]
    private static partial void LogPacketSent(ILogger logger, ViscaPacket packet);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Trace, Message = "Received VISCA packet: {Packet}")]
    private static partial void LogPacketReceived(ILogger logger, ViscaPacket packet);
}
