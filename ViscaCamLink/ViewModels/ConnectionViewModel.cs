namespace ViscaCamLink.ViewModels;

using System.Windows.Input;
using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Resources;
using ViscaCamLink.Services;
using ViscaCamLink.Visca.Types;

public class ConnectionViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly ICameraConnectionService _connectionService;
    private readonly IPowerService _powerService;
    private readonly IUiDispatcher _uiDispatcher;

    private string _ip = string.Empty;
    private string _port = string.Empty;
    private bool _isEditingConnection;
    private ConnectionStatus _connectionStatus = ConnectionStatus.Failed;
    private string _connectionInfo = string.Empty;
    private PowerStatus _powerStatus = PowerStatus.Unknown;
    private bool _changingPowerStatus;
    private string _powerInfo = string.Empty;

    public ConnectionViewModel(
        ISettingsService settings,
        ICameraConnectionService connectionService,
        IPowerService powerService,
        IUiDispatcher uiDispatcher)
    {
        _settings = settings;
        _connectionService = connectionService;
        _powerService = powerService;
        _uiDispatcher = uiDispatcher;

        _ip = _settings.Ip;
        _port = _settings.Port.ToString();

        _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _powerService.PowerStatusChanged += OnPowerStatusChanged;
        _powerService.SwitchingPower += OnSwitchingPower;

        ConnectionEditCommand = new Command(ExecuteConnectionEdit);
        ReconnectCommand = new Command(ExecuteReconnect);
        PowerSwitchCommand = new Command(ExecutePowerSwitch);
    }

    public ICommand ConnectionEditCommand { get; }

    public ICommand ReconnectCommand { get; }

    public ICommand PowerSwitchCommand { get; }

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

    public void Initialize()
    {
        ExecuteReconnect();
    }

    public void CancelEditMode()
    {
        if (IsEditingConnection)
        {
            Ip = _settings.Ip;
            Port = _settings.Port.ToString();
            IsEditingConnection = false;
        }
    }

    public void OnLanguageChanged()
    {
        UpdateConnectionInfo();
        UpdatePowerInfo();
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
                Ip = _settings.Ip;
                Port = _settings.Port.ToString();
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
}
