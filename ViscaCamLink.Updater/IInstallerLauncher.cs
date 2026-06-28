namespace ViscaCamLink.Updater;

public interface IInstallerLauncher
{
    /// <summary>
    /// Launches <paramref name="filePath"/>. Handles <c>.exe</c> (UAC elevation via shell execute)
    /// and <c>.zip</c> (extracts to a sibling folder, then starts <c>ViscaCamLink.exe</c>).
    /// The temp file is deleted after launch.
    /// </summary>
    void Launch(string filePath);
}
