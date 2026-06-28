namespace ViscaCamLink.Visca;

using System.IO;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

public sealed class TcpViscaClient(string host, int port, ILogger? logger, TcpSendLock? sendLock) : ViscaClientBase(logger)
{
    private readonly TcpSendLock sendLock = sendLock ?? new TcpSendLock(postSendDelay: null);
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
            Logger?.LogInformation("Connecting to {Host}:{Port}", host, port);
            isFirstConnection = false;
        }
        else
        {
            Logger?.LogInformation("Reconnecting to {Host}:{Port}", host, port);
        }

        readBuffer.Clear();
        tcpClient?.Dispose();
        tcpClient = new TcpClient { NoDelay = true };

        try
        {
            await tcpClient.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            tcpClient = null;
            Logger?.LogError("Failed to connect to {Host}:{Port}", host, port);
            
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

        await sendLock.AcquireAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await networkStream!.WriteAsync(writeBuffer.AsMemory(0, packet.Length), cancellationToken).ConfigureAwait(false);
            await sendLock.WaitPostSendDelayAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sendLock.Release();
        }
    }

    protected override Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        if (networkStream is null)
        {
            throw new ViscaProtocolException("Cannot receive a packet before sending one");
        }
        return readBuffer.ReadAsync(networkStream, cancellationToken);
    }
}
