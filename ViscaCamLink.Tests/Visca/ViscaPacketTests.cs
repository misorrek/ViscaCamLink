namespace ViscaCamLink.Tests.Visca;

using FluentAssertions;

using ViscaCamLink.Visca;

public sealed class ViscaPacketTests
{
    [Fact]
    public void FromBytes_CreatesPacketWithCorrectLength()
    {
        byte[] data = [0x81, 0x01, 0x04, 0x00, 0x02, 0xff];

        var packet = ViscaPacket.FromBytes(data, 0, 5);

        packet.Length.Should().Be(5);
    }

    [Fact]
    public void Indexer_ReturnsCorrectBytesFromHead()
    {
        byte[] data = [0x81, 0x01, 0x04, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 3);

        packet[0].Should().Be(0x81);
        packet[1].Should().Be(0x01);
        packet[2].Should().Be(0x04);
    }

    [Fact]
    public void Indexer_ReturnsCorrectBytesFromTail()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A];
        var packet = ViscaPacket.FromBytes(data, 0, 10);

        packet[8].Should().Be(0x09);
        packet[9].Should().Be(0x0A);
    }

    [Fact]
    public void Indexer_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0x81, 0x01, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 2);

        var act = () => packet[-1];

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Indexer_IndexBeyondLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0x81, 0x01, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 2);

        var act = () => packet[2];

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetByte_IsSameAsIndexer()
    {
        byte[] data = [0x90, 0x50, 0x02, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 3);

        packet.GetByte(1).Should().Be(packet[1]);
    }

    [Fact]
    public void GetInt16_DecodesNibbleEncodedValue()
    {
        // VISCA encodes int16 as 4 nibble bytes: 0x0p 0x0q 0x0r 0x0s → value = (p<<12)|(q<<8)|(r<<4)|s
        byte[] data = [0x90, 0x50, 0x01, 0x02, 0x03, 0x04, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 6);

        short value = packet.GetInt16(2);

        value.Should().Be(0x1234);
    }

    [Fact]
    public void GetInt16_ZeroValue()
    {
        byte[] data = [0x90, 0x50, 0x00, 0x00, 0x00, 0x00, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 6);

        packet.GetInt16(2).Should().Be(0);
    }

    [Fact]
    public void FromBytes_WithStartOffset_ReadsFromCorrectPosition()
    {
        byte[] data = [0x00, 0x00, 0x81, 0x01, 0xff];

        var packet = ViscaPacket.FromBytes(data, start: 2, length: 2);

        packet[0].Should().Be(0x81);
        packet[1].Should().Be(0x01);
    }

    [Fact]
    public void FromBytes_NullArray_ThrowsArgumentNullException()
    {
        var act = () => ViscaPacket.FromBytes(null!, 0, 1);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromBytes_LengthExceeds16_ThrowsArgumentOutOfRangeException()
    {
        var data = new byte[20];

        var act = () => ViscaPacket.FromBytes(data, 0, 17);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromBytes_ZeroLength_ThrowsArgumentOutOfRangeException()
    {
        var data = new byte[5];

        var act = () => ViscaPacket.FromBytes(data, 0, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ToString_FormatsAsHyphenSeparatedHex()
    {
        byte[] data = [0x81, 0x01, 0x04, 0xff];
        var packet = ViscaPacket.FromBytes(data, 0, 3);

        packet.ToString().Should().Be("81-01-04");
    }

    [Fact]
    public void FromBytesWithPreformatting_CachesToStringResult()
    {
        var packet = ViscaPacket.FromBytesWithPreformatting(0x81, 0x01, 0x04, 0xff);

        var first = packet.ToString();
        var second = packet.ToString();

        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void ToString_SingleByte_NoHyphen()
    {
        byte[] data = [0xAB];
        var packet = ViscaPacket.FromBytes(data, 0, 1);

        packet.ToString().Should().Be("ab");
    }
}
