namespace ViscaCamLink.Tests.Services;

using FluentAssertions;

using Moq;
using ViscaCamLink.Repositories;
using ViscaCamLink.Services;
using ViscaCamLink.Visca;

public sealed class PresetServiceTests
{
    private readonly Mock<IViscaController> _viscaController = new();
    private readonly Mock<IPresetRepository> _repository = new();
    private readonly PresetService _presetService;

    public PresetServiceTests()
    {
        _repository.Setup(r => r.Load()).Returns(CreateDefaultPresetData());
        _presetService = new PresetService(_viscaController.Object, _repository.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(255)]
    public async Task SetMemoryAsync_DelegatesToControllerWithCorrectSlot(byte slot)
    {
        _viscaController
            .Setup(v => v.MemorySet(slot, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _presetService.SetMemoryAsync(slot);

        _viscaController.Verify();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(255)]
    public async Task RecallMemoryAsync_DelegatesToControllerWithCorrectSlot(byte slot)
    {
        _viscaController
            .Setup(v => v.MemoryRecall(slot, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _presetService.RecallMemoryAsync(slot);

        _viscaController.Verify();
    }

    [Fact]
    public void GetPresetName_ReturnsNameFromRepository()
    {
        var name = _presetService.GetPresetName(0);

        name.Should().Be("0");
    }

    [Fact]
    public void GetPresetName_ForUnknownSlot_ReturnsSlotIndexAsString()
    {
        var name = _presetService.GetPresetName(99);

        name.Should().Be("99");
    }

    [Fact]
    public void RenamePreset_UpdatesNameAndSaves()
    {
        _presetService.RenamePreset(0, "Home");

        _presetService.GetPresetName(0).Should().Be("Home");
        _repository.Verify(r => r.Save(It.IsAny<PresetData>()), Times.Once);
    }

    [Fact]
    public void RenamePreset_RaisesPresetsChanged()
    {
        var raised = false;
        _presetService.PresetsChanged += () => raised = true;

        _presetService.RenamePreset(0, "Home");

        raised.Should().BeTrue();
    }

    [Fact]
    public void RenamePreset_ForUnknownSlot_DoesNothing()
    {
        _presetService.RenamePreset(99, "Unknown");

        _repository.Verify(r => r.Save(It.IsAny<PresetData>()), Times.Never);
    }

    [Fact]
    public void Presets_ReturnsPresetsFromActiveGroup()
    {
        _presetService.Presets.Should().HaveCount(10);
        _presetService.Presets[0].SlotIndex.Should().Be(0);
        _presetService.Presets[9].SlotIndex.Should().Be(9);
    }

    [Fact]
    public void Groups_ReturnsAllGroups()
    {
        _presetService.Groups.Should().HaveCount(1);
        _presetService.Groups[0].Id.Should().Be("default");
    }

    [Fact]
    public void ActiveGroupId_ReturnsFirstGroupByDefault()
    {
        _presetService.ActiveGroupId.Should().Be("default");
    }

    [Fact]
    public void SwitchGroup_ChangesActiveGroup()
    {
        _presetService.AddGroup("Second");
        var secondGroupId = _presetService.Groups[1].Id;

        _presetService.SwitchGroup(secondGroupId);

        _presetService.ActiveGroupId.Should().Be(secondGroupId);
    }

    [Fact]
    public void SwitchGroup_RaisesPresetsChanged()
    {
        _presetService.AddGroup("Second");
        var secondGroupId = _presetService.Groups[1].Id;
        var raised = false;
        _presetService.PresetsChanged += () => raised = true;

        _presetService.SwitchGroup(secondGroupId);

        raised.Should().BeTrue();
    }

    [Fact]
    public void SwitchGroup_WithInvalidId_DoesNothing()
    {
        var raised = false;
        _presetService.PresetsChanged += () => raised = true;

        _presetService.SwitchGroup("nonexistent");

        raised.Should().BeFalse();
        _presetService.ActiveGroupId.Should().Be("default");
    }

    [Fact]
    public void AddGroup_CreatesNewGroupWithPresets()
    {
        _presetService.AddGroup("Second");

        _presetService.Groups.Should().HaveCount(2);
        _presetService.Groups[1].Name.Should().Be("Second");
        _presetService.Groups[1].Presets.Should().HaveCount(10);
    }

    [Fact]
    public void AddGroup_AssignsNonOverlappingSlots()
    {
        _presetService.AddGroup("Second");

        var firstSlots = _presetService.Groups[0].Presets.Select(p => p.SlotIndex).ToHashSet();
        var secondSlots = _presetService.Groups[1].Presets.Select(p => p.SlotIndex).ToHashSet();
        firstSlots.Overlaps(secondSlots).Should().BeFalse();
    }

    [Fact]
    public void AddGroup_PersistsData()
    {
        _presetService.AddGroup("Second");

        _repository.Verify(r => r.Save(It.IsAny<PresetData>()), Times.Once);
    }

    [Fact]
    public void AddGroup_RaisesGroupsChanged()
    {
        var raised = false;
        _presetService.GroupsChanged += () => raised = true;

        _presetService.AddGroup("Second");

        raised.Should().BeTrue();
    }

    [Fact]
    public void RemoveGroup_RemovesTheGroup()
    {
        _presetService.AddGroup("Second");
        var secondGroupId = _presetService.Groups[1].Id;

        _presetService.RemoveGroup(secondGroupId);

        _presetService.Groups.Should().HaveCount(1);
        _presetService.Groups[0].Id.Should().Be("default");
    }

    [Fact]
    public void RemoveGroup_CannotRemoveLastGroup()
    {
        _presetService.RemoveGroup("default");

        _presetService.Groups.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveGroup_SwitchesToFirstGroupIfActiveRemoved()
    {
        _presetService.AddGroup("Second");
        var secondGroupId = _presetService.Groups[1].Id;
        _presetService.SwitchGroup(secondGroupId);

        _presetService.RemoveGroup(secondGroupId);

        _presetService.ActiveGroupId.Should().Be("default");
    }

    [Fact]
    public void RemoveGroup_RaisesGroupsChanged()
    {
        _presetService.AddGroup("Second");
        var secondGroupId = _presetService.Groups[1].Id;
        var raised = false;
        _presetService.GroupsChanged += () => raised = true;

        _presetService.RemoveGroup(secondGroupId);

        raised.Should().BeTrue();
    }

    [Fact]
    public void RenameGroup_UpdatesGroupName()
    {
        _presetService.RenameGroup("default", "Main Camera");

        _presetService.Groups[0].Name.Should().Be("Main Camera");
    }

    [Fact]
    public void RenameGroup_PersistsData()
    {
        _presetService.RenameGroup("default", "Main Camera");

        _repository.Verify(r => r.Save(It.IsAny<PresetData>()), Times.Once);
    }

    [Fact]
    public void RenameGroup_RaisesGroupsChanged()
    {
        var raised = false;
        _presetService.GroupsChanged += () => raised = true;

        _presetService.RenameGroup("default", "Main Camera");

        raised.Should().BeTrue();
    }

    [Fact]
    public void RenameGroup_WithInvalidId_DoesNothing()
    {
        _presetService.RenameGroup("nonexistent", "Whatever");

        _repository.Verify(r => r.Save(It.IsAny<PresetData>()), Times.Never);
    }

    [Fact]
    public void SwitchGroup_PresetsReflectNewGroup()
    {
        _presetService.AddGroup("Second");
        var secondGroupId = _presetService.Groups[1].Id;

        _presetService.SwitchGroup(secondGroupId);

        _presetService.Presets.Should().HaveCount(10);
        _presetService.Presets[0].SlotIndex.Should().NotBe(0);
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
