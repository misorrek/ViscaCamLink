namespace ViscaCamLink.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ViscaCamLink.Repositories.Presets;
using ViscaCamLink.Visca;

public class PresetService : IPresetService
{
    private readonly IViscaController _viscaController;
    private readonly IPresetRepository _repository;
    private readonly ISettingsService _settingsService;

    private PresetData _presetData;
    private Guid _activeGroupId;

    public PresetService(IViscaController viscaController, IPresetRepository repository, ISettingsService settingsService)
    {
        _viscaController = viscaController;
        _repository = repository;
        _settingsService = settingsService;
        _presetData = _repository.LoadForCameraProfile(_settingsService.ActiveCameraProfileId);
        _activeGroupId = _presetData.Groups[0].Id;
    }

    public event Action? PresetsChanged;

    public event Action? GroupsChanged;

    public IReadOnlyList<PresetMetadata> Presets => ActiveGroup.Presets;

    public IReadOnlyList<PresetGroup> Groups => _presetData.Groups;

    public Guid ActiveGroupId => _activeGroupId;

    private PresetGroup ActiveGroup => _presetData.Groups.First(group => group.Id == _activeGroupId);

    public Task SetMemoryAsync(byte slot) => _viscaController.MemorySetAsync(slot);

    public Task RecallMemoryAsync(byte slot) => _viscaController.MemoryRecallAsync(slot);

    public string GetPresetName(int slotIndex)
    {
        var preset = ActiveGroup.Presets.FirstOrDefault(candidate => candidate.SlotIndex == slotIndex);

        return preset?.Name ?? slotIndex.ToString();
    }

    public void RenamePreset(int slotIndex, string name)
    {
        var preset = ActiveGroup.Presets.FirstOrDefault(candidate => candidate.SlotIndex == slotIndex);

        if (preset is null)
        {
            return;
        }

        preset.Name = name;

        SavePresetData();
        PresetsChanged?.Invoke();
    }

    public void SwitchGroup(Guid groupId)
    {
        if (_presetData.Groups.All(group => group.Id != groupId))
        {
            return;
        }

        _activeGroupId = groupId;

        PresetsChanged?.Invoke();
    }

    public void AddGroup(string name)
    {
        var usedSlots = _presetData.Groups
            .SelectMany(group => group.Presets)
            .Select(preset => preset.SlotIndex)
            .ToHashSet();

        var nextBaseSlot = FindNextAvailableBaseSlot(usedSlots);

        if (nextBaseSlot is null)
        {
            return;
        }

        var groupId = Guid.NewGuid();
        var presets = new List<PresetMetadata>(PresetLayout.PresetsPerGroup);

        for (var presetIndex = 0; presetIndex < PresetLayout.PresetsPerGroup; presetIndex++)
        {
            var slot = nextBaseSlot.Value + presetIndex;

            if (slot >= PresetLayout.MaxCameraSlots)
            {
                break;
            }

            presets.Add(new PresetMetadata
            {
                GroupId = groupId,
                SlotIndex = slot,
                Name = presetIndex.ToString(),
            });
        }

        _presetData.Groups.Add(new PresetGroup
        {
            Id = groupId,
            Name = name,
            Presets = presets,
        });

        SavePresetData();
        GroupsChanged?.Invoke();
    }

    public void RemoveGroup(Guid groupId)
    {
        // The presets UI always needs an active group, so the last one cannot be removed.
        if (_presetData.Groups.Count <= 1)
        {
            return;
        }

        var group = _presetData.Groups.FirstOrDefault(candidate => candidate.Id == groupId);

        if (group is null)
        {
            return;
        }

        _presetData.Groups.Remove(group);

        if (_activeGroupId == groupId)
        {
            _activeGroupId = _presetData.Groups[0].Id;

            PresetsChanged?.Invoke();
        }

        SavePresetData();
        GroupsChanged?.Invoke();
    }

    public void RenameGroup(Guid groupId, string name)
    {
        var group = _presetData.Groups.FirstOrDefault(candidate => candidate.Id == groupId);

        if (group is null)
        {
            return;
        }

        group.Name = name;

        SavePresetData();
        GroupsChanged?.Invoke();
    }

    public void SwitchCameraProfile(Guid profileId)
    {
        _presetData = _repository.LoadForCameraProfile(profileId);
        _activeGroupId = _presetData.Groups[0].Id;

        GroupsChanged?.Invoke();
        PresetsChanged?.Invoke();
    }

    private void SavePresetData() =>
        _repository.SaveForCameraProfile(_settingsService.ActiveCameraProfileId, _presetData);

    private static int? FindNextAvailableBaseSlot(HashSet<int> usedSlots)
    {
        for (var baseSlot = 0; baseSlot < PresetLayout.MaxCameraSlots; baseSlot += PresetLayout.PresetsPerGroup)
        {
            var allFree = true;

            for (var presetIndex = 0; presetIndex < PresetLayout.PresetsPerGroup && baseSlot + presetIndex < PresetLayout.MaxCameraSlots; presetIndex++)
            {
                if (usedSlots.Contains(baseSlot + presetIndex))
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

        return null;
    }
}
