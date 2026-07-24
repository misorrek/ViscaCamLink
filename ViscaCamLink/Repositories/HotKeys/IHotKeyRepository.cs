namespace ViscaCamLink.Repositories.HotKeys;

using System.Collections.Generic;

public interface IHotKeyRepository
{
    IReadOnlyList<HotKeyBinding> Load();

    void Save(IReadOnlyList<HotKeyBinding> bindings);
}
