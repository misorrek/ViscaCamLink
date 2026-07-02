namespace ViscaCamLink.Services;

using ViscaCamLink.Repositories.Presets;
using ViscaCamLink.Visca;

public sealed class PresetService : IPresetService
{
    private const int PresetsPerGroup = 10;
    private const int MaxCameraSlots = 256;

    private readonly IViscaController _viscaController;
    private readonly IPresetRepository _repository;
    private readonly PresetData _data;

    private string _activeGroupId;

    public PresetService(IViscaController viscaController, IPresetRepository repository)
    {
        _viscaController = viscaController;
        _repository = repository;
        _data = _repository.Load();
        _activeGroupId = _data.Groups[0].Id;
    }

    public IReadOnlyList<PresetMetadata> Presets => ActiveGroup.Presets;

    public IReadOnlyList<PresetGroup> Groups => _data.Groups;

    public string ActiveGroupId => _activeGroupId;

    public event Action? PresetsChanged;

    public event Action? GroupsChanged;

    private PresetGroup ActiveGroup => _data.Groups.First(g => g.Id == _activeGroupId);

    public Task SetMemoryAsync(byte slot) => _viscaController.MemorySet(slot);

    public Task RecallMemoryAsync(byte slot) => _viscaController.MemoryRecall(slot);

    public string GetPresetName(int slotIndex)
    {
        var preset = ActiveGroup.Presets.FirstOrDefault(p => p.SlotIndex == slotIndex);

        return preset?.Name ?? slotIndex.ToString();
    }

    public void RenamePreset(int slotIndex, string name)
    {
        var preset = ActiveGroup.Presets.FirstOrDefault(p => p.SlotIndex == slotIndex);

        if (preset is null)
        {
            return;
        }

        preset.Name = name;

        _repository.Save(_data);
        PresetsChanged?.Invoke();
    }

    public void SwitchGroup(string groupId)
    {
        if (_data.Groups.All(g => g.Id != groupId))
        {
            return;
        }

        _activeGroupId = groupId;

        PresetsChanged?.Invoke();
    }

    public void AddGroup(string name)
    {
        var usedSlots = _data.Groups
            .SelectMany(g => g.Presets)
            .Select(p => p.SlotIndex)
            .ToHashSet();

        var nextBaseSlot = FindNextAvailableBaseSlot(usedSlots);

        if (nextBaseSlot < 0)
        {
            return; // No more camera slots available
        }

        var groupId = Guid.NewGuid().ToString("N")[..8];
        var presets = new List<PresetMetadata>(PresetsPerGroup);

        for (var i = 0; i < PresetsPerGroup; i++)
        {
            var slot = nextBaseSlot + i;

            if (slot >= MaxCameraSlots)
            {
                break;
            }

            presets.Add(new PresetMetadata
            {
                GroupId = groupId,
                SlotIndex = slot,
                Name = (i % PresetsPerGroup).ToString(),
            });
        }

        _data.Groups.Add(new PresetGroup
        {
            Id = groupId,
            Name = name,
            Presets = presets,
        });

        _repository.Save(_data);
        GroupsChanged?.Invoke();
    }

    public void RemoveGroup(string groupId)
    {
        if (_data.Groups.Count <= 1)
        {
            return; // Cannot remove the last group
        }

        var group = _data.Groups.FirstOrDefault(g => g.Id == groupId);

        if (group is null)
        {
            return;
        }

        _data.Groups.Remove(group);

        if (_activeGroupId == groupId)
        {
            _activeGroupId = _data.Groups[0].Id;
            PresetsChanged?.Invoke();
        }

        _repository.Save(_data);
        GroupsChanged?.Invoke();
    }

    public void RenameGroup(string groupId, string name)
    {
        var group = _data.Groups.FirstOrDefault(g => g.Id == groupId);

        if (group is null)
        {
            return;
        }

        group.Name = name;

        _repository.Save(_data);
        GroupsChanged?.Invoke();
    }

    private static int FindNextAvailableBaseSlot(HashSet<int> usedSlots)
    {
        for (var baseSlot = 0; baseSlot < MaxCameraSlots; baseSlot += PresetsPerGroup)
        {
            var allFree = true;

            for (var i = 0; i < PresetsPerGroup && baseSlot + i < MaxCameraSlots; i++)
            {
                if (usedSlots.Contains(baseSlot + i))
                {
                    allFree = false;
                    break;
                }
            }

            if (allFree)
            {
                return baseSlot;
            }
        }

        return -1;
    }
}
