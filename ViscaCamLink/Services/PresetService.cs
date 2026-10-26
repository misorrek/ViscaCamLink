namespace ViscaCamLink.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Repositories.Presets;
using ViscaCamLink.Visca;

public partial class PresetService : IPresetService
{
    private readonly IViscaController _viscaController;
    private readonly IPresetRepository _repository;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<PresetService> _logger;

    private PresetData _presetData;
    private Guid _activeGroupId;

    public PresetService(
        IViscaController viscaController,
        IPresetRepository repository,
        ISettingsService settingsService,
        ILogger<PresetService> logger)
    {
        _viscaController = viscaController;
        _repository = repository;
        _settingsService = settingsService;
        _logger = logger;
        _presetData = _repository.LoadForCameraProfile(_settingsService.ActiveCameraProfileId);
        _activeGroupId = ResolveActiveGroupId(_presetData);
        _presetData.ActiveGroupId = _activeGroupId;
    }

    public event Action? PresetsChanged;

    public event Action? GroupsChanged;

    public IReadOnlyList<PresetMetadata> Presets => ActiveGroup.Presets;

    public IReadOnlyList<PresetGroup> Groups => _presetData.Groups;

    public Guid ActiveGroupId => _activeGroupId;

    public bool CanAddGroup => FindNextAvailableBaseSlot(GetUsedSlots()) is not null;

    private PresetGroup ActiveGroup => _presetData.Groups.First(group => group.Id == _activeGroupId);

    public async Task SetMemoryAsync(byte slot)
    {
        LogSavingPreset(_logger, slot);

        try
        {
            await _viscaController.MemorySetAsync(slot).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Logged here with context and rethrown; callers (TryCameraOperation) swallow without logging.
            LogSavePresetFailed(_logger, slot, exception);

            throw;
        }
    }

    public async Task RecallMemoryAsync(byte slot)
    {
        LogRecallingPreset(_logger, slot);

        try
        {
            await _viscaController.MemoryRecallAsync(slot).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Logged here with context and rethrown; callers (TryCameraOperation) swallow without logging.
            LogRecallPresetFailed(_logger, slot, exception);

            throw;
        }
    }

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
        _presetData.ActiveGroupId = groupId;

        SavePresetData();
        PresetsChanged?.Invoke();
    }

    public void AddGroup(string name)
    {
        var nextBaseSlot = FindNextAvailableBaseSlot(GetUsedSlots());

        if (nextBaseSlot is null)
        {
            LogGroupLimitReached(_logger, name);

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
            _presetData.ActiveGroupId = _activeGroupId;

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
        _activeGroupId = ResolveActiveGroupId(_presetData);
        _presetData.ActiveGroupId = _activeGroupId;

        GroupsChanged?.Invoke();
        PresetsChanged?.Invoke();
    }

    private void SavePresetData() =>
        _repository.SaveForCameraProfile(_settingsService.ActiveCameraProfileId, _presetData);

    private HashSet<int> GetUsedSlots()
    {
        return _presetData.Groups
            .SelectMany(group => group.Presets)
            .Select(preset => preset.SlotIndex)
            .ToHashSet();
    }

    private static Guid ResolveActiveGroupId(PresetData presetData)
    {
        var activeGroupId = presetData.ActiveGroupId;

        if (presetData.Groups.Any(group => group.Id == activeGroupId))
        {
            return activeGroupId;
        }

        return presetData.Groups[0].Id;
    }

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

    [LoggerMessage(EventId = 2201, Level = LogLevel.Information, Message = "Saving current position to preset slot {Slot}")]
    private static partial void LogSavingPreset(ILogger logger, byte slot);

    [LoggerMessage(EventId = 2202, Level = LogLevel.Error, Message = "Saving preset slot {Slot} failed")]
    private static partial void LogSavePresetFailed(ILogger logger, byte slot, Exception exception);

    [LoggerMessage(EventId = 2203, Level = LogLevel.Information, Message = "Recalling preset slot {Slot}")]
    private static partial void LogRecallingPreset(ILogger logger, byte slot);

    [LoggerMessage(EventId = 2204, Level = LogLevel.Error, Message = "Recalling preset slot {Slot} failed")]
    private static partial void LogRecallPresetFailed(ILogger logger, byte slot, Exception exception);

    [LoggerMessage(EventId = 2205, Level = LogLevel.Warning, Message = "Preset group '{Name}' was not added because all camera slots are in use")]
    private static partial void LogGroupLimitReached(ILogger logger, string name);
}
