namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Repositories.Presets;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

using Xunit;

public sealed class ViscaCamLinkViewModelTests
{
    private static readonly Guid DefaultGroupId = Guid.NewGuid();

    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<ICameraConnectionService> _connectionService = new();
    private readonly Mock<IPowerService> _powerService = new();
    private readonly Mock<IUiDispatcher> _uiDispatcher = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IHotKeyService> _hotKeyService = new();
    private readonly Mock<IPresetService> _presetService = new();
    private readonly Mock<ICameraMovementService> _movementService = new();
    private readonly Mock<IUpdateService> _updateService = new();

    private readonly ConnectionViewModel _connection;
    private readonly PresetsViewModel _presets;
    private readonly MovementViewModel _movement;
    private readonly ZoomViewModel _zoom;
    private readonly ViscaCamLinkViewModel _viewModel;

    public ViscaCamLinkViewModelTests()
    {
        _settings.SetupProperty(s => s.ConnectionContainerVisible, true);
        _settings.SetupProperty(s => s.MemoryContainerVisible, true);
        _settings.SetupProperty(s => s.MoveContainerVisible, true);
        _settings.SetupProperty(s => s.ZoomContainerVisible, true);
        _settings.SetupProperty(s => s.UseNumpadLayout, true);
        _settings.SetupProperty(s => s.UsePresetGroups, true);
        _settings.SetupProperty(s => s.PanTiltSpeed, 3);
        _settings.SetupProperty(s => s.ZoomSpeed, 3);
        _settings.Setup(s => s.ActiveCameraProfile).Returns(new CameraProfile());
        _movementService.Setup(m => m.MaxPanTiltSpeed).Returns(24);
        _movementService.Setup(m => m.MaxZoomSpeed).Returns(7);
        _presetService.Setup(p => p.Presets).Returns(CreatePresets());
        _presetService.Setup(p => p.Groups).Returns([new PresetGroup { Id = DefaultGroupId, Name = "Default", Presets = CreatePresets() }]);
        _presetService.Setup(p => p.ActiveGroupId).Returns(DefaultGroupId);
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

        _connection = new ConnectionViewModel(
            _settings.Object,
            _connectionService.Object,
            _powerService.Object,
            _uiDispatcher.Object,
            _dialogService.Object,
            _hotKeyService.Object);
        _presets = new PresetsViewModel(_presetService.Object, _settings.Object, _hotKeyService.Object, _connection);
        _movement = new MovementViewModel(_movementService.Object, _settings.Object, _hotKeyService.Object);
        _zoom = new ZoomViewModel(_movementService.Object, _settings.Object, _hotKeyService.Object);

        _viewModel = new ViscaCamLinkViewModel(
            _settings.Object,
            _connection,
            _presets,
            _movement,
            _zoom,
            _updateService.Object,
            _dialogService.Object,
            _uiDispatcher.Object);
    }

    [Fact]
    public void Constructor_Success()
    {
        _viewModel.Connection.ShouldBeSameAs(_connection);
        _viewModel.Presets.ShouldBeSameAs(_presets);
        _viewModel.Movement.ShouldBeSameAs(_movement);
        _viewModel.Zoom.ShouldBeSameAs(_zoom);
    }

    [Fact]
    public void SidebarCommand_Success()
    {
        _viewModel.SidebarCommand.Execute(SidebarContainer.Memory);

        _viewModel.MemoryContainerVisible.ShouldBeFalse();

        _viewModel.SidebarCommand.Execute(SidebarContainer.Memory);

        _viewModel.MemoryContainerVisible.ShouldBeTrue();
    }

    [Fact]
    public void SidebarCommand_WhenOnlyOneContainerIsVisible_DoesNotHideLastContainer()
    {
        _settings.Object.MemoryContainerVisible = false;
        _settings.Object.MoveContainerVisible = false;
        _settings.Object.ZoomContainerVisible = false;

        _viewModel.SidebarCommand.Execute(SidebarContainer.Connection);

        _viewModel.ConnectionContainerVisible.ShouldBeTrue();
    }

    [Fact]
    public void OptionsCommand_Success()
    {
        _viewModel.OptionsCommand.Execute(null);

        _dialogService.Verify(d => d.ShowOptionsDialog(), Times.Once);
    }

    [Fact]
    public void PresetIndicator_WhenHomeIsExecuted_IsCleared()
    {
        _presets.MemoryCommand.Execute(3);

        _presets.LastRecalledPresetSlot.ShouldBe(3);

        _movement.HomeCommand.Execute(null);

        _presets.LastRecalledPresetSlot.ShouldBe(-1);
        _presets.PresetIndicatorState.ShouldBe(PresetIndicatorState.None);
    }

    private static List<PresetMetadata> CreatePresets()
    {
        return [.. Enumerable.Range(0, 10).Select(i => new PresetMetadata { GroupId = DefaultGroupId, SlotIndex = i, Name = i.ToString() })];
    }
}
