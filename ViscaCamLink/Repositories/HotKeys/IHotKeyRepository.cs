namespace ViscaCamLink.Repositories.HotKeys;

public interface IHotKeyRepository
{
    IReadOnlyList<HotKeyBinding> Load();

    void Save(IReadOnlyList<HotKeyBinding> bindings);
}