namespace ViscaCamLink.ViewModels;

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using ViscaCamLink.Updater;
using ViscaCamLink.Util;

public class UpdateDownloadViewModel : INotifyPropertyChanged
{
    private readonly IUpdateDownloader _downloader;
    private readonly IInstallerLauncher _launcher;
    private readonly string _assetUrl;
    private readonly Action _closeHandler;

    private CancellationTokenSource _cts = new();
    private int _progress;
    private bool _isDownloading = true;
    private bool _hasError;
    private string _errorMessage = string.Empty;

    public UpdateDownloadViewModel(
        string assetUrl,
        IUpdateDownloader downloader,
        IInstallerLauncher launcher,
        Action closeHandler)
    {
        _assetUrl = assetUrl;
        _downloader = downloader;
        _launcher = launcher;
        _closeHandler = closeHandler;

        CancelCommand = new Command(ExecuteCancel);
        RetryCommand = new Command(ExecuteRetry);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand CancelCommand { get; }

    public ICommand RetryCommand { get; }

    public string AssetName => Path.GetFileName(_assetUrl);

    public int Progress
    {
        get => _progress;
        private set
        {
            var wasIndeterminate = IsIndeterminate;
            _progress = value;
            NotifyPropertyChanged();
            if (wasIndeterminate != IsIndeterminate)
                NotifyPropertyChanged(nameof(IsIndeterminate));
        }
    }

    public bool IsIndeterminate => IsDownloading && _progress == 0;

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            _isDownloading = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(IsIndeterminate));
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set
        {
            _hasError = value;
            NotifyPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            NotifyPropertyChanged();
        }
    }

    public async Task StartDownloadAsync()
    {
        IsDownloading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        Progress = 0;

        try
        {
            var progress = new Progress<int>(p => Progress = p);
            var filePath = await _downloader.DownloadAsync(
                _assetUrl,
                AssetName,
                progress,
                _cts.Token);

            IsDownloading = false;
            _launcher.Launch(filePath);
            Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            _closeHandler.Invoke();
        }
        catch (Exception ex)
        {
            IsDownloading = false;
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ExecuteCancel()
    {
        _cts.Cancel();
        _closeHandler.Invoke();
    }

    private void ExecuteRetry()
    {
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        _ = StartDownloadAsync();
    }
}
