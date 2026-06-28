namespace ViscaCamLink.Services;

public interface IPresetRepository
{
    PresetData Load();
    void Save(PresetData data);
}
