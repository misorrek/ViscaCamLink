namespace ViscaCamLink.Views;

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using WpfAnimatedGif;

public partial class ViscaCamLinkView : Window
{
    private const int GWL_STYLE = -16;
    private const int WS_MAXIMIZEBOX = 0x10000;

    private readonly ISettingsService _settingsService;

    private WindowState _restoredWindowState = WindowState.Normal;

    public ViscaCamLinkView(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // Pre-set Width before InitializeComponent so the HWND is created at the correct
        // size. SetWindowPlacement in SourceInitialized then only moves the window (no
        // resize) which avoids the DWM black flash on startup.
        if (_settingsService.WindowPlacement is { } placement)
        {
            Width = placement.NormalRight - placement.NormalLeft;
        }

        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        ContentRendered += OnContentRendered;
        Closing += OnClosing;
    }

    public void ShowUpdateButton()
    {
        var updateButtonTemplate = UpdateButton.Template;
        var updateImageControl = (Image)updateButtonTemplate.FindName("UpdateImage", UpdateButton);
        var animationController = ImageBehavior.GetAnimationController(updateImageControl);

        UpdateButton.Visibility = Visibility.Visible;

        animationController.GotoFrame(0);
        animationController.Play();
    }

    private void OnSourceInitialized(object? sender, EventArgs eventArgs)
    {
        if (sender is Window window)
        {
            var windowHandle = new WindowInteropHelper(window).Handle;
            var windowStyle = GetWindowLong(windowHandle, GWL_STYLE);

            _ = SetWindowLong(windowHandle, GWL_STYLE, (int)(windowStyle & ~WS_MAXIMIZEBOX));
        }

        if (_settingsService.WindowPlacement is { } placement)
        {
            _restoredWindowState = WindowPlacementHelper.Restore(this, placement);
        }
    }

    private void OnContentRendered(object? sender, EventArgs eventArgs)
    {
        // Apply maximized state after first render to avoid the DWM black flash.
        if (_restoredWindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Maximized;
        }

        ContentRendered -= OnContentRendered;
    }

    private void OnClosing(object? sender, CancelEventArgs eventArgs)
    {
        _settingsService.WindowPlacement = WindowPlacementHelper.Save(this);
        _settingsService.Save();
    }

    private void Window_LayoutUpdated(object sender, EventArgs eventArgs)
    {
        SizeToContent = SizeToContent.Height;
    }

    private void Window_Deactivated(object sender, EventArgs eventArgs)
    {
        if (DataContext is ViscaCamLinkViewModel viewModel && viewModel.Movement.IsMousePanning)
        {
            viewModel.Movement.MousePanEndCommand.Execute(null);
        }
    }

    private void Window_Activated(object sender, EventArgs eventArgs)
    {
        if (DataContext is ViscaCamLinkViewModel viewModel &&
            viewModel.Movement.IsMousePanning &&
            Mouse.LeftButton == MouseButtonState.Released)
        {
            viewModel.Movement.MousePanEndCommand.Execute(null);
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongA")]
    private static partial int GetWindowLong(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongA")]
    private static partial int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
