namespace ViscaCamLink.Services;

using System.Reflection;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure;
using ViscaCamLink.ViewModels;
using ViscaCamLink.Views;

public class DialogService(
    ISettingsService settingsService,
    IHotKeyService hotKeyService,
    IUpdateService updateService) : IDialogService
    ILogger<MigrationViewModel> migrationLogger,
    ILogger<UpdateDownloadViewModel> updateDownloadLogger) : IDialogService
{
    public void ShowOptionsDialog()
    {
        var view = new OptionsView();
        var viewModel = new OptionsViewModel(settingsService, hotKeyService, view.Close);

        view.DataContext = viewModel;
        view.ShowDialog();
    }

    public void ShowCameraProfilesDialog()
    {
        var view = new CameraProfilesView();
        var viewModel = new CameraProfilesViewModel(settingsService, view.Close);

        view.DataContext = viewModel;
        view.ShowDialog();
    }

    public void ShowUpdateDialog(UpdateInfo updateInfo)
    {
        var installedVersion = Assembly.GetEntryAssembly()?.GetName().Version;
        var view = new UpdateView();
        var viewModel = new UpdateViewModel(updateInfo, installedVersion, view.Close, ShowUpdateDownloadDialog);

        view.DataContext = viewModel;
        view.ShowDialog();
    }

    public void ShowUpdateDownloadDialog(UpdateInfo updateInfo)
    {
        var view = new UpdateDownloadView();
        var viewModel = new UpdateDownloadViewModel(updateInfo, updateService, view.Close);

        view.DataContext = viewModel;
        view.ShowDialog();
    }

    public void ShowMigrationDialog(string uninstallString)
    {
        var view = new MigrationView();
        var viewModel = new MigrationViewModel(uninstallString, view.Close, migrationLogger);

        view.DataContext = viewModel;
        view.ShowDialog();
    }
}
