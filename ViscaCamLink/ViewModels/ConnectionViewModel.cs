namespace ViscaCamLink.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Repositories.HotKeys;
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
    private ConnectionStatus _connectionStatus = ConnectionStatus.Failed;
    private string _connectionInfo = string.Empty;
    private PowerStatus _powerStatus = PowerStatus.Unknown;
    private bool _changingPowerStatus;
    private string _powerInfo = string.Empty;
    private string _activeCameraName = string.Empty;

    public ConnectionViewModel(
        ISettingsService settings,
        ICameraConnectionService connectionService,
        IPowerService powerService,
        IUiDispatcher uiDispatcher,
        IDialogService dialogService,
        IHotKeyService hotKeyService)
    {
        _settings = settings;
        _connectionService = connectionService;
        _powerService = powerService;
        _uiDispatcher = uiDispatcher;
        _dialogService = dialogService;

        hotKeyService.RegisterActions(CreateHotKeyActions());

        RefreshFromActiveCamera();

        _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _powerService.PowerStatusChanged += OnPowerStatusChanged;
        _powerService.SwitchingPower += OnSwitchingPower;

        ReconnectCommand = new Command(ExecuteReconnect);
        PowerSwitchCommand = new Command(ExecutePowerSwitch);
        PrevCameraCommand = new Command(ExecutePrevCamera, CanCycleCamera);
        NextCameraCommand = new Command(ExecuteNextCamera, CanCycleCamera);
        ManageCamerasCommand = new Command(ExecuteManageCameras);
    }

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

    public void Initialize()
    {
        ExecuteReconnect();
    }

    public void OnLanguageChanged()
    {
        UpdateConnectionInfo();
        UpdatePowerInfo();
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

    private async void OnConnectionStatusChanged(object? sender, ConnectionStatus status)
    {
        await _uiDispatcher.InvokeAsync(() => ConnectionStatus = status);

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

    private void OnSwitchingPower(object? sender, EventArgs eventArgs)
    {
        _uiDispatcher.Post(() => ChangingPowerStatus = true);
    }

    private async void ExecuteReconnect()
    {
        await _connectionService.ReconnectAsync();
    }

    private async void ExecutePowerSwitch()
    {
        await TryCameraOperation(_powerService.SwitchPowerAsync());
    }

    private bool CanCycleCamera() => _settings.CameraProfiles.Count > 1;

    private async void ExecutePrevCamera()
    {
        var target = GetAdjacentCamera(-1);

        if (target is null)
        {
            return;
        }

        await SwitchToCameraAsync(target);
    }

    private async void ExecuteNextCamera()
    {
        var target = GetAdjacentCamera(+1);

        if (target is null)
        {
            return;
        }

        await SwitchToCameraAsync(target);
    }

    private async Task SwitchToCameraAsync(CameraProfile target)
    {
        // Update the UI immediately so the profile label changes while the user is cycling.
        // The actual connection is debounced and performed on a background thread by the connection service.
        // A newer request cancels any pending attempt.
        Ip = target.Ip;
        Port = target.Port.ToString();
        ActiveCameraName = target.Name;

        try
        {
            await _connectionService.SwitchCameraProfileAsync(target);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer switch request; the UI already reflects the latest profile.
        }

        RefreshFromActiveCamera();
    }

    private CameraProfile? GetAdjacentCamera(int direction)
    {
        var cameras = _settings.CameraProfiles;

        if (cameras.Count <= 1)
        {
            return null;
        }

        var currentIndex = cameras.ToList().FindIndex(camera => camera.Id == _settings.ActiveCameraProfileId);

        if (currentIndex < 0)
        {
            return cameras[0];
        }

        var nextIndex = (currentIndex + direction + cameras.Count) % cameras.Count;

        return cameras[nextIndex];
    }

    private IEnumerable<HotKeyActionRegistration> CreateHotKeyActions()
    {
        yield return new HotKeyActionRegistration(HotKeyAction.CameraProfilePrevious, () =>
        {
            if (CanCycleCamera())
            {
                ExecutePrevCamera();
            }
        });
        yield return new HotKeyActionRegistration(HotKeyAction.CameraProfileNext, () =>
        {
            if (CanCycleCamera())
            {
                ExecuteNextCamera();
            }
        });
    }

    private void ExecuteManageCameras()
    {
        _dialogService.ShowCameraProfilesDialog();
        RefreshFromActiveCamera();

        ((Command)PrevCameraCommand).Invalidate();
        ((Command)NextCameraCommand).Invalidate();
    }
}
