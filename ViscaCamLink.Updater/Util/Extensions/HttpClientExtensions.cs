namespace ViscaCamLink.Updater.Util.Extensions;

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

public static class HttpClientExtensions
{
    public static async Task<HttpContentHeaders> DownloadFileAndGetContentHeaderAsync(this HttpClient client, String? requestUri, Stream destination, IProgress<Double>? progress = null, CancellationToken cancellationToken = default)
    {
        using (var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            var contentLenght = response.Content.Headers.ContentLength;

            using (var content = await response.Content.ReadAsStreamAsync(cancellationToken))
            { 
                if (progress == null || !contentLenght.HasValue)
                { 
                    await content.CopyToAsync(destination, cancellationToken);                    
                }
                else 
                {
                    var relativeProgress = new Progress<Int64>(bytesWritten => progress.Report(GetBytesWrittenAsPercentage(bytesWritten, contentLenght.Value)));

                    await content.CopyToAsync(destination, relativeProgress, cancellationToken);
                    progress.Report(1);
                }                
            }

            return response.Content.Headers;
        }
    }

    private static Double GetBytesWrittenAsPercentage(Int64 bytesWritten, Int64 totalBytes) 
    { 
        return (double)bytesWritten / totalBytes;
    }
}
