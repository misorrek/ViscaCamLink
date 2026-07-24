namespace ViscaCamLink.ViewModels;

using System;
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
    private readonly IUiDispatcher _uiDispatcher;

    private UpdateInfo? _lastUpdateInfo;

    public ViscaCamLinkViewModel(
        ISettingsService settings,
        ConnectionViewModel connection,
        PresetsViewModel presets,
        MovementViewModel movement,
        ZoomViewModel zoom,
        IUpdateService updateService,
        IDialogService dialogService,
        IUiDispatcher uiDispatcher)
    {
        _settings = settings;
        _updateService = updateService;
        _dialogService = dialogService;
        _uiDispatcher = uiDispatcher;

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
        if (parameter is not SidebarContainer container)
        {
            return;
        }

        switch (container)
        {
            case SidebarContainer.Connection:
                ConnectionContainerVisible = ToggleContainer(ConnectionContainerVisible);
                break;
            case SidebarContainer.Memory:
                MemoryContainerVisible = ToggleContainer(MemoryContainerVisible);
                break;
            case SidebarContainer.Move:
                MoveContainerVisible = ToggleContainer(MoveContainerVisible);
                break;
            case SidebarContainer.Zoom:
                ZoomContainerVisible = ToggleContainer(ZoomContainerVisible);
                break;
        }
    }

    private bool ToggleContainer(bool currentVisibility)
    {
        // At least one container must stay visible.
        if (currentVisibility && CountVisibleContainers() <= 1)
        {
            return true;
        }

        return !currentVisibility;
    }

    private int CountVisibleContainers()
    {
        var count = 0;

        if (ConnectionContainerVisible) count++;
        if (MemoryContainerVisible) count++;
        if (MoveContainerVisible) count++;
        if (ZoomContainerVisible) count++;

        return count;
    }

    private void OnUpdateAvailable(object? sender, UpdateInfo updateInfo)
    {
        _lastUpdateInfo = updateInfo;

        _uiDispatcher.Post(() => UpdateAvailable?.Invoke());
    }

    private void OpenUpdateDialog()
    {
        if (_lastUpdateInfo is null)
        {
            return;
        }

        _dialogService.ShowUpdateDialog(_lastUpdateInfo);
    }

    private void OpenOptions()
    {
        Presets.CancelEditMode();
        _dialogService.ShowOptionsDialog();
        Presets.RefreshLayout();
    }

    private void OnLanguageChanged(object? sender, EventArgs eventArgs)
    {
        Connection.OnLanguageChanged();
        Presets.OnLanguageChanged();
    }
}
