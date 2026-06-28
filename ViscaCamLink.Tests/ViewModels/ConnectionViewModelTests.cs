namespace ViscaCamLink.Tests.ViewModels;

using FluentAssertions;

using Moq;

using ViscaCamLink.Services;
using ViscaCamLink.Util;
using ViscaCamLink.ViewModels;
using ViscaCamLink.Visca.Types;

public sealed class ConnectionViewModelTests
{
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<ICameraConnectionService> _connectionService = new();
    private readonly Mock<IPowerService> _powerService = new();
    private readonly Mock<IUiDispatcher> _uiDispatcher = new();

    public ConnectionViewModelTests()
    {
        _settings.SetupGet(s => s.Ip).Returns("192.168.0.1");
        _settings.SetupGet(s => s.Port).Returns(5678);

        _uiDispatcher
            .Setup(d => d.InvokeAsync(It.IsAny<Action>()))
            .Returns<Action>(action =>
            {
                action();
                return Task.CompletedTask;
            });

        _uiDispatcher
            .Setup(d => d.Post(It.IsAny<Action>()))
            .Callback<Action>(action => action());
    }

    [Fact]
    public void ConnectionStatusChanged_MarshalsStatusAndUnknownPowerStatusThroughUiDispatcher()
    {
        var viewModel = CreateViewModel();

        _connectionService.Raise(
            service => service.ConnectionStatusChanged += null,
            _connectionService.Object,
            ConnectionStatus.Failed);

        viewModel.ConnectionStatus.Should().Be(ConnectionStatus.Failed);
        viewModel.PowerStatus.Should().Be(PowerStatus.Unknown);
        _uiDispatcher.Verify(d => d.InvokeAsync(It.IsAny<Action>()), Times.Exactly(2));
    }

    [Fact]
    public void PowerStatusChanged_MarshalsPowerStatusThroughUiDispatcher()
    {
        var viewModel = CreateViewModel();

        _powerService.Raise(
            service => service.PowerStatusChanged += null,
            _powerService.Object,
            PowerStatus.On);

        viewModel.PowerStatus.Should().Be(PowerStatus.On);
        _uiDispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
    }

    [Fact]
    public void SwitchingPower_MarshalsChangingPowerStatusThroughUiDispatcher()
    {
        var viewModel = CreateViewModel();

        _powerService.Raise(
            service => service.SwitchingPower += null,
            _powerService.Object,
            EventArgs.Empty);

        viewModel.ChangingPowerStatus.Should().BeTrue();
        _uiDispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
    }

    private ConnectionViewModel CreateViewModel() =>
        new(_settings.Object, _connectionService.Object, _powerService.Object, _uiDispatcher.Object);
}