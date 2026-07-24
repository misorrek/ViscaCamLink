namespace ViscaCamLink.ViewModels;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Services;
using ViscaCamLink.Visca.Types;

public class MovementViewModel : ViewModelBase
{
    private const int MinimumPanTiltSpeed = 1;

    /// <summary>
    /// Horizontal drag distance (in device-independent pixels) required to reach maximum pan speed.
    /// Increase to make the mouse pad less sensitive; decrease to make it more responsive.
    /// </summary>
    private const double MousePanSensitivity = 200.0;

    /// <summary>
    /// Vertical drag distance (in device-independent pixels) required to reach maximum tilt speed.
    /// Increase to make the mouse pad less sensitive; decrease to make it more responsive.
    /// </summary>
    private const double MouseTiltSensitivity = 200.0;

    private static readonly TimeSpan MousePanTiltThrottle = TimeSpan.FromMilliseconds(100);

    private readonly ICameraMovementService _movementService;
    private readonly ISettingsService _settings;

    private DateTime _lastMousePanTiltTime = DateTime.MinValue;
    private bool _isMousePanning;
    private Point? _mousePanStartPosition;

    public MovementViewModel(
        ICameraMovementService movementService,
        ISettingsService settings,
        IHotKeyService hotKeyService)
    {
        _movementService = movementService;
        _settings = settings;

        HomeCommand = new Command(ExecuteHome);
        MoveBeginCommand = new Command(ExecuteMoveBegin);
        MoveEndCommand = new Command(ExecuteMoveEnd);
        MoveSpeedDecreaseCommand = new Command(ExecuteMoveSpeedDecrease);
        MoveSpeedIncreaseCommand = new Command(ExecuteMoveSpeedIncrease);
        MouseMovePanTiltCommand = new Command(ExecuteMouseMovePanTilt);
        MousePanEndCommand = new Command(ExecuteMousePanEnd);

        hotKeyService.RegisterActions(CreateHotKeyActions());
    }

    /// <summary>Raised when any pan/tilt movement begins (button press, mouse drag, or hotkey).</summary>
    public event Action? MovementStarted;

    /// <summary>Raised when the home button is pressed.</summary>
    public event Action? HomeExecuted;

    public ICommand HomeCommand { get; }

    public ICommand MoveBeginCommand { get; }

    public ICommand MoveEndCommand { get; }

    public ICommand MoveSpeedDecreaseCommand { get; }

    public ICommand MoveSpeedIncreaseCommand { get; }

    public ICommand MouseMovePanTiltCommand { get; }

    public ICommand MousePanEndCommand { get; }

    public int MaximalPanTiltSpeed => _movementService.MaxPanTiltSpeed;

    public int PanTiltSpeed
    {
        get => _settings.PanTiltSpeed;
        set
        {
            if (_settings.PanTiltSpeed == value)
            {
                return;
            }

            _settings.PanTiltSpeed = value;

            NotifyPropertyChanged();
        }
    }

    public bool IsMousePanning
    {
        get => _isMousePanning;
        private set
        {
            if (_isMousePanning == value)
            {
                return;
            }

            _isMousePanning = value;

            NotifyPropertyChanged();
        }
    }

    private async void ExecuteHome()
    {
        HomeExecuted?.Invoke();

        await TryCameraOperation(_movementService.GoHomeAsync());
    }

    private void ExecuteMoveBegin(object? parameter)
    {
        if (parameter is MouseButtonEventArgs eventArgs &&
            eventArgs.LeftButton == MouseButtonState.Pressed &&
            eventArgs.Source is Button button &&
            button.CommandParameter is PanTiltDirection direction)
        {
            BeginPanTilt(direction);
        }
    }

