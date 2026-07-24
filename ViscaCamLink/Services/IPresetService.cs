namespace ViscaCamLink.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ViscaCamLink.Repositories.Presets;

public interface IPresetService
{
    IReadOnlyList<PresetMetadata> Presets { get; }

    IReadOnlyList<PresetGroup> Groups { get; }

    Guid ActiveGroupId { get; }

    event Action? PresetsChanged;

    event Action? GroupsChanged;

    Task SetMemoryAsync(byte slot);

    Task RecallMemoryAsync(byte slot);

    string GetPresetName(int slotIndex);

    void RenamePreset(int slotIndex, string name);

    void SwitchGroup(Guid groupId);

    void AddGroup(string name);

    void RemoveGroup(Guid groupId);

    void RenameGroup(Guid groupId, string name);

    void SwitchCameraProfile(Guid profileId);
}
