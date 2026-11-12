namespace ViscaCamLink.Simulator;

using System;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class SimulatorViewModel : ObservableObject, IDisposable
{
    private const int ZoomSpeedMultiplier = 10;
    private const int MaxLogEntries = 200;
    private static readonly TimeSpan SimulationStep = TimeSpan.FromMilliseconds(20);

    private readonly CameraState _state = new();
    private readonly DispatcherTimer _simulationTimer;
    private ViscaTcpServer? _server;
    private bool _isDisposed;

    [ObservableProperty]
    private int _port = ViscaConstants.DefaultPort;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isClientConnected;

    [ObservableProperty]
    private bool _isPoweredOn = true;

    [ObservableProperty]
    private short _pan;

    [ObservableProperty]
    private short _tilt;

    [ObservableProperty]
    private short _zoom;

    [ObservableProperty]
    private int _panVelocity;

    [ObservableProperty]
    private int _tiltVelocity;

    [ObservableProperty]
    private int _zoomVelocity;

    [ObservableProperty]
    private int _lastPreset = -1;

    [ObservableProperty]
    private string _lastCommand = string.Empty;

    // Normalized values for UI (0.0 to 1.0 or -1.0 to 1.0)
    [ObservableProperty]
    private double _panNormalized;

    [ObservableProperty]
    private double _tiltNormalized;

    [ObservableProperty]
    private double _zoomNormalized;

    // Direction indicator (angle in degrees, 0=right, 90=up)
    [ObservableProperty]
    private double _directionAngle;

    [ObservableProperty]
    private double _directionMagnitude;

    [ObservableProperty]
    private bool _isMoving;

    public SimulatorViewModel()
    {
        _simulationTimer = new DispatcherTimer
        {
            Interval = SimulationStep
        };
        _simulationTimer.Tick += SimulationTick;
    }

    public ObservableCollection<string> LogEntries { get; } = [];

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _simulationTimer.Stop();
        _simulationTimer.Tick -= SimulationTick;
        _server?.Dispose();
        _server = null;
        IsRunning = false;
        IsClientConnected = false;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void StartServer()
    {
        if (IsRunning)
        {
            return;
        }

        var handler = new ViscaCommandHandler(_state, OnStateChanged, Log);
        var server = new ViscaTcpServer(handler, Log, OnClientConnectionChanged);

        try
        {
            server.Start(Port);
        }
        catch (SocketException ex)
        {
            server.Dispose();
            Log($"Start failed: {ex.Message}");

            return;
        }

        _server = server;
        IsRunning = true;
        _simulationTimer.Start();
    }

    [RelayCommand]
    private void StopServer()
    {
        if (!IsRunning)
        {
            return;
        }

        _simulationTimer.Stop();
        _server?.Dispose();
        _server = null;
        IsRunning = false;
        IsClientConnected = false;
    }

    [RelayCommand]
    private void ResetPosition()
    {
        _state.ResetPosition();
        UpdateDisplayFromState();
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogEntries.Clear();
    }

    private void SimulationTick(object? sender, EventArgs e)
    {
        var changed = false;

        if (_state.PanVelocity != 0)
        {
            _state.Pan = Math.Clamp((short)(_state.Pan + _state.PanVelocity), ViscaConstants.MinPan, ViscaConstants.MaxPan);

            if (_state.Pan is ViscaConstants.MinPan or ViscaConstants.MaxPan)
            {
                _state.PanVelocity = 0;
            }

            changed = true;
        }

        if (_state.TiltVelocity != 0)
        {
            _state.Tilt = Math.Clamp((short)(_state.Tilt + _state.TiltVelocity), ViscaConstants.MinTilt, ViscaConstants.MaxTilt);

            if (_state.Tilt is ViscaConstants.MinTilt or ViscaConstants.MaxTilt)
            {
                _state.TiltVelocity = 0;
            }

            changed = true;
        }

        if (_state.ZoomVelocity != 0)
        {
            _state.Zoom = Math.Clamp((short)(_state.Zoom + _state.ZoomVelocity * ZoomSpeedMultiplier), ViscaConstants.MinZoom, ViscaConstants.MaxZoom);

            if (_state.Zoom is ViscaConstants.MinZoom or ViscaConstants.MaxZoom)
            {
                _state.ZoomVelocity = 0;
            }

            changed = true;
        }

        if (changed)
        {
            UpdateDisplayFromState();
        }
    }

    private void OnStateChanged()
    {
        Application.Current?.Dispatcher.BeginInvoke(UpdateDisplayFromState);
    }

    private void OnClientConnectionChanged(bool connected)
    {
        Application.Current?.Dispatcher.BeginInvoke(() => IsClientConnected = connected);
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

        // Uniform scale (largest range) for both axes so equal speeds give equal visual movement
        // and diagonals at equal speed appear as true 45° angles
        const double uniformScale = ViscaConstants.MaxPan;

        PanNormalized = (double)Pan / uniformScale;
        TiltNormalized = (double)Tilt / uniformScale;
        ZoomNormalized = (double)Zoom / ViscaConstants.MaxZoom;

        double directionX = _state.PanVelocity;
        var directionY = -(double)_state.TiltVelocity; // invert for screen coordinates

        DirectionMagnitude = Math.Sqrt(directionX * directionX + directionY * directionY) / ViscaConstants.MaxPanSpeed;
        DirectionAngle = Math.Atan2(directionY, directionX) * 180.0 / Math.PI;
        IsMoving = _state.PanVelocity != 0 || _state.TiltVelocity != 0;
    }

    private void Log(string message)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            LogEntries.Insert(0, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            LastCommand = message;

            while (LogEntries.Count > MaxLogEntries)
            {
                LogEntries.RemoveAt(LogEntries.Count - 1);
            }
        });
    }
}
