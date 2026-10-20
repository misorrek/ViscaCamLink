namespace ViscaCamLink.Services;

using System;

public record UpdateInfo(
    Version Version,
    string ReleaseNotes,
    string HtmlUrl);
