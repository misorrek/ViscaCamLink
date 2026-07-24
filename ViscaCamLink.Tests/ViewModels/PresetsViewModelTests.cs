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
using ViscaCamLink.Resources;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;
using ViscaCamLink.Visca.Types;

using Xunit;

public sealed class PresetsViewModelTests
{
    private static readonly Guid DefaultGroupId = Guid.NewGuid();
    private static readonly Guid SecondGroupId = Guid.NewGuid();

    private readonly Mock<IPresetService> _presetService = new();
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<IHotKeyService> _hotKeyService = new();
    private readonly Mock<ICameraConnectionService> _connectionService = new();
    private readonly Mock<IPowerService> _powerService = new();
    private readonly Mock<IUiDispatcher> _uiDispatcher = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly PresetsViewModel _viewModel;

    private List<PresetMetadata> _presets;
    private List<PresetGroup> _groups;

    public PresetsViewModelTests()
    {
        _presets = CreatePresets(0, 10);
        _groups =
        [
            new PresetGroup { Id = DefaultGroupId, Name = "Default", Presets = _presets },
            new PresetGroup { Id = SecondGroupId, Name = "Second", Presets = CreatePresets(10, 10) },
        ];

        _presetService.Setup(p => p.Presets).Returns(() => _presets);
        _presetService.Setup(p => p.Groups).Returns(() => _groups);
        _presetService.Setup(p => p.ActiveGroupId).Returns(DefaultGroupId);
        _settings.SetupProperty(s => s.UseNumpadLayout, true);
        _settings.SetupProperty(s => s.UsePresetGroups, true);
        _settings.Setup(s => s.ActiveCameraProfile).Returns(new CameraProfile());
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

        _viewModel = CreateSut();
    }

    [Fact]
    public void GridPresets_Success()
    {
        _viewModel.GridPresets.Select(p => p.SlotIndex).ShouldBe([7, 8, 9, 4, 5, 6, 1, 2, 3]);
    }

    [Fact]
    public void GridPresets_WhenFewerThanTenPresets_SkipsFirstPreset()
    {
        _presets = CreatePresets(0, 5);

        var viewModel = CreateSut();

        viewModel.GridPresets.Select(p => p.SlotIndex).ShouldBe([1, 2, 3, 4]);
    }

