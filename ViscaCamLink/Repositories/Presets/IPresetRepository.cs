namespace ViscaCamLink.Repositories.Presets;

using System;

public interface IPresetRepository
{
    PresetData LoadForCameraProfile(Guid profileId);

    void SaveForCameraProfile(Guid profileId, PresetData presetData);
}
