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
    private TcpClient? _tcpClient;
    private Stream? _networkStream;

    public override void Dispose()
    {
        _tcpClient?.Dispose();
    }

    public override bool? IsConnected()
    {
        return _tcpClient?.Connected;
    }

    public override Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default)
    {
        _host = host ?? _host;
        _port = port ?? _port;

        return ConnectAsync(cancellationToken);
    }

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
        _tcpClient = new TcpClient { NoDelay = true };

        try
        {
            await _tcpClient.ConnectAsync(_host, _port, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _tcpClient = null;

            LogConnectFailed(Logger, _host, _port, exception);

            throw;
        }

        _networkStream = _tcpClient.GetStream();
    }

    protected override void Disconnect()
    {
        _readBuffer.Clear();
        _tcpClient?.Dispose();
        _tcpClient = null;
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
