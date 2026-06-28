namespace ViscaCamLink.Updater;

public interface IGitHubUpdateChecker
{
    /// <summary>
    /// Checks GitHub Releases for an update newer than <paramref name="currentVersion"/>.
    /// Returns <see langword="null"/> when up to date, on network errors, or on malformed responses.
    /// </summary>
    Task<UpdateInfo?> CheckAsync(Version currentVersion, CancellationToken ct = default);
}
