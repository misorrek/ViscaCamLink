namespace ViscaCamLink.Repositories.Presets;

public interface IPresetRepository
{
    PresetData LoadForCameraProfile(Guid profileId);

    void SaveForCameraProfile(Guid profileId, PresetData presetData);
}