    [Fact]
    public void UseNumpadLayout_Success()
    {
        _viewModel.UseNumpadLayout = false;

        _viewModel.GridPresets.Select(p => p.SlotIndex).ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9]);
    }

    [Fact]
    public void UseNumpadLayout_WhenSet_DoesNotTouchSettings()
    {
        _viewModel.UseNumpadLayout = false;

        _settings.Object.UseNumpadLayout.ShouldBeTrue();
        _settings.Verify(s => s.Save(), Times.Never);
    }

    [Fact]
    public void RefreshLayout_Success()
    {
        _settings.Object.UseNumpadLayout = false;
        _settings.Object.UsePresetGroups = false;

        _viewModel.RefreshLayout();

        _viewModel.UseNumpadLayout.ShouldBeFalse();
        _viewModel.UsePresetGroups.ShouldBeFalse();
        _viewModel.GridPresets.Select(p => p.SlotIndex).ShouldBe([1, 2, 3, 4, 5, 6, 7, 8, 9]);
    }

    [Fact]
    public void MemoryCommand_Success()
    {
        _viewModel.MemoryCommand.Execute(3);

        _viewModel.LastRecalledPresetSlot.ShouldBe(3);
        _viewModel.PresetIndicatorState.ShouldBe(PresetIndicatorState.Active);
        _presetService.Verify(p => p.RecallMemoryAsync(3), Times.Once);
    }

    [Fact]
    public void MemoryCommand_WhenSettingMemory_SetsMemorySlot()
    {
        _viewModel.MemorySetCommand.Execute(null);

        _viewModel.IsSettingMemory.ShouldBeTrue();
        _viewModel.MemoryInfo.ShouldBe(Strings.Presets_ChooseSlot);

        _viewModel.MemoryCommand.Execute(5);

        _presetService.Verify(p => p.SetMemoryAsync(5), Times.Once);
        _viewModel.IsSettingMemory.ShouldBeFalse();
        _viewModel.MemoryInfo.ShouldBe(Strings.Common_Saved);
    }

    [Fact]
    public void NotifyMovementStarted_Success()
    {
        _viewModel.MemoryCommand.Execute(3);

        _viewModel.NotifyMovementStarted();

        _viewModel.PresetIndicatorState.ShouldBe(PresetIndicatorState.Moved);
    }

    [Fact]
    public void NotifyMovementStarted_WhenIndicatorIsNotActive_DoesNothing()
    {
        _viewModel.NotifyMovementStarted();

        _viewModel.PresetIndicatorState.ShouldBe(PresetIndicatorState.None);
    }

    [Fact]
    public void ClearPresetIndicator_Success()
    {
        _viewModel.MemoryCommand.Execute(3);

        _viewModel.ClearPresetIndicator();

        _viewModel.LastRecalledPresetSlot.ShouldBe(-1);
        _viewModel.PresetIndicatorState.ShouldBe(PresetIndicatorState.None);
    }

    [Fact]
    public void ConnectionStatusChanged_WhenConnectionIsLost_ClearsPresetIndicator()
    {
        _viewModel.MemoryCommand.Execute(3);

        _connectionService.Raise(
            service => service.ConnectionStatusChanged += null,
            _connectionService.Object,
            ConnectionStatus.Failed);

        _viewModel.LastRecalledPresetSlot.ShouldBe(-1);
        _viewModel.PresetIndicatorState.ShouldBe(PresetIndicatorState.None);
    }

    [Fact]
    public void HasMultipleGroups_Success()
    {
        _viewModel.HasMultipleGroups.ShouldBeTrue();
    }

    [Fact]
    public void GroupSwitchCommand_Success()
    {
        _viewModel.GroupSwitchCommand.Execute(SecondGroupId);

        _presetService.Verify(p => p.SwitchGroup(SecondGroupId), Times.Once);
        _viewModel.PresetGroups.First(g => g.Id == SecondGroupId).IsActive.ShouldBeTrue();
        _viewModel.PresetGroups.First(g => g.Id == DefaultGroupId).IsActive.ShouldBeFalse();
    }

    [Fact]
    public void GroupAddCommand_Success()
    {
        _viewModel.GroupAddCommand.Execute(null);

        _presetService.Verify(p => p.AddGroup(string.Format(Strings.PresetGroup_NewName, 3)), Times.Once);
        _presetService.Verify(p => p.SwitchGroup(SecondGroupId), Times.Once);
    }

    [Fact]
    public void GroupRenameConfirmCommand_Success()
    {
        _viewModel.GroupRenameCommand.Execute(SecondGroupId);

        var group = _viewModel.PresetGroups.First(g => g.Id == SecondGroupId);

        _viewModel.RenamingGroupText.ShouldBe("Second");
        group.IsRenaming.ShouldBeTrue();

        _viewModel.RenamingGroupText = "  Stage ";

        _viewModel.GroupRenameConfirmCommand.Execute(null);

        _presetService.Verify(p => p.RenameGroup(SecondGroupId, "Stage"), Times.Once);
        group.Name.ShouldBe("Stage");
        group.IsRenaming.ShouldBeFalse();
        _viewModel.RenamingGroupText.ShouldBe(string.Empty);
    }

    [Fact]
    public void GroupRenameConfirmCommand_WhenTextIsWhitespace_KeepsOldName()
    {
        _viewModel.GroupRenameCommand.Execute(SecondGroupId);

        _viewModel.RenamingGroupText = "   ";

        _viewModel.GroupRenameConfirmCommand.Execute(null);

        _presetService.Verify(p => p.RenameGroup(SecondGroupId, "Second"), Times.Once);
    }

    [Fact]
    public void MemoryRenameConfirmCommand_Success()
    {
        _presetService.Setup(p => p.GetPresetName(0)).Returns("0");

        _viewModel.MemoryRenameCommand.Execute(0);

        _viewModel.RenamingSlotIndex.ShouldBe(0);
        _viewModel.RenamingText.ShouldBe("0");

        _viewModel.RenamingText = "  Home ";

        _viewModel.MemoryRenameConfirmCommand.Execute(null);

        _presetService.Verify(p => p.RenamePreset(0, "Home"), Times.Once);
        _viewModel.RenamingSlotIndex.ShouldBe(-1);
        _viewModel.RenamingText.ShouldBe(string.Empty);
    }

    [Fact]
    public void MemoryRenameConfirmCommand_WhenTextIsWhitespace_UsesSlotIndexAsName()
    {
        _presetService.Setup(p => p.GetPresetName(0)).Returns("0");

        _viewModel.MemoryRenameCommand.Execute(0);

        _viewModel.RenamingText = "   ";

        _viewModel.MemoryRenameConfirmCommand.Execute(null);

        _presetService.Verify(p => p.RenamePreset(0, "0"), Times.Once);
    }

    [Fact]
    public void PresetsChanged_RebuildsPresetCollections()
    {
        _presets = CreatePresets(20, 10);

        _presetService.Raise(p => p.PresetsChanged += null);

        _viewModel.Presets.Select(p => p.SlotIndex).ShouldBe(Enumerable.Range(20, 10));
        _viewModel.GridPresets.Select(p => p.SlotIndex).ShouldBe([27, 28, 29, 24, 25, 26, 21, 22, 23]);
    }

    [Fact]
    public void GroupsChanged_RebuildsGroupCollection()
    {
        _groups = [_groups[0]];

        _presetService.Raise(p => p.GroupsChanged += null);

        _viewModel.PresetGroups.ShouldHaveSingleItem().Id.ShouldBe(DefaultGroupId);
        _viewModel.HasMultipleGroups.ShouldBeFalse();
    }

    private PresetsViewModel CreateSut()
    {
        var connection = new ConnectionViewModel(
            _settings.Object,
            _connectionService.Object,
            _powerService.Object,
            _uiDispatcher.Object,
            _dialogService.Object,
            _hotKeyService.Object);

        return new PresetsViewModel(_presetService.Object, _settings.Object, _hotKeyService.Object, connection);
    }

    private static List<PresetMetadata> CreatePresets(int startSlot, int count)
    {
        return [.. Enumerable.Range(startSlot, count).Select(i => new PresetMetadata { GroupId = DefaultGroupId, SlotIndex = i, Name = i.ToString() })];
    }
}
