namespace ViscaCamLink.Infrastructure;

using System;
using System.IO;

public static class AppPaths
{
    private static readonly string RoamingBase = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ViscaCamLink");

    private static readonly string LocalBase = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ViscaCamLink.App");

    private static readonly string LegacyLocalBase = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ViscaCamLink");

    public static string SettingsFile => Path.Combine(RoamingBase, "settings.json");

    public static string LogsDirectory => Path.Combine(LocalBase, "logs");

    public static string PresetsDirectory => Path.Combine(RoamingBase, "presets");

    public static string HotKeysFile => Path.Combine(RoamingBase, "hotkeys.json");

    // ViscaCamLink 0.3.0 wrote user.config here via .NET Framework Properties.Settings.
    // Path used to migrate settings from there. Remove with 1.1.0 or later.
    public static string LegacyUserDataRoot => LegacyLocalBase;
}
