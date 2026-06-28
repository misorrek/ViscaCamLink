namespace ViscaCamLink.Simulator;

using System.Collections.ObjectModel;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class SimulatorViewModel : ObservableObject
{
    private readonly CameraState _state = new();
    private readonly DispatcherTimer _simulationTimer;
    private ViscaTcpServer? _server;
    private ViscaCommandHandler? _handler;

    private const int ZoomSpeedMultiplier = 10;
    private static readonly TimeSpan SimulationStep = TimeSpan.FromMilliseconds(20);

    public SimulatorViewModel()
    {
        _simulationTimer = new DispatcherTimer
        {
            Interval = SimulationStep
        };
        _simulationTimer.Tick += SimulationTick;

        Port = 5678;
    }

    // --- Observable Properties ---

    [ObservableProperty] private int _port;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isClientConnected;
    [ObservableProperty] private bool _isPoweredOn = true;
    [ObservableProperty] private short _pan;
    [ObservableProperty] private short _tilt;
    [ObservableProperty] private short _zoom;
    [ObservableProperty] private int _panVelocity;
    [ObservableProperty] private int _tiltVelocity;
    [ObservableProperty] private int _zoomVelocity;
    [ObservableProperty] private int _lastPreset = -1;
    [ObservableProperty] private string _lastCommand = "";

    // Normalized values for UI (0.0 to 1.0 or -1.0 to 1.0)
    [ObservableProperty] private double _panNormalized;
    [ObservableProperty] private double _tiltNormalized;
    [ObservableProperty] private double _zoomNormalized;

    // Direction indicator (angle in degrees, 0=right, 90=up)
    [ObservableProperty] private double _directionAngle;
    [ObservableProperty] private double _directionMagnitude;
    [ObservableProperty] private bool _isMoving;

    public ObservableCollection<string> LogEntries { get; } = new();

    // --- Commands ---

    [RelayCommand]
    private void StartServer()
    {
        if (IsRunning) return;

        _handler = new ViscaCommandHandler(_state, OnStateChanged, Log);
        _server = new ViscaTcpServer(_handler, Log, OnClientConnectionChanged);
        _server.Start(Port);
        IsRunning = true;
        _simulationTimer.Start();
    }

    [RelayCommand]
    private void StopServer()
    {
        if (!IsRunning) return;

        _simulationTimer.Stop();
        _server?.Stop();
        _server = null;
        IsRunning = false;
        IsClientConnected = false;
    }

    [RelayCommand]
    private void ResetPosition()
    {
        _state.Pan = 0;
        _state.Tilt = 0;
        _state.Zoom = 0;
        _state.PanVelocity = 0;
        _state.TiltVelocity = 0;
        _state.ZoomVelocity = 0;
        UpdateDisplayFromState();
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogEntries.Clear();
    }

    // --- Simulation ---

    private void SimulationTick(object? sender, EventArgs e)
    {
        bool changed = false;

        if (_state.PanVelocity != 0)
        {
            short newPan = (short)(_state.Pan + _state.PanVelocity);
            _state.Pan = Math.Clamp(newPan, ViscaConstants.MinPan, ViscaConstants.MaxPan);
            if (_state.Pan == ViscaConstants.MinPan || _state.Pan == ViscaConstants.MaxPan)
                _state.PanVelocity = 0;
            changed = true;
        }

        if (_state.TiltVelocity != 0)
        {
            short newTilt = (short)(_state.Tilt + _state.TiltVelocity);
            _state.Tilt = Math.Clamp(newTilt, ViscaConstants.MinTilt, ViscaConstants.MaxTilt);
            if (_state.Tilt == ViscaConstants.MinTilt || _state.Tilt == ViscaConstants.MaxTilt)
                _state.TiltVelocity = 0;
            changed = true;
        }

        if (_state.ZoomVelocity != 0)
        {
            short newZoom = (short)(_state.Zoom + _state.ZoomVelocity * ZoomSpeedMultiplier);
            _state.Zoom = Math.Clamp(newZoom, ViscaConstants.MinZoom, ViscaConstants.MaxZoom);
            if (_state.Zoom == ViscaConstants.MinZoom || _state.Zoom == ViscaConstants.MaxZoom)
                _state.ZoomVelocity = 0;
            changed = true;
        }

        if (changed)
        {
            UpdateDisplayFromState();
        }
    }

    // --- State sync ---

    private void OnStateChanged()
    {
        App.Current?.Dispatcher.BeginInvoke(UpdateDisplayFromState);
    }

    private void OnClientConnectionChanged(bool connected)
    {
        App.Current?.Dispatcher.BeginInvoke(() => IsClientConnected = connected);
    }

    private void UpdateDisplayFromState()
    {
        IsPoweredOn = _state.IsPoweredOn;
        Pan = _state.Pan;
        Tilt = _state.Tilt;
        Zoom = _state.Zoom;
        PanVelocity = _state.PanVelocity;
        TiltVelocity = _state.TiltVelocity;
        ZoomVelocity = _state.ZoomVelocity;
        LastPreset = _state.LastRecalledPreset >= 0 ? _state.LastRecalledPreset : _state.LastSavedPreset;

        // Normalized values for visual indicators
        // Use uniform scale (MaxPan) for both axes so equal speeds produce equal visual movement
        // This means diagonals at equal speed appear as true 45° angles
        double uniformScale = ViscaConstants.MaxPan; // largest range = 2448
        PanNormalized = (double)Pan / uniformScale;
        TiltNormalized = (double)Tilt / uniformScale;
        ZoomNormalized = (double)Zoom / ViscaConstants.MaxZoom;

        // Direction angle and magnitude for the compass
        double dx = _state.PanVelocity;
        double dy = -_state.TiltVelocity; // invert for screen coordinates
        DirectionMagnitude = Math.Sqrt(dx * dx + dy * dy) / ViscaConstants.MaxPanSpeed;
        DirectionAngle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        IsMoving = _state.PanVelocity != 0 || _state.TiltVelocity != 0;
    }

    private void Log(string message)
    {
        App.Current?.Dispatcher.BeginInvoke(() =>
        {
            var timestamped = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            LogEntries.Insert(0, timestamped);
            LastCommand = message;

            // Keep log manageable
            while (LogEntries.Count > 200)
                LogEntries.RemoveAt(LogEntries.Count - 1);
        });
    }
}
