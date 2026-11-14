namespace ViscaCamLink.ViewModels;

using System;
using System.Globalization;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Abstractions;
using ViscaCamLink.Resources;
using ViscaCamLink.Services;

public class UpdateViewModel : ViewModelBase
{
    private readonly UpdateInfo _updateInfo;
    private readonly Version? _installedVersion;
    private readonly Action _closeHandler;
    private readonly Action<UpdateInfo> _showUpdateDownloadDialog;
    private readonly string _englishChangelogUrl;
    private readonly string _germanChangelogUrl;
    private string _changelogUrl;

    public UpdateViewModel(
        UpdateInfo updateInfo,
        Version? installedVersion,
        Action closeHandler,
        Action<UpdateInfo> showUpdateDownloadDialog,
        CultureInfo culture)
    {
        _updateInfo = updateInfo;
        _installedVersion = installedVersion;
        _closeHandler = closeHandler;
        _showUpdateDownloadDialog = showUpdateDownloadDialog;

        var version = Uri.EscapeDataString(updateInfo.Version.ToFullString());
        var notesUrl = $"{VelopackUpdateManager.RepositoryUrl}/blob/v{version}/Docs/RELEASE-NOTES-{version}";
        _englishChangelogUrl = $"{notesUrl}.md";
        _germanChangelogUrl = $"{notesUrl}-de.md";
        _changelogUrl = culture.TwoLetterISOLanguageName == "de" ? _germanChangelogUrl : _englishChangelogUrl;

        UpdateCommand = new Command(ExecuteUpdate);
        CancelCommand = new Command(ExecuteCancel);
    }

    public ICommand UpdateCommand { get; }

    public ICommand CancelCommand { get; }

    public string VersionText => $"v{_updateInfo.Version.ToFullString()} ({Strings.Updater_CurrentVersion} v{_installedVersion})";

    public string ChangelogUrl => _changelogUrl;

    public bool IsChangelogUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.GetLeftPart(UriPartial.Path) == _englishChangelogUrl
                || uri.GetLeftPart(UriPartial.Path) == _germanChangelogUrl);
    }

    public void ShowReleasePage()
    {
        _changelogUrl = _updateInfo.HtmlUrl;
        NotifyPropertyChanged(nameof(ChangelogUrl));
    }

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
