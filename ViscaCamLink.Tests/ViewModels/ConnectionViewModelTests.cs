namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shouldly;

using ViscaCamLink.Infrastructure.Abstractions;
using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class ConnectionViewModelTests
{
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<ICameraConnectionService> _connectionService = new();
    private readonly Mock<IPowerService> _powerService = new();
    private readonly Mock<IUiDispatcher> _uiDispatcher = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IHotKeyService> _hotKeyService = new();
    private readonly CameraProfile _activeCameraProfile = new() { Name = "Cam", Ip = "192.168.0.1", Port = 5678 };
    private readonly ConnectionViewModel _viewModel;

    public ConnectionViewModelTests()
    {
        _settings.SetupGet(s => s.ActiveCameraProfile).Returns(_activeCameraProfile);
        _settings.SetupGet(s => s.ActiveCameraProfileId).Returns(_activeCameraProfile.Id);
        _settings.SetupGet(s => s.CameraProfiles).Returns([_activeCameraProfile]);
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

        _viewModel = new ConnectionViewModel(
            _settings.Object,
            _connectionService.Object,
            _powerService.Object,
            _uiDispatcher.Object,
            _dialogService.Object,
            _hotKeyService.Object,
            NullLogger<ConnectionViewModel>.Instance);
    }

    [Fact]
    public void ConnectionStatusChanged_Success()
    {
        _connectionService.Raise(
            service => service.ConnectionStatusChanged += null,
            _connectionService.Object,
            ConnectionStatus.Failed);

        _viewModel.ConnectionStatus.ShouldBe(ConnectionStatus.Failed);
        _viewModel.PowerStatus.ShouldBe(PowerStatus.Unknown);
        _uiDispatcher.Verify(d => d.InvokeAsync(It.IsAny<Action>()), Times.Exactly(2));
    }

    [Fact]
    public void PowerStatusChanged_Success()
    {
        _powerService.Raise(
            service => service.PowerStatusChanged += null,
            _powerService.Object,
            PowerStatus.On);

        _viewModel.PowerStatus.ShouldBe(PowerStatus.On);
        _uiDispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
    }

    [Fact]
    public void SwitchingPower_Success()
    {
        _powerService.Raise(
            service => service.SwitchingPower += null,
            _powerService.Object,
            EventArgs.Empty);

        _viewModel.ChangingPowerStatus.ShouldBeTrue();
        _uiDispatcher.Verify(d => d.Post(It.IsAny<Action>()), Times.Once);
    }

    [Fact]
    public async Task ReconnectCommand_InvokesConnectionService()
    {
        var reconnectStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _connectionService
            .Setup(service => service.ReconnectAsync())
            .Returns(() =>
            {
                reconnectStarted.TrySetResult();
                return Task.CompletedTask;
            });

        _viewModel.ReconnectCommand.Execute(null);

        await reconnectStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        _connectionService.Verify(service => service.ReconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task PowerSwitchCommand_InvokesPowerService()
    {
        var powerSwitchStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _powerService
            .Setup(service => service.SwitchPowerAsync())
            .Returns(() =>
            {
                powerSwitchStarted.TrySetResult();
                return Task.CompletedTask;
            });

        _viewModel.PowerSwitchCommand.Execute(null);

        await powerSwitchStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        _powerService.Verify(service => service.SwitchPowerAsync(), Times.Once);
    }

    [Fact]
    public async Task PrevCameraCommand_SwitchesToPreviousCamera()
    {
        var (profiles, activeCameraId) = SetupCameraCycling();
        var switchedCamera = new TaskCompletionSource<CameraProfile>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connectionService
            .Setup(service => service.SwitchCameraProfileAsync(It.IsAny<CameraProfile>()))
            .Callback<CameraProfile>(profile =>
            {
                activeCameraId.Value = profile.Id;
                switchedCamera.TrySetResult(profile);
            })
            .Returns(Task.CompletedTask);

        _viewModel.PrevCameraCommand.Execute(null);

        (await switchedCamera.Task.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(profiles[1]);
        _connectionService.Verify(service => service.SwitchCameraProfileAsync(profiles[1]), Times.Once);
    }

    [Fact]
    public async Task NextCameraCommand_SwitchesToNextCamera()
    {
        var (profiles, activeCameraId) = SetupCameraCycling();
        activeCameraId.Value = profiles[1].Id;
        var switchedCamera = new TaskCompletionSource<CameraProfile>(TaskCreationOptions.RunContinuationsAsynchronously);
        _connectionService
            .Setup(service => service.SwitchCameraProfileAsync(It.IsAny<CameraProfile>()))
            .Callback<CameraProfile>(profile =>
            {
                activeCameraId.Value = profile.Id;
                switchedCamera.TrySetResult(profile);
            })
            .Returns(Task.CompletedTask);

        _viewModel.NextCameraCommand.Execute(null);

        (await switchedCamera.Task.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(profiles[0]);
        _connectionService.Verify(service => service.SwitchCameraProfileAsync(profiles[0]), Times.Once);
    }

    [Fact]
    public void CameraCommands_CanExecuteOnlyWhenMultipleProfilesExist()
    {
        var (profiles, _) = SetupCameraCycling();

        profiles.RemoveAt(1);
        _viewModel.PrevCameraCommand.CanExecute(null).ShouldBeFalse();
        _viewModel.NextCameraCommand.CanExecute(null).ShouldBeFalse();

        profiles.Add(new CameraProfile { Name = "Cam 2", Ip = "192.168.0.2", Port = 5678 });
        _viewModel.PrevCameraCommand.CanExecute(null).ShouldBeTrue();
        _viewModel.NextCameraCommand.CanExecute(null).ShouldBeTrue();
    }

    private (List<CameraProfile> Profiles, ActiveCameraId ActiveCameraId) SetupCameraCycling()
    {
        var profiles = new List<CameraProfile>
        {
            _activeCameraProfile,
            new() { Name = "Cam 2", Ip = "192.168.0.2", Port = 5678 }
        };
        var activeCameraId = new ActiveCameraId(_activeCameraProfile.Id);

        _settings.SetupGet(settings => settings.CameraProfiles).Returns(profiles);
        _settings.SetupGet(settings => settings.ActiveCameraProfileId).Returns(() => activeCameraId.Value);
        _settings
            .SetupGet(settings => settings.ActiveCameraProfile)
            .Returns(() => profiles.Find(profile => profile.Id == activeCameraId.Value));

        return (profiles, activeCameraId);
    }

    private sealed class ActiveCameraId(Guid value)
    {
        public Guid Value { get; set; } = value;
    }
}
