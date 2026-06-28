namespace ViscaCamLink.Simulator;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// TCP server that listens for VISCA commands and dispatches them to the command handler.
/// </summary>
public class ViscaTcpServer
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ViscaCommandHandler _handler;
    private readonly Action<string> _log;
    private readonly Action<bool> _onClientConnected;

    public int Port { get; private set; }
    public bool IsRunning { get; private set; }

    public ViscaTcpServer(ViscaCommandHandler handler, Action<string> log, Action<bool> onClientConnected)
    {
        _handler = handler;
        _log = log;
        _onClientConnected = onClientConnected;
    }

    public void Start(int port)
    {
        Stop();
        Port = port;
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, port);
        _listener.Start();
        IsRunning = true;
        _log($"Server started on 127.0.0.1:{port}");
        Task.Run(() => AcceptClientsAsync(_cts.Token));
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        _listener?.Stop();
        _listener = null;
        IsRunning = false;
        _log("Server stopped");
    }

    private async Task AcceptClientsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                client.NoDelay = true;
                _log($"Client connected: {client.Client.RemoteEndPoint}");
                _onClientConnected(true);
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var buffer = new byte[256];
        var packetBuffer = new List<byte>();

        try
        {
            using var stream = client.GetStream();
            while (!ct.IsCancellationRequested && client.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, ct);
                if (bytesRead == 0) break;

                for (int i = 0; i < bytesRead; i++)
                {
                    packetBuffer.Add(buffer[i]);

                    if (buffer[i] == ViscaConstants.Terminator)
                    {
                        var packet = packetBuffer.ToArray();
                        packetBuffer.Clear();

                        var response = _handler.ProcessPacket(packet);
                        if (response != null)
                        {
                            await stream.WriteAsync(response, ct);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _log($"Client error: {ex.Message}");
        }
        finally
        {
            client.Dispose();
            _onClientConnected(false);
            _log("Client disconnected");
        }
    }
}
