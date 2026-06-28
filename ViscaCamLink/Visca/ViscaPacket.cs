namespace ViscaCamLink.Visca;

using System.Text;

public readonly struct ViscaPacket
{
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

            return index < 8
                ? (byte)(head >> (56 - (index * 8)))
                : (byte)(tail >> (120 - (index * 8)));
        }
    }

    public static ViscaPacket FromBytes(byte[] bytes, int start, int length) =>
        CreateFromBytes(bytes, start, length, preformatText: false);

    public static ViscaPacket FromBytesWithPreformatting(params byte[] bytes) =>
        CreateFromBytes(bytes, start: 0, bytes.Length, preformatText: true);

    public byte GetByte(int index) => this[index];

    public short GetInt16(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Length - 4);

        return (short)(
            (this[index] << 12) |
            (this[index + 1] << 8) |
            (this[index + 2] << 4) |
            this[index + 3]
        );
    }

    public override string ToString()
    {
        if (cachedText is not null)
        {
            return cachedText;
        }

        var builder = new StringBuilder(Length * 3 - 1);

        for (int i = 0; i < Length; i++)
        {
            if (i > 0)
            {
                builder.Append('-');
            }
            builder.AppendFormat("{0:x2}", this[i]);
        }

        return builder.ToString();
    }

    private static ViscaPacket CreateFromBytes(byte[] bytes, int start, int length, bool preformatText)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, bytes.Length - 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, bytes.Length - start);

        var head = PackBytesIntoLong(bytes, start, length, byteOffset: 0);
        var tail = PackBytesIntoLong(bytes, start, length, byteOffset: 8);
        var packet = new ViscaPacket(head, tail, length, cachedText: null);

        return preformatText
            ? new ViscaPacket(head, tail, length, packet.ToString())
            : packet;
    }

    private static long PackBytesIntoLong(byte[] bytes, int start, int length, int byteOffset)
    {
        long result = 0;

        for (int i = byteOffset; i < byteOffset + 8; i++)
        {
            if (i < length)
            {
                result |= ((long)bytes[start + i]) << (56 - ((i - byteOffset) * 8));
            }
        }

        return result;
    }
}
