namespace ViscaCamLink.Infrastructure.HotKeys;

using System;
using System.Windows;
using System.Windows.Input;

public interface IHotKeyManager : IDisposable
{
    bool UseGlobalHotKeys { get; set; }

    void AddLocalKeyTarget(Window window);

    bool RegisterHotKey(ModifierKeys modifier, Key key, Action action);

    bool RegisterHoldHotKey(ModifierKeys modifier, Key key, Action pressAction, Action releaseAction);

    void UnregisterAll();
}
