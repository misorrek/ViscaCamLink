namespace ViscaCamLink.Visca;

using System.Text;

public readonly struct ViscaPacket
{
    private const int BytesPerSegment = 8;
    private const int BitsPerByte = 8;
    private const int MinimumPacketLength = 1;
    private const int MaximumPacketLength = BytesPerSegment * 2;
    private const int MostSignificantByteShift = (BytesPerSegment - 1) * BitsPerByte;
    private const int BitsPerViscaNibble = 4;

    private readonly long head;
    private readonly long tail;
    private readonly string? cachedText;

    private ViscaPacket(long head, long tail, int length, string? cachedText) =>
        (this.head, this.tail, Length, this.cachedText) = (head, tail, length, cachedText);

    public int Length { get; }

    public byte this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);

            var isHeadSegment = index < BytesPerSegment;
            var segment = isHeadSegment ? head : tail;
            var segmentIndex = isHeadSegment ? index : index - BytesPerSegment;

            return (byte)(segment >> (MostSignificantByteShift - (segmentIndex * BitsPerByte)));
        }
    }

    public static ViscaPacket FromBytes(byte[] bytes, int start, int length) =>
        CreateFromBytes(bytes, start, length, preformatText: false);

    public static ViscaPacket FromBytesWithPreformatting(params byte[] bytes) =>
        CreateFromBytes(bytes, start: 0, bytes.Length, preformatText: true);

    public byte GetByte(int index) => this[index];

    public short GetInt16(int index)
    {
        const int ViscaInt16LengthInBytes = 4;

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Length - ViscaInt16LengthInBytes);

        return (short)(
            (this[index] << (BitsPerViscaNibble * 3)) |
            (this[index + 1] << (BitsPerViscaNibble * 2)) |
            (this[index + 2] << BitsPerViscaNibble) |
            this[index + 3]
        );
    }

    public override string ToString()
    {
        const int StringCharsPerFormattedByte = 3;
        const int HexCharsPerByte = 2;
        const char ByteSeparator = '-';

        if (cachedText is not null)
        {
            return cachedText;
        }

        var builder = new StringBuilder((Length * StringCharsPerFormattedByte) - 1);

        for (int i = 0; i < Length; i++)
        {
            if (i > 0)
            {
                builder.Append(ByteSeparator);
            }
            builder.AppendFormat($"{{0:x{HexCharsPerByte}}}", this[i]);
        }

        return builder.ToString();
    }

    private static ViscaPacket CreateFromBytes(byte[] bytes, int start, int length, bool preformatText)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, bytes.Length - MinimumPacketLength);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, MinimumPacketLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, MaximumPacketLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, bytes.Length - start);

        var head = PackBytesIntoLong(bytes, start, length, byteOffset: 0);
        var tail = PackBytesIntoLong(bytes, start, length, byteOffset: BytesPerSegment);
        var packet = new ViscaPacket(head, tail, length, cachedText: null);

        return preformatText
            ? new ViscaPacket(head, tail, length, packet.ToString())
            : packet;
    }

    private static long PackBytesIntoLong(byte[] bytes, int start, int length, int byteOffset)
    {
        long result = 0;

        for (int i = byteOffset; i < byteOffset + BytesPerSegment; i++)
        {
            if (i < length)
            {
                result |= ((long)bytes[start + i]) << (MostSignificantByteShift - ((i - byteOffset) * BitsPerByte));
            }
        }

        return result;
    }
}
