namespace ViscaCamLink.Tests.Services;

using FluentAssertions;

using Moq;

using ViscaCamLink.Services;
using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

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
    public void MaxPanTiltSpeed_ReturnsControllerMaxPanSpeed()
    {
        _movementService.MaxPanTiltSpeed.Should().Be(ViscaProtocol.MaxPanSpeed);
    }

    [Fact]
    public void MaxZoomSpeed_ReturnsControllerMaxZoomSpeed()
    {
        _movementService.MaxZoomSpeed.Should().Be(ViscaProtocol.MaxZoomSpeed);
    }

    [Fact]
    public void GetProportionalTiltSpeed_AtMaxPanSpeed_ReturnsMaxTiltSpeed()
    {
        var result = _movementService.GetProportionalTiltSpeed(ViscaProtocol.MaxPanSpeed);

        result.Should().Be(ViscaProtocol.MaxTiltSpeed);
    }

    [Fact]
    public void GetProportionalTiltSpeed_AtHalfPanSpeed_ReturnsHalfTiltSpeed()
    {
        var halfPan = ViscaProtocol.MaxPanSpeed / 2;

        var result = _movementService.GetProportionalTiltSpeed(halfPan);

        // Ceiling of (MaxTiltSpeed * 0.5) = Ceiling(20 * 0.5) = 10
        var expected = (byte)Math.Ceiling(ViscaProtocol.MaxTiltSpeed * ((double)halfPan / ViscaProtocol.MaxPanSpeed));
        result.Should().Be(expected);
    }

    [Fact]
    public void GetProportionalTiltSpeed_AtZero_ReturnsZero()
    {
        var result = _movementService.GetProportionalTiltSpeed(0);

        result.Should().Be(0);
    }

    [Fact]
    public async Task PanTiltAsync_DelegatesToController()
    {
        _viscaController
            .Setup(v => v.ContinuousPanTilt(PanTiltDirection.PanRightTiltDown, 10, 5, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _movementService.PanTiltAsync(PanTiltDirection.PanRightTiltDown, 10, 5);

        _viscaController.Verify();
    }

    [Fact]
    public async Task StopPanTiltAsync_SendsStopCommand()
    {
        _viscaController
            .Setup(v => v.ContinuousPanTilt(PanTiltDirection.None, 0, 0, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _movementService.StopPanTiltAsync();

        _viscaController.Verify();
    }

    [Theory]
    [InlineData(ZoomDirection.None, 0)]
    [InlineData(ZoomDirection.In, 5)]
    [InlineData(ZoomDirection.Out, 3)]
    public async Task ZoomAsync_DelegatesToController(ZoomDirection zoomDirection, byte speed)
    {
        _viscaController
            .Setup(v => v.ContinuousZoom(zoomDirection, speed, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _movementService.ZoomAsync(zoomDirection, speed);

        _viscaController.Verify();
    }

    [Fact]
    public async Task GoHomeAsync_DelegatesToController()
    {
        _viscaController
            .Setup(v => v.GoHome(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _movementService.GoHomeAsync();

        _viscaController.Verify();
    }
}
