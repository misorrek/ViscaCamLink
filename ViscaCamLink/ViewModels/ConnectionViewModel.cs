namespace ViscaCamLink.ViewModels;

using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Resources;
using ViscaCamLink.Services;
using ViscaCamLink.Visca.Types;

public class ConnectionViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly ICameraConnectionService _connectionService;
    private readonly IPowerService _powerService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IDialogService _dialogService;

    private string _ip = string.Empty;
    private string _port = string.Empty;
    private bool _isEditingConnection;
    private ConnectionStatus _connectionStatus = ConnectionStatus.Failed;
    private string _connectionInfo = string.Empty;
    private PowerStatus _powerStatus = PowerStatus.Unknown;
    private bool _changingPowerStatus;
    private string _powerInfo = string.Empty;
    private string _activeCameraName = string.Empty;
    private bool _isSwitching;

    public ConnectionViewModel(
        ISettingsService settings,
        ICameraConnectionService connectionService,
        IPowerService powerService,
        IUiDispatcher uiDispatcher,
        IDialogService dialogService)
    {
        _settings = settings;
        _connectionService = connectionService;
        _powerService = powerService;
        _uiDispatcher = uiDispatcher;
        _dialogService = dialogService;

        RefreshFromActiveCamera();

        _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _powerService.PowerStatusChanged += OnPowerStatusChanged;
        _powerService.SwitchingPower += OnSwitchingPower;

        ConnectionEditCommand = new Command(ExecuteConnectionEdit);
        ReconnectCommand = new Command(ExecuteReconnect);
        PowerSwitchCommand = new Command(ExecutePowerSwitch);
        PrevCameraCommand = new Command(ExecutePrevCamera, CanCycleCamera);
        NextCameraCommand = new Command(ExecuteNextCamera, CanCycleCamera);
        ManageCamerasCommand = new Command(ExecuteManageCameras);
    }

    public ICommand ConnectionEditCommand { get; }

    public ICommand ReconnectCommand { get; }

    public ICommand PowerSwitchCommand { get; }

    public ICommand PrevCameraCommand { get; }

    public ICommand NextCameraCommand { get; }

    public ICommand ManageCamerasCommand { get; }

    public string Ip
    {
        get => _ip;
        set
        {
            _ip = value;
            NotifyPropertyChanged();
        }
    }

    public string Port
    {
        get => _port;
        set
        {
            _port = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsEditingConnection
    {
        get => _isEditingConnection;
        set
        {
            _isEditingConnection = value;
            NotifyPropertyChanged();
        }
    }

    public ConnectionStatus ConnectionStatus
    {
        get => _connectionStatus;
        set
        {
            _connectionStatus = value;
            UpdateConnectionInfo();
            NotifyPropertyChanged();
        }
    }

    public string ConnectionInfo
    {
        get => _connectionInfo;
        set
        {
            _connectionInfo = value;
            NotifyPropertyChanged();
        }
    }

    public PowerStatus PowerStatus
    {
        get => _powerStatus;
        set
        {
            _powerStatus = value;
            ChangingPowerStatus = false;
            UpdatePowerInfo();
            NotifyPropertyChanged();
        }
    }

    public bool ChangingPowerStatus
    {
        get => _changingPowerStatus;
        set
        {
            _changingPowerStatus = value;
            UpdatePowerInfo();
            NotifyPropertyChanged();
        }
    }

    public string PowerInfo
    {
        get => _powerInfo;
        set
        {
            _powerInfo = value;
            NotifyPropertyChanged();
        }
    }

    public string ActiveCameraName
    {
        get => _activeCameraName;
        private set
        {
            _activeCameraName = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsSwitching
    {
        get => _isSwitching;
        private set
        {
            _isSwitching = value;
            NotifyPropertyChanged();
            ((Command)PrevCameraCommand).Invalidate();
            ((Command)NextCameraCommand).Invalidate();
        }
    }

    public bool UseMultipleCameras => _settings.UseMultipleCameraProfiles;

    public void Initialize()
    {
        ExecuteReconnect();
    }

    public void CancelEditMode()
    {
        if (IsEditingConnection)
        {
            RefreshFromActiveCamera();
            IsEditingConnection = false;
        }
    }

    public void OnLanguageChanged()
    {
        UpdateConnectionInfo();
        UpdatePowerInfo();
    }

    public void RefreshMultipleCameraMode()
    {
        NotifyPropertyChanged(nameof(UseMultipleCameras));
        RefreshFromActiveCamera();
        ((Command)PrevCameraCommand).Invalidate();
        ((Command)NextCameraCommand).Invalidate();
    }

    private void RefreshFromActiveCamera()
    {
        var camera = _settings.ActiveCameraProfile;
        Ip = camera?.Ip ?? string.Empty;
        Port = camera?.Port.ToString() ?? string.Empty;
        ActiveCameraName = camera?.Name ?? string.Empty;
    }

    private void UpdateConnectionInfo()
    {
        ConnectionInfo = ConnectionStatus switch
        {
            ConnectionStatus.Failed => Strings.ConnectionStatus_Failed,
            ConnectionStatus.Working => Strings.ConnectionStatus_Working,
            ConnectionStatus.Ok => Strings.ConnectionStatus_Ok,
            _ => string.Empty
        };
    }

    private async void OnConnectionStatusChanged(object? sender, ConnectionStatus status)
    {
        await _uiDispatcher.InvokeAsync(() =>
        {
            ConnectionStatus = status;
            IsSwitching = _connectionService.IsSwitchingCameraProfile;
        });

        if (status == ConnectionStatus.Ok)
        {
            await TryCameraOperation(_powerService.RefreshPowerStatusAsync());
        }
        else
        {
            await _uiDispatcher.InvokeAsync(() => PowerStatus = PowerStatus.Unknown);
        }
    }

    private void OnPowerStatusChanged(object? sender, PowerStatus status)
    {
        _uiDispatcher.Post(() => PowerStatus = status);
    }

    private void OnSwitchingPower(object? sender, EventArgs e)
    {
        _uiDispatcher.Post(() => ChangingPowerStatus = true);
    }

    private void UpdatePowerInfo()
    {
        if (ChangingPowerStatus)
        {
            if (PowerStatus == PowerStatus.On)
            {
                PowerInfo = Strings.PowerStatus_SwitchingToStandby;
            }
            else if (PowerStatus == PowerStatus.Standby)
            {
                PowerInfo = Strings.PowerStatus_SwitchingOn;
            }
        }
        else
        {
            PowerInfo = PowerStatus switch
            {
                PowerStatus.Unknown => Strings.PowerStatus_Unknown,
                PowerStatus.On => Strings.PowerStatus_On,
                PowerStatus.Standby => Strings.PowerStatus_Standby,
                PowerStatus.InternalPowerCircuitError => Strings.PowerStatus_Error,
                _ => string.Empty
            };
        }
    }

    private void ExecuteConnectionEdit(object? parameter)
    {
        if (IsEditingConnection)
        {
            if (parameter is bool editingCanceled && editingCanceled)
            {
                RefreshFromActiveCamera();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(Port) && int.TryParse(Port, out var port))
                {
                    _connectionService.CommitConnectionSettings(Ip, port);
                }
            }

            IsEditingConnection = false;
        }
        else
        {
            IsEditingConnection = true;
        }
    }

    private async void ExecuteReconnect()
    {
        await _connectionService.ReconnectAsync();
    }

    private async void ExecutePowerSwitch()
    {
        await TryCameraOperation(_powerService.SwitchPowerAsync());
    }

    private bool CanCycleCamera() => !IsSwitching && _settings.CameraProfiles.Count > 1;

    private async void ExecutePrevCamera()
    {
        var target = GetAdjacentCamera(-1);
        if (target is null) return;
        IsSwitching = true;
        await _connectionService.SwitchCameraProfileAsync(target);
        RefreshFromActiveCamera();
        IsSwitching = false;
    }

    private async void ExecuteNextCamera()
    {
        var target = GetAdjacentCamera(+1);
        if (target is null) return;
        IsSwitching = true;
        await _connectionService.SwitchCameraProfileAsync(target);
        RefreshFromActiveCamera();
        IsSwitching = false;
    }

    private CameraProfile? GetAdjacentCamera(int direction)
    {
        var cameras = _settings.CameraProfiles;
        if (cameras.Count <= 1) return null;

        var currentIndex = cameras.ToList().FindIndex(c => c.Id == _settings.ActiveCameraProfileId);
        if (currentIndex < 0) return cameras[0];

        var nextIndex = (currentIndex + direction + cameras.Count) % cameras.Count;
        return cameras[nextIndex];
    }

    private void ExecuteManageCameras()
    {
        // TODO _dialogService.ShowCameraManagerDialog();
        RefreshFromActiveCamera();
        RefreshMultipleCameraMode();
    }
}
