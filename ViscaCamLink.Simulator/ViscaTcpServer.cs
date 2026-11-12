namespace ViscaCamLink.Simulator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// TCP server that listens for VISCA commands and dispatches them to the command handler.
/// </summary>
public class ViscaTcpServer(ViscaCommandHandler handler, Action<string> log, Action<bool> onClientConnected) : IDisposable
{
    private const int ReceiveBufferSize = 256;

    private TcpListener? _listener;
    private CancellationTokenSource? _cancellationSource;
    private bool _isDisposed;

    public int Port { get; private set; }

    public bool IsRunning { get; private set; }

    public void Start(int port)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        Stop();
        Port = port;

        var cancellationSource = new CancellationTokenSource();
        var listener = new TcpListener(IPAddress.Loopback, port);

        try
        {
            listener.Start();
        }
        catch (SocketException)
        {
            listener.Dispose();
            cancellationSource.Dispose();

            throw;
        }

        _cancellationSource = cancellationSource;
        _listener = listener;
        IsRunning = true;
        log($"Server started on 127.0.0.1:{port}");
        _ = Task.Run(() => AcceptClientsAsync(listener, cancellationSource.Token));
    }

    public void Stop()
    {
        var wasRunning = IsRunning;

        _cancellationSource?.Cancel();
        _listener?.Dispose();
        _listener = null;
        _cancellationSource?.Dispose();
        _cancellationSource = null;
        IsRunning = false;

        if (wasRunning)
        {
            log("Server stopped");
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Stop();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task AcceptClientsAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                client.NoDelay = true;
                log($"Client connected: {client.Client.RemoteEndPoint}");
                onClientConnected(true);
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                // Expected when the server stops while waiting for a client.
                break;
            }
            catch (SocketException ex)
            {
                log($"Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var buffer = new byte[ReceiveBufferSize];
        var packetBuffer = new List<byte>();

        try
        {
            using var stream = client.GetStream();

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                for (var i = 0; i < bytesRead; i++)
                {
                    packetBuffer.Add(buffer[i]);

                    if (buffer[i] != ViscaConstants.Terminator)
                    {
                        continue;
                    }

                    var response = handler.ProcessPacket([.. packetBuffer]);
                    packetBuffer.Clear();

                    if (response is not null)
                    {
                        await stream.WriteAsync(response, cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the server stops while a client is connected.
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            log($"Client error: {ex.Message}");
        }
        finally
        {
            client.Dispose();
            onClientConnected(false);
            log("Client disconnected");
        }
    }
}
