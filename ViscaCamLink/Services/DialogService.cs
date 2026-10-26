namespace ViscaCamLink.Services;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure;
using ViscaCamLink.ViewModels;
using ViscaCamLink.Views;

public class DialogService(
    ISettingsService settingsService,
    IHotKeyService hotKeyService,
    IUpdateService updateService,
    IHintService hintService,
    ILogger<MigrationViewModel> migrationLogger,
    ILogger<UpdateDownloadViewModel> updateDownloadLogger) : IDialogService
{
    private const int VersionFieldCount = 3;

    public void ShowOptionsDialog()
    {
        var view = new OptionsView();
        var viewModel = new OptionsViewModel(
            settingsService,
            hotKeyService,
            hintService,
            GetAppVersion(),
            OpenLogsFolder,
            view.Close);

        view.DataContext = viewModel;
        view.ShowDialog();
    }

    public void ShowCameraProfilesDialog()
    {
        var view = new CameraProfilesView();
        var viewModel = new CameraProfilesViewModel(settingsService, this, view.Close);

        view.DataContext = viewModel;
        view.ShowDialog();
    }

    public void ShowUpdateDialog(UpdateInfo updateInfo)
    {
        var view = new UpdateView();
        var viewModel = new UpdateViewModel(updateInfo, updateService.InstalledVersion, view.Close, ShowUpdateDownloadDialog);

        view.DataContext = viewModel;
        view.ShowDialog();
    }

    public void ShowUpdateDownloadDialog(UpdateInfo updateInfo)
    {
        var view = new UpdateDownloadView();
        var viewModel = new UpdateDownloadViewModel(updateInfo, updateService, view.Close, updateDownloadLogger);

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

    public bool ShowConfirmation(string message, string title)
    {
        var owner = Application.Current.MainWindow;
        var result = owner is { IsVisible: true }
            ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
            : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

        return result == MessageBoxResult.Yes;
    }

    private string GetAppVersion() =>
        updateService.InstalledVersion?.ToString()
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString(VersionFieldCount)
        ?? string.Empty;

    private static void OpenLogsFolder()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogsDirectory);
            Process.Start(new ProcessStartInfo(AppPaths.LogsDirectory) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException)
        {
            // Opening Explorer is a convenience; the logs path is also written to the log at startup, so a failure is not surfaced.
        }
    }
}
