namespace ViscaCamLink.ViewModels;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ViscaCamLink.Repositories;
using ViscaCamLink.Services;
using ViscaCamLink.Util;
using ViscaCamLink.Visca.Types;

public class MovementViewModel : ViewModelBase
{
    private readonly ICameraMovementService _movementService;
    private readonly ISettingsService _settings;

    private DateTime _lastMousePanTiltTime = DateTime.MinValue;
    private bool _isMousePanning;

    private static readonly TimeSpan MousePanTiltThrottle = TimeSpan.FromMilliseconds(100);

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

    public ICommand HomeCommand { get; }

    public ICommand MoveBeginCommand { get; }

    public ICommand MoveEndCommand { get; }

    public ICommand MoveSpeedDecreaseCommand { get; }

    public ICommand MoveSpeedIncreaseCommand { get; }

    public ICommand MouseMovePanTiltCommand { get; }

    public ICommand MousePanEndCommand { get; }

    /// <summary>Raised when any pan/tilt movement begins (button press, mouse drag, or hotkey).</summary>
    public event Action? MovementStarted;

    /// <summary>Raised when the home button is pressed.</summary>
    public event Action? HomeExecuted;

    public int MaximalPanTiltSpeed => _movementService.MaxPanTiltSpeed;

    public int PanTiltSpeed
    {
        get => _settings.PanTiltSpeed;
        set
        {
            if (_settings.PanTiltSpeed == value) return;
            _settings.PanTiltSpeed = value;
            NotifyPropertyChanged();
        }
    }

    public bool IsMousePanning
    {
        get => _isMousePanning;
        private set
        {
            if (_isMousePanning == value) return;
            _isMousePanning = value;
            NotifyPropertyChanged();
        }
    }

    private async void ExecuteHome()
    {
        HomeExecuted?.Invoke();
        await TryCameraOperation(_movementService.GoHomeAsync());
    }

    private async void ExecuteMoveBegin(object? parameter)
    {
        if (parameter is MouseButtonEventArgs eventArgs &&
            eventArgs.LeftButton == MouseButtonState.Pressed &&
            eventArgs.Source is Button button &&
            button.CommandParameter is PanTiltDirection direction)
        {
            MovementStarted?.Invoke();
            await TryCameraOperation(_movementService.PanTiltAsync(
                direction,
                (byte)_settings.PanTiltSpeed,
                _movementService.GetProportionalTiltSpeed(_settings.PanTiltSpeed)));
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

        var now = DateTime.UtcNow;
        if (now - _lastMousePanTiltTime < MousePanTiltThrottle)
            return;

        _lastMousePanTiltTime = now;
        MovementStarted?.Invoke();

        var position = eventArgs.GetPosition(element);
        var panSpeed = NormalizeSpeed(position.X, element.ActualWidth, _movementService.MaxPanTiltSpeed);
        var tiltSpeed = -NormalizeSpeed(position.Y, element.ActualHeight, _movementService.MaxPanTiltSpeed);
        var panTiltDirection = GetPanTiltDirectionFromSpeed(panSpeed, tiltSpeed);

        await TryCameraOperation(_movementService.PanTiltAsync(panTiltDirection, AbsSpeed(panSpeed), AbsSpeed(tiltSpeed)));
    }

    private async void ExecuteMousePanEnd(object? parameter)
    {
        IsMousePanning = false;
        _lastMousePanTiltTime = DateTime.MinValue;
        await TryCameraOperation(_movementService.StopPanTiltAsync());
    }

    private async void ExecuteMoveEnd(object? parameter)
    {
        await TryCameraOperation(_movementService.StopPanTiltAsync());
    }

    private void ExecuteMoveSpeedDecrease()
    {
        if (_settings.PanTiltSpeed > 1)
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

    private static byte AbsSpeed(int speed) => (byte)Math.Max(Math.Abs(speed), 1);

    private static int NormalizeSpeed(double position, double visualScale, int maxValue)
    {
        double scaledPosition = (position / visualScale) * 2 - 1;
        int speed = (int)(scaledPosition * (maxValue + 1));
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

    private static bool? GetPan(PanTiltDirection direction)
    {
        if (direction.HasFlag(PanTiltDirection.PanRight)) return true;
        if (direction.HasFlag(PanTiltDirection.PanLeft)) return false;
        return null;
    }

    private static bool? GetTilt(PanTiltDirection direction)
    {
        if (direction.HasFlag(PanTiltDirection.TiltUp)) return true;
        if (direction.HasFlag(PanTiltDirection.TiltDown)) return false;
        return null;
    }

    private IEnumerable<HotKeyActionRegistration> CreateHotKeyActions()
    {
        yield return new HotKeyActionRegistration(HotKeyAction.MoveUp,
            () => { MovementStarted?.Invoke(); _ = TryCameraOperation(_movementService.PanTiltAsync(PanTiltDirection.TiltUp, (byte)_settings.PanTiltSpeed, _movementService.GetProportionalTiltSpeed(_settings.PanTiltSpeed))); },
            () => _ = TryCameraOperation(_movementService.StopPanTiltAsync()));
        yield return new HotKeyActionRegistration(HotKeyAction.MoveDown,
            () => { MovementStarted?.Invoke(); _ = TryCameraOperation(_movementService.PanTiltAsync(PanTiltDirection.TiltDown, (byte)_settings.PanTiltSpeed, _movementService.GetProportionalTiltSpeed(_settings.PanTiltSpeed))); },
            () => _ = TryCameraOperation(_movementService.StopPanTiltAsync()));
        yield return new HotKeyActionRegistration(HotKeyAction.MoveLeft,
            () => { MovementStarted?.Invoke(); _ = TryCameraOperation(_movementService.PanTiltAsync(PanTiltDirection.PanLeft, (byte)_settings.PanTiltSpeed, _movementService.GetProportionalTiltSpeed(_settings.PanTiltSpeed))); },
            () => _ = TryCameraOperation(_movementService.StopPanTiltAsync()));
        yield return new HotKeyActionRegistration(HotKeyAction.MoveRight,
            () => { MovementStarted?.Invoke(); _ = TryCameraOperation(_movementService.PanTiltAsync(PanTiltDirection.PanRight, (byte)_settings.PanTiltSpeed, _movementService.GetProportionalTiltSpeed(_settings.PanTiltSpeed))); },
            () => _ = TryCameraOperation(_movementService.StopPanTiltAsync()));
    }
}
