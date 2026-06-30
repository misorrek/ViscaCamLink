namespace ViscaCamLink.Tests.Visca;

using System.IO;

using Shouldly;

using ViscaCamLink.Visca;

public sealed class ReadBufferTests
{
    private readonly ReadBuffer readBuffer = new();

    [Fact]
    public async Task ReadAsync_SingleCompletePacket_ReturnsPacketWithoutTerminator()
    {
        byte[] streamData = [0x90, 0x50, 0x02, 0xff];
        using var stream = new MemoryStream(streamData);

        var packet = await readBuffer.ReadAsync(stream, CancellationToken.None);

        packet.Length.ShouldBe(3);
        packet[0].ShouldBe((byte)0x90);
        packet[1].ShouldBe((byte)0x50);
        packet[2].ShouldBe((byte)0x02);
    }

    [Fact]
    public async Task ReadAsync_TwoPacketsInStream_ReturnsBothSequentially()
    {
        byte[] streamData = [0x90, 0x50, 0xff, 0x90, 0x41, 0xff];
        using var stream = new MemoryStream(streamData);

        var first = await readBuffer.ReadAsync(stream, CancellationToken.None);
        var second = await readBuffer.ReadAsync(stream, CancellationToken.None);

        first.Length.ShouldBe(2);
        first[0].ShouldBe((byte)0x90);
        first[1].ShouldBe((byte)0x50);

        second.Length.ShouldBe(2);
        second[0].ShouldBe((byte)0x90);
        second[1].ShouldBe((byte)0x41);
    }

    [Fact]
    public async Task ReadAsync_PacketArrivesInMultipleChunks_AssemblesCorrectly()
    {
        var chunkedStream = new ChunkedMemoryStream([0x90, 0x50, 0x02, 0xff], chunkSize: 2);

        var packet = await readBuffer.ReadAsync(chunkedStream, CancellationToken.None);

        packet.Length.ShouldBe(3);
        packet[0].ShouldBe((byte)0x90);
        packet[1].ShouldBe((byte)0x50);
        packet[2].ShouldBe((byte)0x02);
    }

    [Fact]
    public async Task ReadAsync_StreamEndsWithoutTerminator_ThrowsViscaProtocolException()
    {
        byte[] incompleteData = [0x90, 0x50];
        using var stream = new MemoryStream(incompleteData);

        var act = () => readBuffer.ReadAsync(stream, CancellationToken.None);

        var ex = await Should.ThrowAsync<ViscaProtocolException>(act);
        ex.Message.ShouldContain("end of VISCA stream");
    }

    [Fact]
    public async Task ReadAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var neverEndingStream = new BlockingStream();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => readBuffer.ReadAsync(neverEndingStream, cts.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task Clear_ResetsBufferState_AllowsNewRead()
    {
        byte[] firstData = [0x90, 0x50, 0xff];
        using var firstStream = new MemoryStream(firstData);
        await readBuffer.ReadAsync(firstStream, CancellationToken.None);

        readBuffer.Clear();

        byte[] secondData = [0x81, 0x01, 0x04, 0xff];
        using var secondStream = new MemoryStream(secondData);
        var packet = await readBuffer.ReadAsync(secondStream, CancellationToken.None);

        packet.Length.ShouldBe(3);
        packet[0].ShouldBe((byte)0x81);
    }

    private sealed class ChunkedMemoryStream(byte[] data, int chunkSize) : MemoryStream(data)
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            return base.Read(buffer, offset, Math.Min(count, chunkSize));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return base.ReadAsync(buffer, offset, Math.Min(count, chunkSize), cancellationToken);
        }
    }

    private sealed class BlockingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Stream should not be read without cancellation");
        }
    }
}
