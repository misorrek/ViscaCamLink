namespace ViscaCamLink.Updater;

public interface IUpdateDownloader
{
    /// <summary>
    /// Downloads <paramref name="url"/> to the temp folder using <paramref name="fileName"/>.
    /// Reports integer progress (0–100) via <paramref name="progress"/> when Content-Length is known.
    /// </summary>
    /// <returns>Absolute path of the downloaded temp file.</returns>
    Task<string> DownloadAsync(
        string url,
        string fileName,
        IProgress<int>? progress = null,
        CancellationToken ct = default);
}
