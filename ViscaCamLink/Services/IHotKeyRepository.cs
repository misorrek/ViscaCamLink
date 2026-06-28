namespace ViscaCamLink.Services;

public interface IHotKeyRepository
{
    IReadOnlyList<HotKeyBinding> Load();

    void Save(IReadOnlyList<HotKeyBinding> bindings);
}