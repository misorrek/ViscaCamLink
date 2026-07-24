namespace ViscaCamLink.ViewModels;

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Services;
using ViscaCamLink.Visca.Types;

public class ZoomViewModel : ViewModelBase
{
    private const int MinimumZoomSpeed = 1;

    private readonly ICameraMovementService _movementService;
    private readonly ISettingsService _settings;

    public ZoomViewModel(
        ICameraMovementService movementService,
        ISettingsService settings,
        IHotKeyService hotKeyService)
    {
        _movementService = movementService;
        _settings = settings;

        ZoomCommand = new Command(ExecuteZoom);
        ZoomSpeedDecreaseCommand = new Command(ExecuteZoomSpeedDecrease);
        ZoomSpeedIncreaseCommand = new Command(ExecuteZoomSpeedIncrease);

        hotKeyService.RegisterActions(CreateHotKeyActions());
    }

    public event Action? ZoomStarted;

    public ICommand ZoomCommand { get; }

    public ICommand ZoomSpeedIncreaseCommand { get; }

    public ICommand ZoomSpeedDecreaseCommand { get; }

    public int MaximalZoomSpeed => _movementService.MaxZoomSpeed;

    public int ZoomSpeed
    {
        get => _settings.ZoomSpeed;
        set
        {
            if (_settings.ZoomSpeed == value)
            {
                return;
            }

            _settings.ZoomSpeed = value;

            NotifyPropertyChanged();
        }
    }

    private void ExecuteZoom(object? parameter)
    {
        if (parameter is MouseButtonEventArgs eventArgs &&
            eventArgs.Source is Button button &&
            button.CommandParameter is ZoomDirection direction)
        {
            switch (eventArgs.LeftButton)
            {
                case MouseButtonState.Pressed:
                    BeginZoom(direction);
                    break;
                case MouseButtonState.Released:
                    StopZoom();
                    break;
            }
        }
    }

    private void ExecuteZoomSpeedDecrease()
    {
        if (_settings.ZoomSpeed > MinimumZoomSpeed)
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

    private IEnumerable<HotKeyActionRegistration> CreateHotKeyActions()
    {
        yield return new HotKeyActionRegistration(HotKeyAction.ZoomIn, () => BeginZoom(ZoomDirection.In), StopZoom);
        yield return new HotKeyActionRegistration(HotKeyAction.ZoomOut, () => BeginZoom(ZoomDirection.Out), StopZoom);
    }

    private void BeginZoom(ZoomDirection direction)
    {
        ZoomStarted?.Invoke();

        _ = TryCameraOperation(_movementService.ZoomAsync(direction, (byte)_settings.ZoomSpeed));
    }

    private void StopZoom()
    {
        _ = TryCameraOperation(_movementService.ZoomAsync(ZoomDirection.None, 0));
    }
}
