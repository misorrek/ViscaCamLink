namespace ViscaCamLink.Updater.Common;

using System;

public class UpdateInfoEventArgs : EventArgs
{
    public UpdateInfoEventArgs(UpdateXml updateXml, Version? installedVersion)
    {
        UpdateXml = updateXml;

        if (String.IsNullOrEmpty(updateXml.DownloadUrl) || String.IsNullOrEmpty(updateXml.Version))
        {
            throw new MissingFieldException();
        }

        InstalledVersion = installedVersion;
        IsUpdateAvailable = new Version(AvailableVersion) > InstalledVersion;
    }

    public Boolean IsUpdateAvailable { get; }

    public Boolean IsMandatory => UpdateXml.Mandatory?.Value ?? false;    

    public String DownloadUrl => UpdateXml.DownloadUrl!;

    public String? ChangelogUrl => UpdateXml.ChangelogUrl;

    public String AvailableVersion => UpdateXml.Version!;

    public Version? InstalledVersion { get; }

    public String? ExecutablePath => UpdateXml.ExecutablePath;

    public Exception? Error { get; set; }

    private UpdateXml UpdateXml { get; }
}
