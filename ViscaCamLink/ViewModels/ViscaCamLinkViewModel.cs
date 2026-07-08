namespace ViscaCamLink.ViewModels;

using System.Windows;
using System.Windows.Input;
using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Services;
using ViscaCamLink.Updater;

public class ViscaCamLinkViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IUpdateService _updateService;
    private readonly IDialogService _dialogService;

    private UpdateInfo? _lastUpdateInfo;

    public ViscaCamLinkViewModel(
        ISettingsService settings,
        ConnectionViewModel connection,
        PresetsViewModel presets,
        MovementViewModel movement,
        ZoomViewModel zoom,
        IUpdateService updateService,
        IDialogService dialogService)
    {
        _settings = settings;
        _updateService = updateService;
        _dialogService = dialogService;

        Connection = connection;
        Presets = presets;
        Movement = movement;
        Zoom = zoom;

        _updateService.UpdateAvailable += OnUpdateAvailable;
        TranslationSource.Instance.LanguageChanged += OnLanguageChanged;

        Movement.MovementStarted += Presets.NotifyMovementStarted;
        Movement.HomeExecuted += Presets.ClearPresetIndicator;
        Zoom.ZoomStarted += Presets.NotifyMovementStarted;

        SidebarCommand = new Command(ExecuteSidebar);
        UpdateCommand = new Command(OpenUpdateDialog);
        OptionsCommand = new Command(OpenOptions);

        Connection.Initialize();
    }

    public event Action? UpdateAvailable;

    public ConnectionViewModel Connection { get; }

    public PresetsViewModel Presets { get; }

    public MovementViewModel Movement { get; }

    public ZoomViewModel Zoom { get; }

    public ICommand SidebarCommand { get; }

    public ICommand UpdateCommand { get; }

    public ICommand OptionsCommand { get; }

    public bool ConnectionContainerVisible
    {
        get => _settings.ConnectionContainerVisible;
        set
        {
            _settings.ConnectionContainerVisible = value;
            NotifyPropertyChanged();
        }
    }

    public bool MemoryContainerVisible
    {
        get => _settings.MemoryContainerVisible;
        set
        {
            _settings.MemoryContainerVisible = value;
            NotifyPropertyChanged();
        }
    }

    public bool MoveContainerVisible
    {
        get => _settings.MoveContainerVisible;
        set
        {
            _settings.MoveContainerVisible = value;
            NotifyPropertyChanged();
        }
    }

    public bool ZoomContainerVisible
    {
        get => _settings.ZoomContainerVisible;
        set
        {
            _settings.ZoomContainerVisible = value;
            NotifyPropertyChanged();
        }
    }

    private void ExecuteSidebar(object? parameter)
    {
        if (parameter is string container)
        {
            switch (container)
            {
                case "Connection":
                    ConnectionContainerVisible = !ConnectionContainerVisible;
                    break;
                case "Memory":
                    MemoryContainerVisible = !MemoryContainerVisible;
                    break;
                case "Move":
                    MoveContainerVisible = !MoveContainerVisible;
                    break;
                case "Zoom":
                    ZoomContainerVisible = !ZoomContainerVisible;
                    break;
            }
        }
    }

    private void OnUpdateAvailable(object? sender, UpdateInfo info)
    {
        _lastUpdateInfo = info;
        Application.Current.Dispatcher.Invoke(() => UpdateAvailable?.Invoke());
    }

    private void OpenUpdateDialog()
    {
        if (_lastUpdateInfo is null)
            return;

        _dialogService.ShowUpdateDialog(_lastUpdateInfo);
    }

    private void OpenOptions()
    {
        Connection.CancelEditMode();
        Presets.CancelEditMode();
        _dialogService.ShowOptionsDialog();
        Connection.RefreshMultipleCameraMode();
        Presets.RefreshLayout();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Connection.OnLanguageChanged();
        Presets.OnLanguageChanged();
    }
}
