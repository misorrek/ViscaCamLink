namespace ViscaCamLink.Tests.Visca;

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using ViscaCamLink.Visca;

using Xunit;

public sealed class TcpViscaClientTests
{
    [Fact]
    public async Task ReconnectAsync_ConnectsAndDisposeClearsConnection()
    {
        using var listener = CreateListener();
        var acceptTask = listener.AcceptTcpClientAsync();
        using var client = CreateClient(listener);

        await client.ReconnectAsync();
        using var serverClient = await acceptTask.WaitAsync(TimeSpan.FromSeconds(3));

        client.IsConnected().ShouldBe(true);

        client.Dispose();

        client.IsConnected().ShouldBeNull();
    }

    [Fact]
    public async Task ReconnectAsync_WhenConnectionIsRefused_CleansUpClient()
    {
        using var listener = CreateListener();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        using var client = new TcpViscaClient(IPAddress.Loopback.ToString(), port, NullLogger.Instance);

        await Should.ThrowAsync<SocketException>(() => client.ReconnectAsync());

        client.IsConnected().ShouldBeNull();
        ViscaProtocol.ConnectTimeout.ShouldBe(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task ReconnectAsync_DuringSend_WaitsUntilSendCompletes()
    {
        using var listener = CreateListener();
        var firstAccept = listener.AcceptTcpClientAsync();
        using var client = CreateClient(listener);
        var sendTask = ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);
        using var firstServerClient = await firstAccept.WaitAsync(TimeSpan.FromSeconds(3));
        var firstStream = firstServerClient.GetStream();
        var requestBytes = new byte[6];
        await firstStream.ReadExactlyAsync(requestBytes);

        var secondAccept = listener.AcceptTcpClientAsync();
        var reconnectTask = client.ReconnectAsync();
        await Task.Delay(50);
        reconnectTask.IsCompleted.ShouldBeFalse();

        await firstStream.WriteAsync(new byte[] { 0x90, 0x50, 0x02, 0xff });
        await sendTask.WaitAsync(TimeSpan.FromSeconds(3));
        await reconnectTask.WaitAsync(TimeSpan.FromSeconds(3));
        using var secondServerClient = await secondAccept.WaitAsync(TimeSpan.FromSeconds(3));

        client.IsConnected().ShouldBe(true);
    }

    [Fact]
    public async Task SendAsync_WhenServerClosesConnection_DisconnectsClient()
    {
        using var listener = CreateListener();
        var acceptTask = listener.AcceptTcpClientAsync();
        using var client = CreateClient(listener);
        var reconnectTask = client.ReconnectAsync();
        using var serverClient = await acceptTask.WaitAsync(TimeSpan.FromSeconds(3));
        await reconnectTask;
        serverClient.Dispose();

        var sendTask = ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        await Should.ThrowAsync<IOException>(async () => await sendTask.WaitAsync(TimeSpan.FromSeconds(3)));

        client.IsConnected().ShouldBeNull();
    }

    [Fact]
    public async Task SendAsync_WhenContinuousCommandTimesOut_DisconnectsClient()
    {
        using var listener = CreateListener();
        var acceptTask = listener.AcceptTcpClientAsync();
        using var client = CreateClient(listener);
        var sendTask = ((IViscaClient)client).SendAsync(CreatePanTiltRequest(), CancellationToken.None);
        using var serverClient = await acceptTask.WaitAsync(TimeSpan.FromSeconds(3));
        await serverClient.GetStream().ReadExactlyAsync(new byte[9]);

        await Should.ThrowAsync<OperationCanceledException>(async () => await sendTask.WaitAsync(TimeSpan.FromSeconds(4)));

        client.IsConnected().ShouldBeNull();
    }

    private static TcpListener CreateListener()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static TcpViscaClient CreateClient(TcpListener listener) =>
        new(IPAddress.Loopback.ToString(), ((IPEndPoint)listener.LocalEndpoint).Port, NullLogger.Instance);

    private static ViscaPacket CreateRequest() =>
        ViscaPacket.FromBytesWithPreformatting(0x81, 0x01, 0x04, 0x00, 0x02, 0xff);

    private static ViscaPacket CreatePanTiltRequest() =>
        ViscaPacket.FromBytesWithPreformatting(
            0x81, 0x01, 0x06, ViscaProtocol.CmdContinuousPanTilt,
            0x10, 0x10, 0x02, ViscaProtocol.DirectionStop, 0xff);
}
