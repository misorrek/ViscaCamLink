namespace ViscaCamLink.Services;

using System;

public record UpdateInfo(
    Version Version,
    string ReleaseNotes,
    string InstallerAssetUrl,
    string? PortableAssetUrl,
    string HtmlUrl);
