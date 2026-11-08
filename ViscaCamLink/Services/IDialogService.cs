namespace ViscaCamLink.Services;

public interface IDialogService
{
    void ShowOptionsDialog();

    void ShowCameraProfilesDialog();

    void ShowUpdateDialog(UpdateInfo updateInfo);

    void ShowUpdateDownloadDialog(UpdateInfo updateInfo);

    void ShowMigrationDialog(string uninstallString);

    bool ShowConfirmation(string message, string title);

    void ShowInformation(string message, string title);

    void ShowWarning(string message, string title);

    void ShowError(string message, string title);
}
