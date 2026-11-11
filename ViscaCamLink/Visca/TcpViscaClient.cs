namespace ViscaCamLink.Visca;

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

public partial class TcpViscaClient(string host, int port, ILogger logger) : ViscaClientBase(logger)
{
    private readonly ReadBuffer _readBuffer = new();
    private readonly byte[] _writeBuffer = new byte[ViscaPacket.MaximumLength];

    private string _host = host;
    private int _port = port;
    private bool _isFirstConnection = true;
    private int _connectionState = -1;
    private TcpClient? _tcpClient;
    private Stream? _networkStream;

    public override void Dispose()
    {
        ExecuteWithSendReceiveLock(() =>
        {
            _tcpClient?.Dispose();
            _tcpClient = null;
            _networkStream = null;
            _readBuffer.Clear();
            Volatile.Write(ref _connectionState, -1);
        });

        GC.SuppressFinalize(this);
    }

    public override bool? IsConnected()
    {
        var state = Volatile.Read(ref _connectionState);
        return state < 0 ? null : state == 1;
    }

    public override Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default) =>
        ExecuteWithSendReceiveLockAsync(async () =>
        {
            _host = host ?? _host;
            _port = port ?? _port;
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }, cancellationToken);

    protected override async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_isFirstConnection)
        {
            LogConnecting(Logger, _host, _port);

            _isFirstConnection = false;
        }
        else
        {
            LogReconnecting(Logger, _host, _port);
        }

        _readBuffer.Clear();
        _tcpClient?.Dispose();
        _networkStream = null;
        Volatile.Write(ref _connectionState, -1);
        var client = new TcpClient { NoDelay = true };
        _tcpClient = client;

        try
        {
            using var timeoutSource = new CancellationTokenSource(ViscaProtocol.ConnectTimeout);
            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            await client.ConnectAsync(_host, _port, linkedCancellation.Token).ConfigureAwait(false);
            _networkStream = client.GetStream();
            Volatile.Write(ref _connectionState, 1);
        }
        catch (Exception exception)
        {
            client.Dispose();
            _tcpClient = null;
            _networkStream = null;
            Volatile.Write(ref _connectionState, -1);

            LogConnectFailed(Logger, _host, _port, exception);

            throw;
        }
    }

    protected override void Disconnect()
    {
        _readBuffer.Clear();
        _tcpClient?.Dispose();
        _tcpClient = null;
        _networkStream = null;
        Volatile.Write(ref _connectionState, -1);
    }

    protected override async Task SendPacketAsync(ViscaPacket packet, CancellationToken cancellationToken)
    {
        if (_tcpClient is null)
        {
            await ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        for (var i = 0; i < packet.Length; i++)
        {
            _writeBuffer[i] = packet[i];
        }

        await _networkStream!.WriteAsync(_writeBuffer.AsMemory(0, packet.Length), cancellationToken).ConfigureAwait(false);
    }

    protected override Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        if (_networkStream is null)
        {
            throw new ViscaProtocolException("Cannot receive a packet before sending one");
        }

        return _readBuffer.ReadAsync(_networkStream, cancellationToken);
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Connecting to {Host}:{Port}")]
    private static partial void LogConnecting(ILogger logger, string host, int port);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Reconnecting to {Host}:{Port}")]
    private static partial void LogReconnecting(ILogger logger, string host, int port);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Failed to connect to {Host}:{Port}")]
    private static partial void LogConnectFailed(ILogger logger, string host, int port, Exception exception);
}
