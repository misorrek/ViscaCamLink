namespace ViscaCamLink.Repositories;

public interface IPresetRepository
{
    PresetData Load();

    void Save(PresetData data);
}
