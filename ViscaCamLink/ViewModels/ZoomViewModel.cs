namespace ViscaCamLink.ViewModels;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ViscaCamLink.Services;
using ViscaCamLink.Util;
using ViscaCamLink.Visca.Types;

public class ZoomViewModel : ViewModelBase
{
    private readonly ICameraMovementService _movementService;
    private readonly ISettingsService _settings;

    public ZoomViewModel(
        ICameraMovementService movementService,
        ISettingsService settings)
    {
        _movementService = movementService;
        _settings = settings;

        ZoomCommand = new Command(ExecuteZoom);
        ZoomSpeedDecreaseCommand = new Command(ExecuteZoomSpeedDecrease);
        ZoomSpeedIncreaseCommand = new Command(ExecuteZoomSpeedIncrease);
    }

    public ICommand ZoomCommand { get; }

    public ICommand ZoomSpeedIncreaseCommand { get; }

    public ICommand ZoomSpeedDecreaseCommand { get; }

    public int MaximalZoomSpeed => _movementService.MaxZoomSpeed;

    public int ZoomSpeed
    {
        get => _settings.ZoomSpeed;
        set
        {
            if (_settings.ZoomSpeed == value) return;
            _settings.ZoomSpeed = value;
            NotifyPropertyChanged();
        }
    }

    private async void ExecuteZoom(object? parameter)
    {
        if (parameter is MouseButtonEventArgs eventArgs &&
            eventArgs.Source is Button button &&
            button.CommandParameter is ZoomDirection direction)
        {
            switch (eventArgs.LeftButton)
            {
                case MouseButtonState.Pressed:
                    await TryCameraOperation(_movementService.ZoomAsync(direction, (byte)_settings.ZoomSpeed));
                    break;
                case MouseButtonState.Released:
                    await TryCameraOperation(_movementService.ZoomAsync(ZoomDirection.None, 0));
                    break;
            }
        }
    }

    private void ExecuteZoomSpeedDecrease()
    {
        if (_settings.ZoomSpeed > 1)
        {
            _settings.ZoomSpeed--;
            NotifyPropertyChanged(nameof(ZoomSpeed));
        }
    }

    private void ExecuteZoomSpeedIncrease()
    {
        if (_settings.ZoomSpeed < MaximalZoomSpeed)
        {
            _settings.ZoomSpeed++;
            NotifyPropertyChanged(nameof(ZoomSpeed));
        }
    }
}
