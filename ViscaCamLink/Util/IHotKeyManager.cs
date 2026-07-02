namespace ViscaCamLink.Util;

using System.Windows.Input;

public interface IHotKeyManager : IDisposable
{
    bool UseGlobalHotKeys { get; set; }

    bool RegisterHotKey(ModifierKeys modifier, Key key, Action action);

    bool RegisterHoldHotKey(ModifierKeys modifier, Key key, Action pressAction, Action releaseAction);

    void UnregisterAll();
}
