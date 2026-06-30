namespace ViscaCamLink.Util;

using System.IO;

public static class AppPaths
{
    private static readonly string AppDataBase = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ViscaCamLink");

    public static string AppData => AppDataBase;

    public static string Settings => Path.Combine(AppDataBase, "settings.json");

    public static string Logs => Path.Combine(AppDataBase, "logs");

    public static string Presets => Path.Combine(AppDataBase, "presets.json");

    public static string HotKeys => Path.Combine(AppDataBase, "hotkeys.json");
}
