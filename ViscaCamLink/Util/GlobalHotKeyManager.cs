namespace ViscaCamLink.Util;

using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;

public class GlobalHotKeyManager : IDisposable
{
    // Registers a hot key with Windows
    [DllImport("User32.dll")]
    private static extern Boolean RegisterHotKey(
        [In] IntPtr hWnd,
        [In] Int32 id,
        [In] UInt32 fsModifiers,
        [In] UInt32 vk);

    // Unregisters a hot key with Windows
    [DllImport("User32.dll")]
    private static extern Boolean UnregisterHotKey(
        [In] IntPtr hWnd,
        [In] Int32 id);

    public GlobalHotKeyManager(Window mainWindow)
    {
        var windowInteropHelper = new WindowInteropHelper(mainWindow);

        MainWindowHandle = windowInteropHelper.Handle;

        HwndSource = HwndSource.FromHwnd(MainWindowHandle);
        HwndSource.AddHook(HwndHook);
    }

    private IntPtr MainWindowHandle { get; }

    private HwndSource HwndSource { get; }

    private Int32 CurrentHotKeyId { get; set; } = 0;

    private Dictionary<Int32, Action> RegisteredHotKeys { get; } = new Dictionary<int, Action>();

    public Boolean RegisterHotKey(ModifierKeys modifier, Key key, Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var modifierCode = Convert.ToUInt32(modifier);
        var virtualKeyCode = Convert.ToUInt32(KeyInterop.VirtualKeyFromKey(key));

        CurrentHotKeyId++;

        Boolean registered = RegisterHotKey(MainWindowHandle, CurrentHotKeyId, modifierCode, virtualKeyCode);

        if (registered)
        {
            RegisteredHotKeys.Add(CurrentHotKeyId, action);
        }
        return registered;
    }

    private IntPtr HwndHook(IntPtr hwnd, Int32 msg, IntPtr wParam, IntPtr lParam, ref Boolean handled)
    {
        const int WM_HOTKEY = 0x0312;

        switch (msg)
        {
            case WM_HOTKEY:
                var hotKeyId = wParam.ToInt32();
                Action? action;

                if (RegisteredHotKeys.TryGetValue(hotKeyId, out action))
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

        for (var id = CurrentHotKeyId; id > 0; id--)
        {
            UnregisterHotKey(MainWindowHandle, id);
        }
    }
}
