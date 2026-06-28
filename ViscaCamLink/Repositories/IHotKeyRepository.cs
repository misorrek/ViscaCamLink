namespace ViscaCamLink.Repositories;

public interface IHotKeyRepository
{
    IReadOnlyList<HotKeyBinding> Load();

    void Save(IReadOnlyList<HotKeyBinding> bindings);
}