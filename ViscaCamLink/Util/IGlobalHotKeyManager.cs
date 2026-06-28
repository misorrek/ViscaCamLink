namespace ViscaCamLink.Util;

using System.Windows.Input;

public interface IGlobalHotKeyManager : IDisposable
{
    bool RegisterHotKey(ModifierKeys modifier, Key key, Action action);

    void UnregisterAll();
}
