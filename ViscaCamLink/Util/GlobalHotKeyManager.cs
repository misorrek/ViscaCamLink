namespace ViscaCamLink.Util;

using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;

public sealed class GlobalHotKeyManager : IGlobalHotKeyManager
{
    [DllImport("User32.dll")]
    private static extern bool RegisterHotKey(
        [In] IntPtr hWnd,
        [In] int id,
        [In] uint fsModifiers,
        [In] uint vk);

    [DllImport("User32.dll")]
    private static extern bool UnregisterHotKey(
        [In] IntPtr hWnd,
        [In] int id);

    public GlobalHotKeyManager(Window mainWindow)
    {
        var windowInteropHelper = new WindowInteropHelper(mainWindow);
        windowInteropHelper.EnsureHandle();

        MainWindowHandle = windowInteropHelper.Handle;

        HwndSource = HwndSource.FromHwnd(MainWindowHandle);
        HwndSource.AddHook(HwndHook);
    }

    private IntPtr MainWindowHandle { get; }

    private HwndSource HwndSource { get; }

    private int CurrentHotKeyId { get; set; } = 0;

    private Dictionary<int, Action> RegisteredHotKeys { get; } = [];

    public bool RegisterHotKey(ModifierKeys modifier, Key key, Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var modifierCode = Convert.ToUInt32(modifier);
        var virtualKeyCode = Convert.ToUInt32(KeyInterop.VirtualKeyFromKey(key));

        CurrentHotKeyId++;

        bool registered = RegisterHotKey(MainWindowHandle, CurrentHotKeyId, modifierCode, virtualKeyCode);

        if (registered)
        {
            RegisteredHotKeys.Add(CurrentHotKeyId, action);
        }
        return registered;
    }

    public void UnregisterAll()
    {
        foreach (var id in RegisteredHotKeys.Keys.ToList())
        {
            UnregisterHotKey(MainWindowHandle, id);
            RegisteredHotKeys.Remove(id);
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;

        switch (msg)
        {
            case WM_HOTKEY:
                var hotKeyId = wParam.ToInt32();
                if (RegisteredHotKeys.TryGetValue(hotKeyId, out var action))
                {
                    action.Invoke();
                }
                break;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        HwndSource.RemoveHook(HwndHook);
        UnregisterAll();

        GC.SuppressFinalize(this);
    }
}
