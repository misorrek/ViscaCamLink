namespace ViscaCamLink.Visca;

using System.IO;

public class ReadBuffer
{
    private const int MaxBufferSize = 256;
    private const byte PacketTerminator = 0xff;

    private readonly byte[] buffer = new byte[MaxBufferSize];

    private int bytesInBuffer;

    public void Clear()
    {
        bytesInBuffer = 0;
    }

    public async Task<ViscaPacket> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (FindTerminatorPosition() is int packetLength)
            {
                return ConsumePacket(packetLength);
            }

            if (bytesInBuffer == buffer.Length)
            {
                throw new ViscaProtocolException($"Read {bytesInBuffer} bytes without reaching the end of a VISCA packet");
            }

            using var streamCloseOnCancel = cancellationToken.Register(stream.Close);
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(bytesInBuffer, buffer.Length - bytesInBuffer), cancellationToken).ConfigureAwait(false);

            if (bytesRead == 0)
            {
                throw new ViscaProtocolException("Reached end of VISCA stream");
            }

            bytesInBuffer += bytesRead;
        }
    }

    private int? FindTerminatorPosition()
    {
        for (int i = 0; i < bytesInBuffer; i++)
        {
            if (buffer[i] == PacketTerminator)
            {
                return i + 1;
            }
        }
        return null;
    }

    private ViscaPacket ConsumePacket(int packetLengthIncludingTerminator)
    {
        int packetDataLength = packetLengthIncludingTerminator - 1;
        var packet = ViscaPacket.FromBytes(buffer, 0, packetDataLength);

        bytesInBuffer -= packetLengthIncludingTerminator;

        if (bytesInBuffer > 0)
        {
            Buffer.BlockCopy(buffer, packetLengthIncludingTerminator, buffer, 0, bytesInBuffer);
        }

        return packet;
    }
}
