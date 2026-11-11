namespace ViscaCamLink.Infrastructure.HotKeys;

using System.Windows.Input;

public static class HotKeyMapping
{
    public static uint ToModifierCode(ModifierKeys modifier) => (uint)modifier;

    public static uint ToVirtualKeyCode(Key key) => (uint)KeyInterop.VirtualKeyFromKey(key);
}