    private async void ExecuteMouseMovePanTilt(object? parameter)
    {
        if (parameter is not MouseEventArgs eventArgs ||
            eventArgs.Source is not FrameworkElement element ||
            eventArgs.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        IsMousePanning = true;

        var position = eventArgs.GetPosition(element);

        if (parameter is MouseButtonEventArgs)
        {
            _mousePanStartPosition = position;
            _lastMousePanTiltTime = DateTime.UtcNow - MousePanTiltThrottle;
            return;
        }

        if (_mousePanStartPosition is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        if (now - _lastMousePanTiltTime < MousePanTiltThrottle)
        {
            return;
        }

        _lastMousePanTiltTime = now;

        MovementStarted?.Invoke();

        var deltaX = position.X - _mousePanStartPosition.Value.X;
        var deltaY = position.Y - _mousePanStartPosition.Value.Y;
        var panSpeed = NormalizeSpeedFromDelta(deltaX, MousePanSensitivity, _movementService.MaxPanTiltSpeed);
        var tiltSpeed = -NormalizeSpeedFromDelta(deltaY, MouseTiltSensitivity, _movementService.MaxProportionalTiltSpeed);
        var panTiltDirection = GetPanTiltDirectionFromSpeed(panSpeed, tiltSpeed);

        await TryCameraOperation(_movementService.PanTiltAsync(panTiltDirection, AbsoluteSpeed(panSpeed), AbsoluteSpeed(tiltSpeed)));
    }

    private async void ExecuteMousePanEnd(object? parameter)
    {
        IsMousePanning = false;
        _lastMousePanTiltTime = DateTime.MinValue;
        _mousePanStartPosition = null;

        await TryCameraOperation(_movementService.StopPanTiltAsync());
    }

    private async void ExecuteMoveEnd(object? parameter)
    {
        await TryCameraOperation(_movementService.StopPanTiltAsync());
    }

    private void ExecuteMoveSpeedDecrease()
    {
        if (_settings.PanTiltSpeed > MinimumPanTiltSpeed)
        {
            _settings.PanTiltSpeed--;

            NotifyPropertyChanged(nameof(PanTiltSpeed));
        }
    }

    private void ExecuteMoveSpeedIncrease()
    {
        if (_settings.PanTiltSpeed < MaximalPanTiltSpeed)
        {
            _settings.PanTiltSpeed++;

            NotifyPropertyChanged(nameof(PanTiltSpeed));
        }
    }

    private IEnumerable<HotKeyActionRegistration> CreateHotKeyActions()
    {
        yield return new HotKeyActionRegistration(HotKeyAction.MoveUp, () => BeginPanTilt(PanTiltDirection.TiltUp), StopPanTilt);
        yield return new HotKeyActionRegistration(HotKeyAction.MoveDown, () => BeginPanTilt(PanTiltDirection.TiltDown), StopPanTilt);
        yield return new HotKeyActionRegistration(HotKeyAction.MoveLeft, () => BeginPanTilt(PanTiltDirection.PanLeft), StopPanTilt);
        yield return new HotKeyActionRegistration(HotKeyAction.MoveRight, () => BeginPanTilt(PanTiltDirection.PanRight), StopPanTilt);
    }

    private void BeginPanTilt(PanTiltDirection direction)
    {
        MovementStarted?.Invoke();

        _ = TryCameraOperation(_movementService.PanTiltAsync(
            direction,
            (byte)_settings.PanTiltSpeed,
            _movementService.GetProportionalTiltSpeed(_settings.PanTiltSpeed)));
    }

    private void StopPanTilt()
    {
        _ = TryCameraOperation(_movementService.StopPanTiltAsync());
    }

    private static byte AbsoluteSpeed(int speed)
    {
        return (byte)Math.Max(Math.Abs(speed), MinimumPanTiltSpeed);
    }

    private static int NormalizeSpeedFromDelta(double delta, double sensitivity, int maxValue)
    {
        var scaledDelta = delta / sensitivity;
        var speed = (int)(scaledDelta * (maxValue + 1));

        speed = Math.Min(speed, maxValue);
        speed = Math.Max(speed, -maxValue);

        return speed;
    }

    private static PanTiltDirection GetPanTiltDirectionFromSpeed(int panSpeed, int tiltSpeed)
    {
        var panTiltDirection = PanTiltDirection.None;

        if (panSpeed > 0)
        {
            panTiltDirection |= PanTiltDirection.PanRight;
        }
        else if (panSpeed < 0)
        {
            panTiltDirection |= PanTiltDirection.PanLeft;
        }

        if (tiltSpeed > 0)
        {
            panTiltDirection |= PanTiltDirection.TiltUp;
        }
        else if (tiltSpeed < 0)
        {
            panTiltDirection |= PanTiltDirection.TiltDown;
        }

        return panTiltDirection;
    }
}
