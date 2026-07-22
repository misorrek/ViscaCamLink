namespace ViscaCamLink.Tests.Visca;

using System;

using Shouldly;

using ViscaCamLink.Visca;

using Xunit;

public sealed class ViscaPacketTests
{
    [Fact]
    public void FromBytes_Success()
    {
        byte[] data = [0x81, 0x01, 0x04, 0x00, 0x02, 0xff];

        var packet = ViscaPacket.FromBytes(data, 0, 5);

        packet.Length.ShouldBe(5);
    }

    [Fact]
    public void FromBytes_WhenStartOffsetIsGiven_ReadsFromOffset()
    {
        byte[] data = [0x00, 0x00, 0x81, 0x01, 0xff];

        var packet = ViscaPacket.FromBytes(data, start: 2, length: 2);

        packet[0].ShouldBe((byte)0x81);
        packet[1].ShouldBe((byte)0x01);
    }

    [Fact]
    public void FromBytes_WhenArrayIsNull_ThrowsArgumentNullException()
    {
        static void act() => _ = ViscaPacket.FromBytes(null!, 0, 1);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void FromBytes_WhenLengthExceedsMaximum_ThrowsArgumentOutOfRangeException()
    {
        var data = new byte[20];

        void act() => _ = ViscaPacket.FromBytes(data, 0, 17);

        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void FromBytes_WhenLengthIsZero_ThrowsArgumentOutOfRangeException()
    {
        var data = new byte[5];

        void act() => _ = ViscaPacket.FromBytes(data, 0, 0);

        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Indexer_Success()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a];
        var packet = ViscaPacket.FromBytes(data, 0, 10);

        packet[0].ShouldBe((byte)0x01);
        packet[1].ShouldBe((byte)0x02);
        packet[8].ShouldBe((byte)0x09);
        packet[9].ShouldBe((byte)0x0a);
    }

    [Fact]
    public void Indexer_WhenIndexIsNegative_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0x81, 0x01, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 2);

        void act() => _ = packet[-1];

        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Indexer_WhenIndexExceedsLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0x81, 0x01, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 2);

        void act() => _ = packet[2];

        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void GetByte_Success()
    {
        byte[] data = [0x90, 0x50, 0x02, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 3);

        packet.GetByte(1).ShouldBe(packet[1]);
    }

    [Theory]
    [InlineData((short)0x1234)]
    [InlineData((short)0)]
    public void GetInt16_Success(short expected)
    {
        byte[] data =
        [
            0x90, 0x50,
            (byte)((expected >> 12) & 0x0f),
            (byte)((expected >> 8) & 0x0f),
            (byte)((expected >> 4) & 0x0f),
            (byte)(expected & 0x0f),
            0xff,
        ];
        var packet = ViscaPacket.FromBytes(data, 0, 6);

        var value = packet.GetInt16(2);

        value.ShouldBe(expected);
    }

    [Fact]
    public void ToString_Success()
    {
        byte[] data = [0x81, 0x01, 0x04, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 3);

        packet.ToString().ShouldBe("81-01-04");
    }

    [Fact]
    public void ToString_WhenPacketHasSingleByte_OmitsHyphen()
    {
        byte[] data = [0xab];
        var packet = ViscaPacket.FromBytes(data, 0, 1);

        packet.ToString().ShouldBe("ab");
    }

    [Fact]
    public void FromBytesWithPreformatting_Success()
    {
        var packet = ViscaPacket.FromBytesWithPreformatting(0x81, 0x01, 0x04, 0xff);

        var first = packet.ToString();
        var second = packet.ToString();

        ReferenceEquals(first, second).ShouldBeTrue();
    }
}
