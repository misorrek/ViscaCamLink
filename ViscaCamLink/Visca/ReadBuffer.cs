namespace ViscaCamLink.Visca;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class ReadBuffer
{
    private const int MaxBufferSize = 256;

    private readonly byte[] _buffer = new byte[MaxBufferSize];

    private int _bytesInBuffer;

    public void Clear()
    {
        _bytesInBuffer = 0;
    }

    public async Task<ViscaPacket> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (FindTerminatorPosition() is int packetLength)
            {
                return ConsumePacket(packetLength);
            }

            if (_bytesInBuffer == _buffer.Length)
            {
                throw new ViscaProtocolException($"Read {_bytesInBuffer} bytes without reaching the end of a VISCA packet");
            }

            using var streamCloseOnCancel = cancellationToken.Register(stream.Close);
            var bytesRead = await stream.ReadAsync(_buffer.AsMemory(_bytesInBuffer, _buffer.Length - _bytesInBuffer), cancellationToken).ConfigureAwait(false);

            if (bytesRead == 0)
            {
                throw new ViscaProtocolException("Reached end of VISCA stream");
            }

            _bytesInBuffer += bytesRead;
        }
    }

    private int? FindTerminatorPosition()
    {
        for (var i = 0; i < _bytesInBuffer; i++)
        {
            if (_buffer[i] == ViscaProtocol.Terminator)
            {
                return i + 1;
            }
        }

        return null;
    }

    private ViscaPacket ConsumePacket(int packetLengthIncludingTerminator)
    {
        var packetDataLength = packetLengthIncludingTerminator - 1;
        var packet = ViscaPacket.FromBytes(_buffer, 0, packetDataLength);

        _bytesInBuffer -= packetLengthIncludingTerminator;

        if (_bytesInBuffer > 0)
        {
            Buffer.BlockCopy(_buffer, packetLengthIncludingTerminator, _buffer, 0, _bytesInBuffer);
        }

        return packet;
    }
}
