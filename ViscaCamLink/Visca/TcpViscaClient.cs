namespace ViscaCamLink.Visca;

using System.IO;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

public sealed partial class TcpViscaClient(string host, int port, ILogger logger) : ViscaClientBase(logger)
{
    private readonly ReadBuffer readBuffer = new();
    private readonly byte[] writeBuffer = new byte[16];

    private string host = host;
    private int port = port;
    private bool isFirstConnection = true;
    private TcpClient? tcpClient;
    private Stream? networkStream;

    public override void Dispose()
    {
        tcpClient?.Dispose();
    }

    public override bool? IsConnected()
    {
        return tcpClient?.Connected;
    }

    public override Task Reconnect(CancellationToken cancellationToken, string? host = null, int? port = null)
    {
        this.host = host ?? this.host;
        this.port = port ?? this.port;

        return ConnectAsync(cancellationToken);
    }

    protected override void Disconnect()
    {
        readBuffer.Clear();
        tcpClient?.Dispose();
        tcpClient = null;
    }

    protected override async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (isFirstConnection)
        {
            LogConnecting(Logger, host, port);

            isFirstConnection = false;
        }
        else
        {
            LogReconnecting(Logger, host, port);
        }

        readBuffer.Clear();
        tcpClient?.Dispose();
        tcpClient = new TcpClient { NoDelay = true };

        try
        {
            await tcpClient.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            tcpClient = null;
            LogConnectFailed(Logger, host, port, ex);

            throw;
        }

        networkStream = tcpClient.GetStream();
    }

    protected override async Task SendPacketAsync(ViscaPacket packet, CancellationToken cancellationToken)
    {
        if (tcpClient is null)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        for (int i = 0; i < packet.Length; i++)
        {
            writeBuffer[i] = packet[i];
        }

        await networkStream!.WriteAsync(writeBuffer.AsMemory(0, packet.Length), cancellationToken).ConfigureAwait(false);
    }

    protected override Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        if (networkStream is null)
        {
            throw new ViscaProtocolException("Cannot receive a packet before sending one");
        }
        return readBuffer.ReadAsync(networkStream, cancellationToken);
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Connecting to {Host}:{Port}")]
    private static partial void LogConnecting(ILogger logger, string host, int port);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Reconnecting to {Host}:{Port}")]
    private static partial void LogReconnecting(ILogger logger, string host, int port);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Failed to connect to {Host}:{Port}")]
    private static partial void LogConnectFailed(ILogger logger, string host, int port, Exception ex);
}
