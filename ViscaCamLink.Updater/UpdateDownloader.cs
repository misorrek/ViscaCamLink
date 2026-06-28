namespace ViscaCamLink.Updater;

public sealed class UpdateDownloader : IUpdateDownloader
{
    private const int BufferSize = 81920; // 80 KB

    private readonly HttpClient _http;

    public UpdateDownloader(HttpClient http) => _http = http;

    public async Task<string> DownloadAsync(
        string url,
        string fileName,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            return await DownloadCoreAsync(url, fileName, progress, ct).ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            // Content-Length mismatch on first attempt — auto-retry once.
            TryDelete(Path.Combine(Path.GetTempPath(), fileName));
            return await DownloadCoreAsync(url, fileName, progress, ct).ConfigureAwait(false);
        }
    }

    private async Task<string> DownloadCoreAsync(
        string url,
        string fileName,
        IProgress<int>? progress,
        CancellationToken ct)
    {
        using var response = await _http
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var destPath   = Path.Combine(Path.GetTempPath(), fileName);
        var totalBytes = response.Content.Headers.ContentLength;

        await using var src  = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var dest = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var  buffer    = new byte[BufferSize];
        long bytesRead = 0;
        int  read;

        while ((read = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await dest.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            bytesRead += read;

            if (progress != null && totalBytes > 0)
                progress.Report((int)(bytesRead * 100 / totalBytes));
        }

        if (totalBytes.HasValue && bytesRead != totalBytes.Value)
            throw new InvalidDataException(
                $"Download corrupt: expected {totalBytes.Value} bytes but received {bytesRead}.");

        return destPath;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best effort */ }
    }
}
