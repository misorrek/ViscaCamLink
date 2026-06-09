namespace ViscaCamLink.Updater.Util.Extensions;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public static class StreamExtensions
{
    private static readonly Int32 OptimalFileStreamBufferSize = 131072; // 128 KiB

    public static async Task CopyToAsync(this Stream source, Stream destination, IProgress<Int64>? progress = null, CancellationToken cancellationToken = default) 
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (!source.CanRead)
        {
            throw new ArgumentException($"Has to be readable", nameof(source));
        }

        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }
            
        if (!destination.CanWrite)
        {
            throw new ArgumentException("Has to be writable", nameof(destination));
        }
                  
        var buffer = new byte[OptimalFileStreamBufferSize];
        Int64 totalBytesRead = 0;
        Int32 bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);

            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }
}
