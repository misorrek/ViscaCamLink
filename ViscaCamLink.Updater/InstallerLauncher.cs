namespace ViscaCamLink.Updater;

using System.Diagnostics;
using System.IO.Compression;

public sealed class InstallerLauncher : IInstallerLauncher
{
    private const string AppExeName = "ViscaCamLink.exe";

    public void Launch(string filePath)
    {
        try
        {
            if (filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                LaunchPortable(filePath);
            }
            else
            {
                LaunchInstaller(filePath);
            }
        }
        finally
        {
            TryDelete(filePath);
        }
    }

    private static void LaunchInstaller(string exePath)
    {
        Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = true
        });
    }

    private static void LaunchPortable(string zipPath)
    {
        var extractDir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(zipPath));

        if (Directory.Exists(extractDir))
            Directory.Delete(extractDir, recursive: true);

        ZipFile.ExtractToDirectory(zipPath, extractDir);

        var exe = Path.Combine(extractDir, AppExeName);
        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best effort */ }
    }
}
