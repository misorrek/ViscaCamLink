namespace ViscaCamLink.Services;

using Velopack;

public record UpdateInfo(
    SemanticVersion Version,
    string ReleaseNotes,
    string HtmlUrl);
