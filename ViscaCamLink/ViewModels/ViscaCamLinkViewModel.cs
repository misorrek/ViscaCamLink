namespace ViscaCamLink.ViewModels;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using AutoUpdaterDotNET;

using CameraControl.Visca;
using ViscaCamLink.Factories;
using ViscaCamLink.Properties;
using ViscaCamLink.Resources;
using ViscaCamLink.Util;
using ViscaCamLink.Views;

public class ViscaCamLinkViewModel : INotifyPropertyChanged
{
    public ViscaCamLinkViewModel()
    {
        ViscaController = ViscaController.ForTcp(Settings.Default.Ip, Settings.Default.Port);

        SidebarCommand = new Command(ExecuteSidebar);
        UpdateCommand = new Command(OpenUpdateDialog);
        OptionsCommand = new Command(OpenOptions);
        ConnectionEditCommand = new Command(ExecuteConnectionEdit);
        ReconnectCommand = new Command(ExecuteReconnect);
        PowerSwitchCommand = new Command(ExecutePowerSwitch);
        MemoryRenameCommand = new Command(ExecuteMemoryRename);
        MemorySetCommand = new Command(ExecuteMemorySet);
        MemoryCommand = new Command(ExecuteMemorySetOrRecall);            
        HomeCommand = new Command(ExecuteHome);
        MoveBeginCommand = new Command(ExecuteMoveBegin);
        MoveEndCommand = new Command(ExecuteMoveEnd);
        MoveSpeedDecreaseCommand = new Command(ExecuteMoveSpeedDecreaseCommand);
        MoveSpeedIncreaseCommand = new Command(ExecuteMoveSpeedIncreaseCommand);
        ZoomCommand = new Command(ExecuteZoom);
        ZoomSpeedDecreaseCommand = new Command(ExecuteZoomSpeedDecrease);
        ZoomSpeedIncreaseCommand = new Command(ExecuteZoomSpeedIncrease);

        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad0, () => ExecuteMemorySetOrRecall("0"));
        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad1, () => ExecuteMemorySetOrRecall("1"));
        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad2, () => ExecuteMemorySetOrRecall("2"));
        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad3, () => ExecuteMemorySetOrRecall("3"));
        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad4, () => ExecuteMemorySetOrRecall("4"));
        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad5, () => ExecuteMemorySetOrRecall("5"));
        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad6, () => ExecuteMemorySetOrRecall("6"));
        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad7, () => ExecuteMemorySetOrRecall("7"));
        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad8, () => ExecuteMemorySetOrRecall("8"));
        GlobalHotKey.RegisterHotKey(ModifierKeys.None, Key.NumPad9, () => ExecuteMemorySetOrRecall("9"));

        ExecuteReconnect(); //Inital connection attempt 
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? UpdateAvailable;

    public ViscaController ViscaController { get; }

    public ICommand SidebarCommand { get; }

    public ICommand UpdateCommand { get; }

    public ICommand OptionsCommand { get; }

    public ICommand ConnectionEditCommand { get; }

    public ICommand ReconnectCommand { get; }

    public ICommand PowerSwitchCommand { get; }

    public ICommand MemoryRenameCommand { get; }

    public ICommand MemorySetCommand { get; }

    public ICommand MemoryCommand { get; }        

    public ICommand HomeCommand { get; }

    public ICommand MoveBeginCommand { get; }

    public ICommand MoveEndCommand { get; }

    public ICommand MoveSpeedDecreaseCommand { get; }

    public ICommand MoveSpeedIncreaseCommand { get; }

    public ICommand ZoomCommand { get; }

    public ICommand ZoomSpeedIncreaseCommand { get; }

    public ICommand ZoomSpeedDecreaseCommand { get; }

    public static Int32 MaximalPanTiltSpeed
    {
        get => ViscaController.MaxPanSpeed;
    }

    public static Int32 MaximalZoomSpeed
    {
        get => ViscaController.MaxZoomSpeed;
    }  

    public Boolean MemoryContainerVisible
    {
        get => Settings.Default.MemoryContainerVisible;

        set
        {
            Settings.Default.MemoryContainerVisible = value;
            NotifyPropertyChanged();
        }
    }

    public Boolean MoveContainerVisible
    {
        get => Settings.Default.MoveContainerVisible;

        set
        {
            Settings.Default.MoveContainerVisible = value;
            NotifyPropertyChanged();
        }
    }

    public Boolean ZoomContainerVisible
    {
        get => Settings.Default.ZoomContainerVisible;

        set
        {
            Settings.Default.ZoomContainerVisible = value;
            NotifyPropertyChanged();
        }
    }

    public String Ip
    {
        get => _ip;

        set
        {
            _ip = value;
            NotifyPropertyChanged();
        }
    }

    public String Port
    {
        get => _port;

        set
        {       
            _port = value;
            NotifyPropertyChanged();
        }
    }               

    public Boolean IsEditingConnection
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
            UpdatePowerStatus();
            NotifyPropertyChanged();
        }
    }

    public String ConnectionInfo
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

    public Boolean ChangingPowerStatus
    {
        get => _changingPowerStatus;

        set
        {
            _changingPowerStatus = value;
            UpdatePowerInfo();
            NotifyPropertyChanged();
        }
    }

    public String PowerInfo
    {
        get => _powerInfo;

        set
        {
            _powerInfo = value;
            NotifyPropertyChanged();
        }
    }

    public Boolean IsSettingMemory
    {
        get => _isSettingMemory;

        set
        {
            _isSettingMemory = value;
            NotifyPropertyChanged();
        }
    }

    public String MemoryInfo
    {
        get => _memoryInfo;

        set
        {
            _memoryInfo = value;
            NotifyPropertyChanged();
        }
    }

    private CancellationTokenSource? MemoryInfoCancellationTokenSource { get; set; }

    private String _ip = Settings.Default.Ip;

    private String _port = Settings.Default.Port.ToString();

    private Boolean _isEditingConnection = false;

    private ConnectionStatus _connectionStatus = ConnectionStatus.Failed;

    private String _connectionInfo = String.Empty;

    private PowerStatus _powerStatus = PowerStatus.Unknown;

    private Boolean _changingPowerStatus = false;

    private String _powerInfo = String.Empty;

    private Boolean _isSettingMemory = false;

    private String _memoryInfo = String.Empty;

    private void ExecuteSidebar(Object? parameter)
    {
        if (parameter != null && parameter is String container)
        {
            switch (container)
            {
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

    public void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
    {
        if (args.Error != null)
        {
            return;
        }

        if (args.IsUpdateAvailable)
        {
            lastArgs = args;

            UpdateAvailable?.Invoke();
        }
    }

    private UpdateInfoEventArgs? lastArgs;

    public void UpdateAvailableMethod()
    {
        UpdateAvailable?.Invoke();
    }

    private void OpenUpdateDialog()
    {
        if (lastArgs == null)
        {
            return;
        }

        var updateView = UpdateViewFactory.CreateUpdateView(lastArgs);
        var dialogResult = updateView.ShowDialog();
    }

    private void OpenOptions()
    {
        OptionsViewFactory.CreateOptionsView().ShowDialog();
    }

    private void UpdateConnectionInfo()
    {
        switch(ConnectionStatus)
        {
            case ConnectionStatus.Failed:
                ConnectionInfo = Strings.ConnectionStatus_Failed;
                break;
            case ConnectionStatus.Working:
                ConnectionInfo = Strings.ConnectionStatus_Working;
                break;
            case ConnectionStatus.Ok:
                ConnectionInfo = Strings.ConnectionStatus_Ok;
                break;
        }
    }

    private void UpdatePowerStatus()
    {
        if (ConnectionStatus == ConnectionStatus.Ok)
        {
            PowerStatus = ViscaController.GetPowerStatus().Result;
        }
        else
        {
            PowerStatus = PowerStatus.Unknown;
        }
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
            switch (PowerStatus)
            {
                case PowerStatus.Unknown:
                    PowerInfo = Strings.PowerStatus_Unknown;
                    break;
                case PowerStatus.On:
                    PowerInfo = Strings.PowerStatus_On;
                    break;
                case PowerStatus.Standby:
                    PowerInfo = Strings.PowerStatus_Standby;
                    break;
                case PowerStatus.InternalPowerCircuitError:
                    PowerInfo = Strings.PowerStatus_Error;
                    break;
            }
        }
    }

    private void ExecuteConnectionEdit(Object? parameter)
    {
        if (IsEditingConnection)
        {
            if (parameter is Boolean editingCancled && editingCancled)
            {
                Ip = Settings.Default.Ip;
                Port = Settings.Default.Port.ToString();
            }
            else
            {
                Settings.Default.Ip = Ip;

                Int32 port;

                if (!String.IsNullOrWhiteSpace(Port) && Int32.TryParse(Port, out port))
                {
                    Settings.Default.Port = port;
                }
            }
            IsEditingConnection = false;
        }
        else
        {
            IsEditingConnection = true;
        }
    }

    private void ExecuteReconnect()
    {
        var source = new CancellationTokenSource();

        ConnectionStatus = ConnectionStatus.Working;

        ViscaController
            .Reconnect(source.Token, Settings.Default.Ip, Settings.Default.Port)
            .ContinueWith(task =>
            {
                var connected = ViscaController.Connected.GetValueOrDefault();

                ConnectionStatus = connected ? ConnectionStatus.Ok : ConnectionStatus.Failed;
            });
    }

    private void ExecutePowerSwitch()
    {
        PowerStatus = ViscaController.GetPowerStatus().Result;

        switch(PowerStatus)
        {
            case PowerStatus.On:
                ViscaController.PowerOff().ContinueWith(task =>
                {
                    ChangingPowerStatus = true;
                    PowerStatus = ViscaController.GetUpdatedPowerStatus(PowerStatus).Result;
                });
                break;
            case PowerStatus.Standby:
                ViscaController.PowerOn().ContinueWith(task =>
                {
                    ChangingPowerStatus = true;
                    PowerStatus = ViscaController.GetUpdatedPowerStatus(PowerStatus).Result;
                });
                break;
        }
    }

    private void ExecuteMemoryRename()
    {
        throw new NotImplementedException();
    }

    private void ExecuteMemorySet()
    {
        IsSettingMemory = !IsSettingMemory;

        if (IsSettingMemory)
        {
            MemoryInfo = Strings.Presets_ChooseSlot;
            MemoryInfoCancellationTokenSource?.Cancel();
        }
        else
        {
            MemoryInfo = Strings.Common_Cancel;
            ResetMemorySetInfo();
        }
    }

    private void ExecuteMemorySetOrRecall(Object? parameter)
    {
        if (parameter != null && parameter is String stringedParameter)
        {
            var slot = Byte.Parse(stringedParameter);

            if (IsSettingMemory)
            {
                ViscaController.MemorySet(slot);
                IsSettingMemory = false;
                MemoryInfo = Strings.Common_Saved;
                ResetMemorySetInfo();
            }
            else
            {
                ViscaController.MemoryRecall(slot);
            }                
        }
    }        

    private void ExecuteHome()
    {
        ViscaController.GoHome();
    }

    private void ExecuteMoveBegin(Object? parameter)
    {
        if (parameter is MouseButtonEventArgs eventArgs &&
            eventArgs.LeftButton == MouseButtonState.Pressed &&
            eventArgs.Source is Button button &&
            button.CommandParameter is MoveDirection direction)
        {         
             ViscaController.ContinuousPanTilt(
                GetPan(direction), 
                GetTilt(direction), 
                (Byte)Settings.Default.PanTiltSpeed, 
                GetTiltSpeed());
        }
    }

    private void ExeuteMoveMouse(Object? parameter)
    {
        if (parameter != null && parameter is MouseEventArgs eventArgs &&
            eventArgs.Source != null && eventArgs.Source is FrameworkElement element &&
            eventArgs.LeftButton == MouseButtonState.Pressed)
        {
            var position = eventArgs.GetPosition(element); //TODO
            var panSpeed = NormalizeSpeed(position.X, element.ActualWidth, ViscaController.MaxPanSpeed);
            // Tilt has "up is positive" in camera coordinates, but "up is negative" in screen coordinates
            var tiltSpeed = -NormalizeSpeed(position.Y, element.ActualHeight, ViscaController.MaxTiltSpeed);
            var pan = panSpeed == 0 ? default(bool?) : panSpeed > 0;
            var tilt = tiltSpeed == 0 ? default(bool?) : tiltSpeed > 0;

            ViscaController.ContinuousPanTilt(pan, tilt, (Byte)Settings.Default.PanTiltSpeed, GetTiltSpeed());
        }
    }

    private void ExecuteMoveEnd(Object? parameter) //TODO
    {
        ViscaController.ContinuousPanTilt(default(bool?), default(bool?), 0, 0);
    }

    private static byte AbsSpeed(int speed) => (byte)Math.Max(Math.Abs(speed), 1); //TODO

    private static int NormalizeSpeed(double position, double visualScale, int maxValue) //TODO
    {
        // Scale to -1 / +1
        double scaledPosition = (position / visualScale) * 2 - 1;
        int speed = (int)(scaledPosition * (maxValue + 1));
        speed = Math.Min(speed, maxValue);
        speed = Math.Max(speed, -maxValue);
        return speed;
    }

    private static Boolean? GetPan(MoveDirection direction)
    {
        if (direction.HasFlag(MoveDirection.Right))
        {
            return true;
        }
        if (direction.HasFlag(MoveDirection.Left))
        {
            return false;
        }
        return null;
    }

    private static Boolean? GetTilt(MoveDirection direction)
    {
        if (direction.HasFlag(MoveDirection.Up))
        {
            return true;
        }
        if (direction.HasFlag(MoveDirection.Down))
        {
            return false;
        }
        return null;
    }

    private Byte GetTiltSpeed() //TODO
    {
        var speedInPercent = (Double)Settings.Default.PanTiltSpeed / ViscaController.MaxPanSpeed;

        return (Byte)Math.Ceiling(ViscaController.MaxTiltSpeed * speedInPercent);
    }

    private void ExecuteMoveSpeedDecreaseCommand()
    {
        if (Settings.Default.PanTiltSpeed > 1)
        {
            Settings.Default.PanTiltSpeed--;
            NotifyPropertyChanged();
        }
    }

    private void ExecuteMoveSpeedIncreaseCommand()
    {
        if (Settings.Default.PanTiltSpeed < MaximalPanTiltSpeed)
        {
            Settings.Default.PanTiltSpeed++;
            NotifyPropertyChanged();
        }
    }

    private void ExecuteZoom(Object? parameter)
    {
        if (parameter is MouseButtonEventArgs eventArgs && 
            eventArgs.Source is Button button &&
            button.CommandParameter is ZoomDirection direction)
        {
            switch(eventArgs.LeftButton)
            {
                case MouseButtonState.Pressed:
                    ViscaController.ContinuousZoom(direction == ZoomDirection.In, (Byte)Settings.Default.ZoomSpeed);
                    break;
                case MouseButtonState.Released:
                    ViscaController.ContinuousZoom(default(bool?), 0);
                    break;
            }
        }
    }

    private void ExecuteZoomSpeedDecrease()
    {
        if (Settings.Default.ZoomSpeed > 1)
        {
            Settings.Default.ZoomSpeed--;
            NotifyPropertyChanged();
        }
    }

    private void ExecuteZoomSpeedIncrease()
    {
        if (Settings.Default.ZoomSpeed < MaximalZoomSpeed)
        {
            Settings.Default.ZoomSpeed++;
            NotifyPropertyChanged();
        }
    }

    private async Task ResetMemorySetInfo()
    {
        MemoryInfoCancellationTokenSource = new CancellationTokenSource();

        await Task.Delay(5000, MemoryInfoCancellationTokenSource.Token);

        MemoryInfo = String.Empty;
    }

    protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
