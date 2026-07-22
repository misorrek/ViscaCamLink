namespace ViscaCamLink.Tests.Services;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Moq;

using Shouldly;

using ViscaCamLink.Repositories.Presets;
using ViscaCamLink.Services;
using ViscaCamLink.Visca;

using Xunit;

public sealed class PresetServiceTests
{
    private readonly Mock<IViscaController> _viscaController = new();
    private readonly Mock<IPresetRepository> _repository = new();
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly Guid _cameraId = Guid.NewGuid();
    private readonly PresetService _presetService;

    public PresetServiceTests()
    {
        _settingsService.Setup(s => s.ActiveCameraProfileId).Returns(_cameraId);
        _repository.Setup(r => r.LoadForCameraProfile(_cameraId)).Returns(CreateDefaultPresetData());

        _presetService = new PresetService(_viscaController.Object, _repository.Object, _settingsService.Object);
    }

    [Fact]
    public void Presets_Success()
    {
        _presetService.Presets.Count.ShouldBe(10);
        _presetService.Presets[0].SlotIndex.ShouldBe(0);
        _presetService.Presets[9].SlotIndex.ShouldBe(9);
    }

    [Fact]
    public void Groups_Success()
    {
        _presetService.Groups.Count.ShouldBe(1);
        _presetService.Groups[0].Id.ShouldBe("default");
    }

    [Fact]
    public void ActiveGroupId_Success()
    {
        _presetService.ActiveGroupId.ShouldBe("default");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(255)]
    public async Task SetMemoryAsync_Success(byte slot)
    {
        await _presetService.SetMemoryAsync(slot);

        _viscaController.Verify(v => v.MemorySet(slot, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(255)]
    public async Task RecallMemoryAsync_Success(byte slot)
    {
        await _presetService.RecallMemoryAsync(slot);

        _viscaController.Verify(v => v.MemoryRecall(slot, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetPresetName_Success()
    {
        var name = _presetService.GetPresetName(0);

        name.ShouldBe("0");
    }

    [Fact]
    public void GetPresetName_WhenSlotIsUnknown_ReturnsSlotIndexAsString()
    {
        var name = _presetService.GetPresetName(99);

        name.ShouldBe("99");
    }

    [Fact]
    public void RenamePreset_Success()
    {
        var raised = false;

        _presetService.PresetsChanged += () => raised = true;

        _presetService.RenamePreset(0, "Home");

        _presetService.GetPresetName(0).ShouldBe("Home");
        raised.ShouldBeTrue();
        _repository.Verify(r => r.SaveForCameraProfile(_cameraId, It.IsAny<PresetData>()), Times.Once);
    }

    [Fact]
    public void RenamePreset_WhenSlotIsUnknown_DoesNotSave()
    {
        _presetService.RenamePreset(99, "Unknown");

        _repository.Verify(r => r.SaveForCameraProfile(_cameraId, It.IsAny<PresetData>()), Times.Never);
    }

    [Fact]
    public void SwitchGroup_Success()
    {
        _presetService.AddGroup("Second");

        var secondGroupId = _presetService.Groups[1].Id;
        var raised = false;

        _presetService.PresetsChanged += () => raised = true;

        _presetService.SwitchGroup(secondGroupId);

        _presetService.ActiveGroupId.ShouldBe(secondGroupId);
        _presetService.Presets.Count.ShouldBe(10);
        _presetService.Presets[0].SlotIndex.ShouldNotBe(0);
        raised.ShouldBeTrue();
    }

    [Fact]
    public void SwitchGroup_WhenGroupIdIsUnknown_DoesNothing()
    {
        var raised = false;

        _presetService.PresetsChanged += () => raised = true;

        _presetService.SwitchGroup("nonexistent");

        raised.ShouldBeFalse();
        _presetService.ActiveGroupId.ShouldBe("default");
    }

    [Fact]
    public void AddGroup_Success()
    {
        var raised = false;

        _presetService.GroupsChanged += () => raised = true;

        _presetService.AddGroup("Second");

        _presetService.Groups.Count.ShouldBe(2);
        _presetService.Groups[1].Name.ShouldBe("Second");
        _presetService.Groups[1].Presets.Count.ShouldBe(10);

        var firstSlots = _presetService.Groups[0].Presets.Select(p => p.SlotIndex).ToHashSet();
        var secondSlots = _presetService.Groups[1].Presets.Select(p => p.SlotIndex).ToHashSet();

        firstSlots.Overlaps(secondSlots).ShouldBeFalse();
        raised.ShouldBeTrue();
        _repository.Verify(r => r.SaveForCameraProfile(_cameraId, It.IsAny<PresetData>()), Times.Once);
    }

    [Fact]
    public void RemoveGroup_Success()
    {
        _presetService.AddGroup("Second");

        var secondGroupId = _presetService.Groups[1].Id;
        var raised = false;

        _presetService.GroupsChanged += () => raised = true;

        _presetService.RemoveGroup(secondGroupId);

        _presetService.Groups.Count.ShouldBe(1);
        _presetService.Groups[0].Id.ShouldBe("default");
        raised.ShouldBeTrue();
    }

    [Fact]
    public void RemoveGroup_WhenOnlyOneGroupExists_DoesNotRemove()
    {
        _presetService.RemoveGroup("default");

        _presetService.Groups.Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveGroup_WhenActiveGroupIsRemoved_SwitchesToFirstGroup()
    {
        _presetService.AddGroup("Second");

        var secondGroupId = _presetService.Groups[1].Id;

        _presetService.SwitchGroup(secondGroupId);
        _presetService.RemoveGroup(secondGroupId);

        _presetService.ActiveGroupId.ShouldBe("default");
    }

    [Fact]
    public void RenameGroup_Success()
    {
        var raised = false;

        _presetService.GroupsChanged += () => raised = true;

        _presetService.RenameGroup("default", "Main Camera");

        _presetService.Groups[0].Name.ShouldBe("Main Camera");
        raised.ShouldBeTrue();
        _repository.Verify(r => r.SaveForCameraProfile(_cameraId, It.IsAny<PresetData>()), Times.Once);
    }

    [Fact]
    public void RenameGroup_WhenGroupIdIsUnknown_DoesNotSave()
    {
        _presetService.RenameGroup("nonexistent", "Whatever");

        _repository.Verify(r => r.SaveForCameraProfile(_cameraId, It.IsAny<PresetData>()), Times.Never);
    }

    private static PresetData CreateDefaultPresetData()
    {
        var presets = Enumerable.Range(0, 10)
            .Select(i => new PresetMetadata
            {
                GroupId = "default",
                SlotIndex = i,
                Name = i.ToString(),
            })
            .ToList();

        return new PresetData
        {
            Groups =
            [
                new PresetGroup
                {
                    Id = "default",
                    Name = "Default",
                    Presets = presets,
                },
            ],
        };
    }
}
