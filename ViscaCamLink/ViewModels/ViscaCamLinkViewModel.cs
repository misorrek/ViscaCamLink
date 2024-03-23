namespace ViscaCamLink.ViewModels;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using AutoUpdaterDotNET;

using CameraControl.Visca;
using ViscaCamLink.Factories;
using ViscaCamLink.Properties;
using ViscaCamLink.Util;

public class ViscaCamLinkViewModel : INotifyPropertyChanged
{
    public ViscaCamLinkViewModel()
    {
        ViscaController = ViscaController.ForTcp(Settings.Default.Ip, Settings.Default.Port);

        var connected = ViscaController.Connected.GetValueOrDefault();

        ConnectionStatus = connected ? Status.Ok : Status.Failed;
        ConnectionInfo = connected ? "Verbunden" : "Keine Verbindung";

        SidebarCommand = new Command(ExecuteSidebar);
        UpdateCommand = new Command(OpenUpdateDialog);
        OptionsCommand = new Command(OpenOptions);
        ConnectionEditCommand = new Command(ExecuteConnectionEdit);
        ReconnectCommand = new Command(ExecuteReconnect);
        MemoryRenameCommand = new Command(ExecuteMemoryRename);
        MemorySetCommand = new Command(ExecuteMemorySet);
        MemoryCommand = new Command(ExecuteMemorySetOrRecall);            
        HomeCommand = new Command(ExecuteHome);
        MoveBeginCommand = new Command(ExecuteMoveBegin);
        MoveEndCommand = new Command(ExecuteMoveEnd);
        ZoomCommand = new Command(ExecuteZoom);                                   
        MoveSpeedDecreaseCommand = new Command(ExecuteMoveSpeedDecreaseCommand);
        MoveSpeedIncreaseCommand = new Command(ExecuteMoveSpeedIncreaseCommand);
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
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? UpdateAvailable;

    public ViscaController ViscaController { get; }

    public ICommand SidebarCommand { get; }

    public ICommand UpdateCommand { get; }

    public ICommand OptionsCommand { get; }

    public ICommand ConnectionEditCommand { get; }

    public ICommand ReconnectCommand { get; }

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
        get => Settings.Default.Ip;

        set
        {
            Settings.Default.Ip = value;
            NotifyPropertyChanged();
        }
    }

    public String Port
    {
        get => Settings.Default.Port.ToString();

        set
        {
            Int32 port;

            if (!String.IsNullOrWhiteSpace(value) && Int32.TryParse(value, out port))
            {
                Settings.Default.Port = port;
                NotifyPropertyChanged();
            }
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

    public Status ConnectionStatus
    {
        get => _connectionStatus;

        set
        {
            _connectionStatus = value;
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

    private Boolean _isEditingConnection = false;

    private Status _connectionStatus = Status.Failed;

    private String _connectionInfo = String.Empty;

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
        MessageBox.Show("Bald verfügbar!");
    }

    private void ExecuteConnectionEdit()
    {
        IsEditingConnection = !IsEditingConnection;
    }

    private void ExecuteReconnect()
    {
        var source = new CancellationTokenSource();

        ConnectionStatus = Status.Working;
        ConnectionInfo = "Verbindungsversuch";

        ViscaController.Reconnect(source.Token, Settings.Default.Ip, Settings.Default.Port)
            .ContinueWith(task =>
            {
                var connected = ViscaController.Connected.GetValueOrDefault();

                ConnectionStatus = connected ? Status.Ok : Status.Failed;
                ConnectionInfo = connected ? "Verbunden" : "Keine Verbindung";
            });
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
            MemoryInfo = "Wähle einen Slot";
            MemoryInfoCancellationTokenSource?.Cancel();
        }
        else
        {
            MemoryInfo = "Abgebrochen";
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
                MemoryInfo = "Gespeichert";
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

    private void ExecuteMoveBegin(Object? parameter) //TODO
    {
        if (parameter != null && parameter is MouseEventArgs eventArgs &&
            eventArgs.Source != null && eventArgs.Source is FrameworkElement element &&
            eventArgs.LeftButton == MouseButtonState.Pressed)
        {
            if (element.Tag == null)
            {
                return;
            }
            var tag = element.Tag.ToString();

            if (tag == null)
            {
                return;
            }

            bool? pan;
            bool? tilt;

            if (tag == "Mouse")
            {
                var position = eventArgs.GetPosition(element); //TODO

                var panSpeed = NormalizeSpeed(position.X, element.ActualWidth, ViscaController.MaxPanSpeed);
                // Tilt has "up is positive" in camera coordinates, but "up is negative" in screen coordinates
                var tiltSpeed = -NormalizeSpeed(position.Y, element.ActualHeight, ViscaController.MaxTiltSpeed);

                pan = panSpeed == 0 ? default(bool?) : panSpeed > 0;
                tilt = tiltSpeed == 0 ? default(bool?) : tiltSpeed > 0;
            }
            else
            {
                pan = GetPan(tag);
                tilt = GetTilt(tag);
            }
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

    private static Boolean? GetPan(String tag) //TODO
    {
        if (tag.ToLower().Contains("right"))
        {
            return true;
        }
        if (tag.ToLower().Contains("left"))
        {
            return false;
        }
        return null;
    }

    private static Boolean? GetTilt(String tag) //TODO
    {
        if (tag.ToLower().Contains("up"))
        {
            return true;
        }
        if (tag.ToLower().Contains("down"))
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

    private void ExecuteZoom(Object? parameter)
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
    {
        if (parameter != null && parameter is MouseButtonEventArgs eventArgs && 
            eventArgs.Source != null && eventArgs.Source is FrameworkElement element)
        {
            var zoomIn = element.Tag.ToString() == "In";
            
            switch(eventArgs.LeftButton)
            {
                case MouseButtonState.Pressed:
                    ViscaController.ContinuousZoom(zoomIn, (Byte)Settings.Default.ZoomSpeed);
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
