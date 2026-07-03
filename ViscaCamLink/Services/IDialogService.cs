namespace ViscaCamLink.Services;

using ViscaCamLink.Updater;

public interface IDialogService
{
    void ShowOptionsDialog();

    void ShowCameraProfilesDialog();

    void ShowUpdateDialog(UpdateInfo updateInfo);

    void ShowUpdateDownloadDialog(UpdateInfo updateInfo);

    void ShowMigrationDialog(string uninstallString);
}
