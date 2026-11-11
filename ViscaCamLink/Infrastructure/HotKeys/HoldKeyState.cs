namespace ViscaCamLink.Infrastructure.HotKeys;

using System.Collections.Generic;
using System.Linq;

public sealed class HoldKeyState<TKey> where TKey : notnull
{
    private readonly HashSet<TKey> _activeKeys = [];

    public bool Press(TKey key) => _activeKeys.Add(key);

    public bool Release(TKey key) => _activeKeys.Remove(key);

    public TKey[] ReleaseAll()
    {
        var activeKeys = _activeKeys.ToArray();
        _activeKeys.Clear();
        return activeKeys;
    }
}
