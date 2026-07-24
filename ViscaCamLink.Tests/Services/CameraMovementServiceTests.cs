namespace ViscaCamLink.Tests.Services;

using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Services;
using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class CameraMovementServiceTests
{
    private readonly Mock<IViscaController> _viscaController = new();
    private readonly CameraMovementService _movementService;

    public CameraMovementServiceTests()
    {
        _viscaController.Setup(v => v.MaxPanSpeed).Returns(ViscaProtocol.MaxPanSpeed);
        _viscaController.Setup(v => v.MaxTiltSpeed).Returns(ViscaProtocol.MaxTiltSpeed);
        _viscaController.Setup(v => v.MaxZoomSpeed).Returns(ViscaProtocol.MaxZoomSpeed);

        _movementService = new CameraMovementService(_viscaController.Object);
    }

    [Fact]
    public void MaxPanTiltSpeed_Success()
    {
        _movementService.MaxPanTiltSpeed.ShouldBe(ViscaProtocol.MaxPanSpeed);
    }

    [Fact]
    public void MaxZoomSpeed_Success()
    {
        _movementService.MaxZoomSpeed.ShouldBe(ViscaProtocol.MaxZoomSpeed);
    }

    [Theory]
    [InlineData(ViscaProtocol.MaxPanSpeed, ViscaProtocol.MaxTiltSpeed)]
    [InlineData(ViscaProtocol.MaxPanSpeed / 2, 10)]
    [InlineData(0, 0)]
    public void GetProportionalTiltSpeed_Success(byte panSpeed, byte expectedTiltSpeed)
    {
        var result = _movementService.GetProportionalTiltSpeed(panSpeed);

        result.ShouldBe(expectedTiltSpeed);
    }

    [Fact]
    public async Task PanTiltAsync_Success()
    {
        await _movementService.PanTiltAsync(PanTiltDirection.PanRightTiltDown, 10, 5);

        _viscaController.Verify(
            v => v.ContinuousPanTiltAsync(PanTiltDirection.PanRightTiltDown, 10, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StopPanTiltAsync_Success()
    {
        await _movementService.StopPanTiltAsync();

        _viscaController.Verify(
            v => v.ContinuousPanTiltAsync(PanTiltDirection.None, 0, 0, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(ZoomDirection.None, 0)]
    [InlineData(ZoomDirection.In, 5)]
    [InlineData(ZoomDirection.Out, 3)]
    public async Task ZoomAsync_Success(ZoomDirection zoomDirection, byte speed)
    {
        await _movementService.ZoomAsync(zoomDirection, speed);

        _viscaController.Verify(
            v => v.ContinuousZoomAsync(zoomDirection, speed, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GoHomeAsync_Success()
    {
        await _movementService.GoHomeAsync();

        _viscaController.Verify(v => v.GoHomeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
