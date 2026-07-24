namespace ViscaCamLink.Services;

using System;
using System.Windows;

public class WindowModeCoordinator(ISettingsService settingsService) : IWindowModeCoordinator
{
    private const double ScreenEdgeMargin = 24.0;

    private Window? _mainWindow;
    private Window? _compactWindow;
    private bool _isTransitioning;

    public void Initialize(Window mainWindow, Window compactWindow)
    {
        _mainWindow = mainWindow;
        _compactWindow = compactWindow;
        _mainWindow.StateChanged += OnMainWindowStateChanged;
    }

    public void EnterCompactMode()
    {
        if (_compactWindow is null || _mainWindow is null)
        {
            return;
        }

        var workArea = SystemParameters.WorkArea;

        (_compactWindow.Left, _compactWindow.Top) = CalculateBottomRightPosition(
            workArea, _compactWindow.Width, _compactWindow.Height, ScreenEdgeMargin);

        _compactWindow.Topmost = true;
        _compactWindow.Show();
        _compactWindow.Activate();
    }

    public void ExitCompactMode()
    {
        if (_mainWindow is null || _compactWindow is null)
        {
            return;
        }

        _isTransitioning = true;

        try
        {
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _compactWindow.Hide();
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public void MinimizeCompact()
    {
        if (_compactWindow is null)
        {
            return;
        }

        _compactWindow.WindowState = WindowState.Minimized;
    }

    public void CloseApplication()
    {
        _compactWindow?.Hide();
        _mainWindow?.Close();
    }

    /// <summary>
    /// Calculates the Left and Top position to place a window in the bottom-right of the
    /// work area (excludes taskbar) with the given margin from both edges.
    /// </summary>
    public static (double Left, double Top) CalculateBottomRightPosition(
        Rect workArea,
        double windowWidth,
        double windowHeight,
        double margin)
    {
        return (workArea.Right - windowWidth - margin, workArea.Bottom - windowHeight - margin);
    }

    private void OnMainWindowStateChanged(object? sender, EventArgs eventArgs)
    {
        if (_isTransitioning || _mainWindow is null || _compactWindow is null)
        {
            return;
        }

        if (_mainWindow.WindowState == WindowState.Minimized && settingsService.UseCompactView)
        {
            EnterCompactMode();
        }
        else if (_mainWindow.WindowState == WindowState.Normal && _compactWindow.IsVisible)
        {
            ExitCompactMode();
        }
    }
}
