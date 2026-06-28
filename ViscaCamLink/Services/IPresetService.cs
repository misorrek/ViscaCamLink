using ViscaCamLink.Repositories;

namespace ViscaCamLink.Services;

public interface IPresetService
{
    IReadOnlyList<PresetMetadata> Presets { get; }

    IReadOnlyList<PresetGroup> Groups { get; }

    string ActiveGroupId { get; }

    event Action? PresetsChanged;

    event Action? GroupsChanged;

    Task SetMemoryAsync(byte slot);
    Task RecallMemoryAsync(byte slot);
    string GetPresetName(int slotIndex);
    void RenamePreset(int slotIndex, string name);
    void SwitchGroup(string groupId);
    void AddGroup(string name);
    void RemoveGroup(string groupId);
    void RenameGroup(string groupId, string name);
}
