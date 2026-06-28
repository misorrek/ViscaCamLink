namespace ViscaCamLink.Tests.Services;

using FluentAssertions;

using Moq;

using ViscaCamLink.Services;
using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public sealed class PowerServiceTests
{
    private readonly Mock<IViscaController> _viscaController = new();
    private readonly PowerService _powerService;

    public PowerServiceTests()
    {
        _powerService = new PowerService(_viscaController.Object);
    }

    [Fact]
    public async Task RefreshPowerStatusAsync_RaisesPowerStatusChanged()
    {
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);

        PowerStatus? received = null;
        _powerService.PowerStatusChanged += (_, s) => received = s;

        await _powerService.RefreshPowerStatusAsync();

        received.Should().Be(PowerStatus.On);
    }

    [Fact]
    public async Task SwitchPowerAsync_WhenOn_CallsPowerOff()
    {
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);
        _viscaController
            .Setup(v => v.PowerOff(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        _viscaController
            .Setup(v => v.GetUpdatedPowerStatus(PowerStatus.On, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.Standby);

        await _powerService.SwitchPowerAsync();

        _viscaController.Verify(v => v.PowerOff(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SwitchPowerAsync_WhenStandby_CallsPowerOn()
    {
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.Standby);
        _viscaController
            .Setup(v => v.PowerOn(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        _viscaController
            .Setup(v => v.GetUpdatedPowerStatus(PowerStatus.Standby, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);

        await _powerService.SwitchPowerAsync();

        _viscaController.Verify(v => v.PowerOn(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SwitchPowerAsync_WhenUnknown_DoesNothing()
    {
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.Unknown);

        await _powerService.SwitchPowerAsync();

        _viscaController.Verify(v => v.PowerOn(It.IsAny<CancellationToken>()), Times.Never);
        _viscaController.Verify(v => v.PowerOff(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SwitchPowerAsync_WhenInternalError_DoesNothing()
    {
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.InternalPowerCircuitError);

        await _powerService.SwitchPowerAsync();

        _viscaController.Verify(v => v.PowerOn(It.IsAny<CancellationToken>()), Times.Never);
        _viscaController.Verify(v => v.PowerOff(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SwitchPowerAsync_RaisesSwitchingPowerBeforeCommand()
    {
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);
        _viscaController
            .Setup(v => v.GetUpdatedPowerStatus(PowerStatus.On, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.Standby);

        var switchingRaised = false;
        _powerService.SwitchingPower += (_, _) => switchingRaised = true;

        await _powerService.SwitchPowerAsync();

        switchingRaised.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchPowerAsync_RaisesPowerStatusChangedWithNewStatus()
    {
        _viscaController
            .Setup(v => v.GetPowerStatus(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.Standby);
        _viscaController
            .Setup(v => v.PowerOn(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _viscaController
            .Setup(v => v.GetUpdatedPowerStatus(PowerStatus.Standby, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);

        PowerStatus? received = null;
        _powerService.PowerStatusChanged += (_, s) => received = s;

        await _powerService.SwitchPowerAsync();

        received.Should().Be(PowerStatus.On);
    }
}
