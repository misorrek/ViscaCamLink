namespace ViscaCamLink.Services;

using System.Windows;

public interface IWindowModeCoordinator
{
    void Initialize(Window mainWindow, Window compactWindow);

    void EnterCompactMode();

    void ExitCompactMode();

    void MinimizeCompact();

    void CloseApplication();
}
