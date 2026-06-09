namespace ViscaCamLink.Updater.ViewModels;

using System;
using System.IO;
using System.Threading;
using System.Windows.Input;

using ViscaCamLink.Common;
using ViscaCamLink.Updater.Common;
using ViscaCamLink.Updater.Manager;

public class UpdaterViewModel : BaseViewModel
{
    public UpdaterViewModel(
        IDownloadManager downloadManager,
        IApplicationUpdaterOptions applicationUpdaterOptions,
        UpdateInfoEventArgs updateInfoEventArgs        
        ) : base()
    {
        _downloadManager = downloadManager;
        _updaterOptions = applicationUpdaterOptions;
        _updateInfoEventArgs = updateInfoEventArgs;       
        _cancellationTokenSource = new CancellationTokenSource();

        _progressText = String.Empty;

        UpdateCommand = new Command(ExecuteUpdate);
        CancelCommand = new Command(ExecuteCancel);        
    }

    #region Commands

    public ICommand UpdateCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    #region Bindable properties

    public String VersionText => $"{_updateInfoEventArgs.AvailableVersion} (Aktuell: {_updateInfoEventArgs.InstalledVersion})";

    public String ChangelogUrl => _updateInfoEventArgs.ChangelogUrl ?? String.Empty;

    public Boolean UpdateStarted
    {
        get => _updateStarted;

        set
        {
            _updateStarted = value;
            NotifyPropertyChanged();
        }
    }

    private Boolean _updateStarted;

    public Double ProgressInPercent
    {
        get => _progressInPercent;

        set
        {
            _progressInPercent = value;
            NotifyPropertyChanged();
        }
    }

    private Double _progressInPercent;

    public String ProgressText
    {
        get => _progressText;

        set
        {
            _progressText = value;
            NotifyPropertyChanged();
        }
    }

    private String _progressText;

    #endregion

    private readonly IDownloadManager _downloadManager;

    private readonly IApplicationUpdaterOptions _updaterOptions;

    private readonly UpdateInfoEventArgs _updateInfoEventArgs;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private void ExecuteUpdate()
    {
        UpdateStarted = true;

        var downloadedFilePath = _downloadManager.Download(
            _updateInfoEventArgs.DownloadUrl,
            _updaterOptions.DownloadPath,
            CreateProgress(),
            _cancellationTokenSource.Token).Result;
        var downloadFileExtension = Path.GetExtension(downloadedFilePath);

        if (downloadFileExtension == null)
        {
            //TODO ErrorHandling
            return;
        }

        if (downloadFileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            //_zipExtractionManager.
        }
        else if (downloadFileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
        {

        }

       /* if (AutoUpdater.DownloadUpdate(UpdateInfoEventArgs))
        {
            //TODO CloseHandler.Invoke();
            Application.Current.MainWindow.Close();
        }*/
    }
    private void ExecuteCancel()
    {
        _cancellationTokenSource.Cancel();
        RequestCloseDialog?.Invoke();
    }

    private IProgress<Double> CreateProgress()
    {
        var progress = new Progress<Double>();

        progress.ProgressChanged += (_, progressValue) => ProgressInPercent = progressValue;

        return progress;
    }
}
