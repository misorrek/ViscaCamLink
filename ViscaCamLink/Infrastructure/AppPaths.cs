namespace ViscaCamLink.Infrastructure;

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

    private static readonly string PresetsBase = Path.Combine(RoamingBase, "presets");

    public static string Settings => Path.Combine(RoamingBase, "settings.json");

    public static string PresetsForCamera(Guid cameraId) => Path.Combine(PresetsBase, $"presets-{cameraId:N}.json");

    public static string HotKeys => Path.Combine(RoamingBase, "hotkeys.json");

    public static string Logs => Path.Combine(LocalBase, "logs");

    // ViscaCamLink 0.3.0 wrote user.config here via .NET Framework Properties.Settings.
    // Path used to migrate settings from there.
    public static string LegacyUserDataRoot => LegacyLocalBase;
}
