namespace ViscaCamLink.Tests.Visca;

using FluentAssertions;

using ViscaCamLink.Visca;

public sealed class ViscaClientBaseTests
{
    [Fact]
    public async Task SendAsync_ReceivesCompletionResponse_ReturnsIt()
    {
        var client = new FakeClient([CreateResponsePacket(0x90, 0x50, 0x02)]);

        var result = await ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        result[1].Should().Be(0x50);
    }

    [Fact]
    public async Task SendAsync_ReceivesAckThenCompletion_SkipsAckAndReturnsCompletion()
    {
        var client = new FakeClient([
            CreateResponsePacket(0x90, 0x41),
            CreateResponsePacket(0x90, 0x50, 0x02)
        ]);

        var result = await ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        result[1].Should().Be(0x50);
    }

    [Fact]
    public async Task SendAsync_ReceivesErrorResponse_ThrowsViscaResponseException()
    {
        var client = new FakeClient([CreateResponsePacket(0x90, 0x60, 0x02)]);

        var act = () => ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ViscaResponseException>()
            .WithMessage("*Error returned from VISCA endpoint*");
    }

    [Fact]
    public async Task SendAsync_ReceivesInvalidResponseType_ThrowsViscaProtocolException()
    {
        var client = new FakeClient([CreateResponsePacket(0x90, 0x70, 0x00)]);

        var act = () => ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ViscaProtocolException>()
            .WithMessage("*Invalid packet*");
    }

    [Fact]
    public async Task SendAsync_ReceivesTooShortPacket_ThrowsViscaProtocolException()
    {
        var client = new FakeClient([ViscaPacket.FromBytes([0x90], 0, 1)]);

        var act = () => ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<ViscaProtocolException>()
            .WithMessage("*length*");
    }

    [Fact]
    public async Task SendAsync_OnError_CallsDisconnect()
    {
        var client = new FakeClient([CreateResponsePacket(0x90, 0x60, 0x02)]);

        try { await ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None); } catch { }

        client.DisconnectCallCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_OnSuccess_DoesNotDisconnect()
    {
        var client = new FakeClient([CreateResponsePacket(0x90, 0x50, 0x02)]);

        await ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        client.DisconnectCallCount.Should().Be(0);
    }

    private static ViscaPacket CreateRequest() =>
        ViscaPacket.FromBytesWithPreformatting(0x81, 0x01, 0x04, 0x00, 0x02, 0xff);

    private static ViscaPacket CreateResponsePacket(params byte[] bytes) =>
        ViscaPacket.FromBytes(bytes, 0, bytes.Length);

    private sealed class FakeClient(ViscaPacket[] responses) : ViscaClientBase(logger: null)
    {
        private int responseIndex;

        public int DisconnectCallCount { get; private set; }

        protected override Task SendPacketAsync(ViscaPacket packet, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        protected override Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken) =>
            Task.FromResult(responses[responseIndex++]);

        protected override Task ConnectAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        protected override void Disconnect() => DisconnectCallCount++;

        public override void Dispose() { }
        public override bool? IsConnected() => true;
        public override Task Reconnect(CancellationToken cancellationToken, string? host = null, int? port = null) =>
            Task.CompletedTask;
    }
}
