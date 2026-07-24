namespace ViscaCamLink.Tests.Services;

using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Services;
using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class PowerServiceTests
{
    private readonly Mock<IViscaController> _viscaController = new();
    private readonly PowerService _powerService;

    public PowerServiceTests()
    {
        _powerService = new PowerService(_viscaController.Object);
    }

    [Fact]
    public async Task RefreshPowerStatusAsync_Success()
    {
        _viscaController
            .Setup(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);

        PowerStatus? received = null;

        _powerService.PowerStatusChanged += (_, status) => received = status;

        await _powerService.RefreshPowerStatusAsync();

        received.ShouldBe(PowerStatus.On);
    }

    [Fact]
    public async Task SwitchPowerAsync_Success()
    {
        _viscaController
            .Setup(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);
        _viscaController
            .Setup(v => v.GetUpdatedPowerStatusAsync(PowerStatus.On, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.Standby);

        var switchingRaised = false;
        PowerStatus? received = null;

        _powerService.SwitchingPower += (_, _) => switchingRaised = true;
        _powerService.PowerStatusChanged += (_, status) => received = status;

        await _powerService.SwitchPowerAsync();

        switchingRaised.ShouldBeTrue();
        received.ShouldBe(PowerStatus.Standby);
        _viscaController.Verify(v => v.PowerOffAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SwitchPowerAsync_WhenPowerIsStandby_CallsPowerOnAndRaisesNewStatus()
    {
        _viscaController
            .Setup(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.Standby);
        _viscaController
            .Setup(v => v.GetUpdatedPowerStatusAsync(PowerStatus.Standby, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PowerStatus.On);

        PowerStatus? received = null;

        _powerService.PowerStatusChanged += (_, status) => received = status;

        await _powerService.SwitchPowerAsync();

        received.ShouldBe(PowerStatus.On);
        _viscaController.Verify(v => v.PowerOnAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PowerStatus.Unknown)]
    [InlineData(PowerStatus.InternalPowerCircuitError)]
    public async Task SwitchPowerAsync_WhenPowerStatusIsNotSwitchable_DoesNothing(PowerStatus powerStatus)
    {
        _viscaController
            .Setup(v => v.GetPowerStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(powerStatus);

        await _powerService.SwitchPowerAsync();

        _viscaController.Verify(v => v.PowerOnAsync(It.IsAny<CancellationToken>()), Times.Never);
        _viscaController.Verify(v => v.PowerOffAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
