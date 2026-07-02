namespace ViscaCamLink.Repositories.Presets;

public interface IPresetRepository
{
    PresetData Load();

    void Save(PresetData data);
}
