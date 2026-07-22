namespace ViscaCamLink.Tests.Services;

using System.Windows;

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
}
