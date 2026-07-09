namespace ViscaCamLink.Tests.ViewModels;

using Moq;
using Shouldly;
using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;
using ViscaCamLink.Visca.Types;

public sealed class ConnectionViewModelTests
{
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<ICameraConnectionService> _connectionService = new();
    private readonly Mock<IPowerService> _powerService = new();
    private readonly Mock<IUiDispatcher> _uiDispatcher = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IHotKeyService> _hotKeyService = new();
    private readonly CameraProfile _activeCameraProfile = new() { Name = "Cam", Ip = "192.168.0.1", Port = 5678 };

    public ConnectionViewModelTests()
    {
        _settings.SetupGet(s => s.ActiveCameraProfile).Returns(_activeCameraProfile);
        _settings.SetupGet(s => s.ActiveCameraProfileId).Returns(_activeCameraProfile.Id);
        _settings.SetupGet(s => s.CameraProfiles).Returns(new List<CameraProfile> { _activeCameraProfile });
        _settings.SetupGet(s => s.UseMultipleCameraProfiles).Returns(false);

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

        viewModel.ConnectionStatus.ShouldBe(ConnectionStatus.Failed);
        viewModel.PowerStatus.ShouldBe(PowerStatus.Unknown);
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

        viewModel.PowerStatus.ShouldBe(PowerStatus.On);
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

        viewModel.ChangingPowerStatus.ShouldBeTrue();
        _uiDispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
    }

    private ConnectionViewModel CreateViewModel() =>
        new(_settings.Object, _connectionService.Object, _powerService.Object, _uiDispatcher.Object, _dialogService.Object, _hotKeyService.Object);
}