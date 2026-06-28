namespace ViscaCamLink.Updater;

/// <summary>
/// Describes an available update fetched from GitHub Releases.
/// </summary>
public record UpdateInfo(
    Version Version,
    string ReleaseNotes,
    string InstallerAssetUrl,
    string? PortableAssetUrl,
    string HtmlUrl);
