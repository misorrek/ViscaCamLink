namespace ViscaCamLink.Tests.Services;

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;

using Moq;

using Shouldly;

using ViscaCamLink.Services;

using Xunit;

public sealed class WindowModeCoordinatorTests
{
    [Theory]
    [InlineData(0, 0, 1920, 1080, 300, 200, 24, 1596, 856)]
    [InlineData(100, 50, 800, 600, 300, 200, 24, 576, 426)]
    [InlineData(0, 0, 1000, 500, 100, 100, 0, 900, 400)]
    public void CalculateBottomRightPosition_Success(
        double workAreaX,
        double workAreaY,
        double workAreaWidth,
        double workAreaHeight,
        double windowWidth,
        double windowHeight,
        double margin,
        double expectedLeft,
        double expectedTop)
    {
        var workArea = new Rect(workAreaX, workAreaY, workAreaWidth, workAreaHeight);

        var (left, top) = WindowModeCoordinator.CalculateBottomRightPosition(workArea, windowWidth, windowHeight, margin);

        left.ShouldBe(expectedLeft);
        top.ShouldBe(expectedTop);
    }

    [Fact]
    public void MinimizeCompact_AfterInitialize_MinimizesCompactWindow()
    {
        RunOnStaThread(() =>
        {
            var coordinator = new WindowModeCoordinator(Mock.Of<ISettingsService>());
            var mainWindow = new Window();
            var compactWindow = new Window();
            coordinator.Initialize(mainWindow, compactWindow);

            coordinator.MinimizeCompact();

            compactWindow.WindowState.ShouldBe(WindowState.Minimized);
        });
    }

    [Fact]
    public void WindowCommands_BeforeInitialize_AreSafeNoOps()
    {
        var coordinator = new WindowModeCoordinator(Mock.Of<ISettingsService>());

        coordinator.EnterCompactMode();
        coordinator.ExitCompactMode();
        coordinator.MinimizeCompact();
        coordinator.CloseApplication();
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception caughtException)
            {
                exception = caughtException;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
