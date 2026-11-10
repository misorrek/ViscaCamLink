namespace ViscaCamLink.Tests.Visca;

using System.Collections.Generic;
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
    public async Task SendAsync_WhenReplyAddressDoesNotMatch_DiscardsReplyAndContinues()
    {
        var client = new FakeClient([
            CreateResponsePacket(0xa0, 0x50),
            CreateResponsePacket(0x90, 0x50, 0x02)
        ]);

        var result = await ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);

        result[0].ShouldBe((byte)0x90);
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

    [Fact]
    public async Task SendAsync_WhenContinuousMovesAreQueued_SendsOnlyTheLatestMove()
    {
        var client = new QueuedFakeClient();
        var firstSend = ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);
        await client.FirstPacketSent.Task;

        var firstMove = ((IViscaClient)client).SendAsync(CreatePanTiltRequest(0x01), CancellationToken.None);
        var latestMove = ((IViscaClient)client).SendAsync(CreatePanTiltRequest(0x02), CancellationToken.None);

        client.ReleaseFirstResponse();
        await Task.WhenAll(firstSend, firstMove, latestMove);

        client.SentPackets.Count.ShouldBe(2);
        client.SentPackets[1][6].ShouldBe((byte)0x02);
    }

    [Fact]
    public async Task SendAsync_WhenStopIsQueued_ReplacesQueuedMoveAndRetriesFailure()
    {
        var client = new QueuedFakeClient(failNextStop: true);
        var firstSend = ((IViscaClient)client).SendAsync(CreateRequest(), CancellationToken.None);
        await client.FirstPacketSent.Task;

        var queuedMove = ((IViscaClient)client).SendAsync(CreatePanTiltRequest(0x01), CancellationToken.None);
        var stop = ((IViscaClient)client).SendAsync(CreatePanTiltStopRequest(), CancellationToken.None);

        client.ReleaseFirstResponse();
        await Task.WhenAll(firstSend, queuedMove, stop);

        client.SentPackets.Count.ShouldBe(3);
        client.SentPackets[1][6].ShouldBe(ViscaProtocol.DirectionStop);
        client.SentPackets[1][7].ShouldBe(ViscaProtocol.DirectionStop);
        client.SentPackets[2][6].ShouldBe(ViscaProtocol.DirectionStop);
    }

    private static ViscaPacket CreateRequest()
    {
        return ViscaPacket.FromBytesWithPreformatting(0x81, 0x01, 0x04, 0x00, 0x02, 0xff);
    }

    private static ViscaPacket CreatePanTiltRequest(byte panDirection)
    {
        return ViscaPacket.FromBytesWithPreformatting(
            0x81, 0x01, 0x06, ViscaProtocol.CmdContinuousPanTilt,
            0x10, 0x10, panDirection, ViscaProtocol.DirectionStop, 0xff);
    }

    private static ViscaPacket CreatePanTiltStopRequest()
    {
        return ViscaPacket.FromBytesWithPreformatting(
            0x81, 0x01, 0x06, ViscaProtocol.CmdContinuousPanTilt,
            0x00, 0x00, ViscaProtocol.DirectionStop, ViscaProtocol.DirectionStop, 0xff);
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

    private sealed class QueuedFakeClient(bool failNextStop = false) : ViscaClientBase(NullLogger.Instance)
    {
        private readonly TaskCompletionSource _firstResponse = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _sendCount;
        private bool _failNextStop = failNextStop;

        public List<ViscaPacket> SentPackets { get; } = [];

        public TaskCompletionSource FirstPacketSent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override void Dispose() { }

        public override bool? IsConnected() => true;

        public override Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        protected override Task SendPacketAsync(ViscaPacket packet, CancellationToken cancellationToken)
        {
            SentPackets.Add(packet);
            _sendCount++;

            if (_sendCount == 1)
            {
                FirstPacketSent.TrySetResult();
            }
            else if (_failNextStop && packet.Length == 9 && packet[6] == ViscaProtocol.DirectionStop && packet[7] == ViscaProtocol.DirectionStop)
            {
                _failNextStop = false;
                throw new ViscaProtocolException("Simulated stop failure");
            }

            return Task.CompletedTask;
        }

        protected override async Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken)
        {
            if (_sendCount == 1)
            {
                await _firstResponse.Task.WaitAsync(cancellationToken);
            }

            return CreateResponsePacket(0x90, 0x50, 0x02);
        }

        protected override Task ConnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected override void Disconnect() { }

        public void ReleaseFirstResponse() => _firstResponse.TrySetResult();
    }
}
