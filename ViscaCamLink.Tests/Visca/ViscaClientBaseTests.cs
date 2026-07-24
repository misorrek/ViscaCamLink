namespace ViscaCamLink.Tests.Visca;

using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using ViscaCamLink.Visca;

using Xunit;

public sealed class ViscaClientBaseTests
{
    [Fact]
    public async Task SendAsync_Success()
    {
        var client = new FakeClient([CreateResponsePacket(0x90, 0x50, 0x02)]);

        var result = await ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        result[1].ShouldBe((byte)0x50);
        client.DisconnectCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task SendAsync_WhenAckPrecedesCompletion_SkipsAckAndReturnsCompletion()
    {
        var client = new FakeClient([
            CreateResponsePacket(0x90, 0x41),
            CreateResponsePacket(0x90, 0x50, 0x02)
        ]);

        var result = await ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        result[1].ShouldBe((byte)0x50);
    }

    [Fact]
    public async Task SendAsync_WhenEndpointReturnsError_ThrowsViscaResponseExceptionAndDisconnects()
    {
        var client = new FakeClient([CreateResponsePacket(0x90, 0x60, 0x02)]);

        Task<ViscaPacket> act() => ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        var exception = await Should.ThrowAsync<ViscaResponseException>((System.Func<Task<ViscaPacket>>)act);

        exception.Message.ShouldContain("Error returned from VISCA endpoint");
        client.DisconnectCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task SendAsync_WhenResponseTypeIsInvalid_ThrowsViscaProtocolException()
    {
        var client = new FakeClient([CreateResponsePacket(0x90, 0x70, 0x00)]);

        Task<ViscaPacket> act() => ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        var exception = await Should.ThrowAsync<ViscaProtocolException>(act);

        exception.Message.ShouldContain("Invalid packet");
    }

    [Fact]
    public async Task SendAsync_WhenResponseIsTooShort_ThrowsViscaProtocolException()
    {
        var client = new FakeClient([ViscaPacket.FromBytes([0x90], 0, 1)]);

        Task<ViscaPacket> act() => ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        var exception = await Should.ThrowAsync<ViscaProtocolException>((System.Func<Task<ViscaPacket>>)act);

        exception.Message.ShouldContain("length");
    }

    private static ViscaPacket CreateRequest()
    {
        return ViscaPacket.FromBytesWithPreformatting(0x81, 0x01, 0x04, 0x00, 0x02, 0xff);
    }

    private static ViscaPacket CreateResponsePacket(params byte[] bytes)
    {
        return ViscaPacket.FromBytes(bytes, 0, bytes.Length);
    }

    private sealed class FakeClient(ViscaPacket[] responses) : ViscaClientBase(NullLogger.Instance)
    {
        private int _responseIndex;

        public int DisconnectCallCount { get; private set; }

        public override void Dispose() { }

        public override bool? IsConnected() => true;

        public override Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        protected override Task SendPacketAsync(ViscaPacket packet, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        protected override Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken) =>
            Task.FromResult(responses[_responseIndex++]);

        protected override Task ConnectAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        protected override void Disconnect() => DisconnectCallCount++;
    }
}
