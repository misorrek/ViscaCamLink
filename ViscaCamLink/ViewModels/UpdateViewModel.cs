namespace ViscaCamLink.ViewModels;

using System;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Resources;
using ViscaCamLink.Services;

public class UpdateViewModel : ViewModelBase
{
    private readonly UpdateInfo _updateInfo;
    private readonly Version? _installedVersion;
    private readonly Action _closeHandler;
    private readonly Action<UpdateInfo> _showUpdateDownloadDialog;

    public UpdateViewModel(
        UpdateInfo updateInfo,
        Version? installedVersion,
        Action closeHandler,
        Action<UpdateInfo> showUpdateDownloadDialog)
    {
        _updateInfo = updateInfo;
        _installedVersion = installedVersion;
        _closeHandler = closeHandler;
        _showUpdateDownloadDialog = showUpdateDownloadDialog;

        UpdateCommand = new Command(ExecuteUpdate);
        CancelCommand = new Command(ExecuteCancel);
    }

    public ICommand UpdateCommand { get; }

    public ICommand CancelCommand { get; }

    public string VersionText => $"v{_updateInfo.Version} ({Strings.Updater_CurrentVersion} v{_installedVersion})";

    public string ChangelogUrl => _updateInfo.HtmlUrl;

    private void ExecuteUpdate()
    {
        _closeHandler.Invoke();
        _showUpdateDownloadDialog.Invoke(_updateInfo);
    }

    private void ExecuteCancel()
    {
        _closeHandler.Invoke();
    }
}
