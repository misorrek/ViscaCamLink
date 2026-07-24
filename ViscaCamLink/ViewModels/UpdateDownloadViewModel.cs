namespace ViscaCamLink.ViewModels;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Services;
using ViscaCamLink.Updater;

public class UpdateDownloadViewModel : ViewModelBase
{
    private readonly UpdateInfo _updateInfo;
    private readonly IUpdateService _updateService;
    private readonly Action _closeHandler;

    private CancellationTokenSource _downloadCancellation = new();
    private int _progress;
    private bool _isDownloading = true;
    private bool _hasError;
    private string _errorMessage = string.Empty;

    public UpdateDownloadViewModel(UpdateInfo updateInfo, IUpdateService updateService, Action closeHandler)
    {
        _updateInfo = updateInfo;
        _updateService = updateService;
        _closeHandler = closeHandler;

        CancelCommand = new Command(ExecuteCancel);
        RetryCommand = new Command(ExecuteRetry);
    }

    public ICommand CancelCommand { get; }

    public ICommand RetryCommand { get; }

    public string VersionText => $"v{_updateInfo.Version}";

    public int Progress
    {
        get => _progress;
        private set
        {
            var wasIndeterminate = IsIndeterminate;

            _progress = value;

            NotifyPropertyChanged();

            if (wasIndeterminate != IsIndeterminate)
            {
                NotifyPropertyChanged(nameof(IsIndeterminate));
            }
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
            var progress = new Progress<int>(percent => Progress = percent);

            // On success the update service applies the update and restarts the application.
            // No code after this call is reached.
            await _updateService.DownloadAndApplyAsync(progress, _downloadCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            _closeHandler.Invoke();
        }
        catch (Exception exception)
        {
            IsDownloading = false;
            HasError = true;
            ErrorMessage = exception.Message;
        }
    }

    private void ExecuteCancel()
    {
        _downloadCancellation.Cancel();
        _closeHandler.Invoke();
    }

    private void ExecuteRetry()
    {
        _downloadCancellation.Dispose();
        _downloadCancellation = new CancellationTokenSource();

        _ = StartDownloadAsync();
    }
}
