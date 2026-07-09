namespace ViscaCamLink.Infrastructure.Interface;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

public partial class HotKeyManager : IHotKeyManager
{
    private class LocalKeyTargetState(Window window)
    {
        public Window Window { get; } = window;

        public bool IsKeyDownHandlerAttached { get; set; }

        public bool IsKeyUpHandlerAttached { get; set; }
    }

    [LibraryImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll")]
    private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string? lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;

    private readonly List<LocalKeyTargetState> _localKeyTargets = [];

    public HotKeyManager(Window mainWindow)
    {
        _localKeyTargets.Add(new LocalKeyTargetState(mainWindow));

        var windowInteropHelper = new WindowInteropHelper(mainWindow);
        windowInteropHelper.EnsureHandle();

        MainWindowHandle = windowInteropHelper.Handle;

        HwndSource = HwndSource.FromHwnd(MainWindowHandle);
        HwndSource.AddHook(HwndHook);
    }

    private IntPtr MainWindowHandle { get; }
    private HwndSource HwndSource { get; }
    private int CurrentHotKeyId { get; set; } = 0;

    // Global press registrations: hotkey ID -> action
    private Dictionary<int, Action> GlobalRegistrations { get; } = [];

    // Global hold registrations: hotkey ID -> (virtual key code, release action)
    private Dictionary<int, (uint Vk, Action ReleaseAction)> GlobalHoldRegistrations { get; } = [];

    // Currently active (pressed) global hold keys, tracked by virtual key code
    private HashSet<uint> ActiveGlobalHoldVks { get; } = [];

    // LL hook state
    private LowLevelKeyboardProc? _llKeyboardProc;
    private IntPtr _llHookHandle = IntPtr.Zero;

    // Local press registrations: (modifier, key) -> action
    private Dictionary<(ModifierKeys, Key), Action> LocalRegistrations { get; } = [];

    // Local hold registrations: key -> release action (matched by key only on release)
    private Dictionary<Key, Action> LocalHoldReleaseCallbacks { get; } = [];

    // Currently active local hold keys
    private HashSet<Key> ActiveLocalHoldKeys { get; } = [];

    public bool UseGlobalHotKeys { get; set; } = true;

    public void AddLocalKeyTarget(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (_localKeyTargets.Any(target => ReferenceEquals(target.Window, window)))
        {
            return;
        }

        var target = new LocalKeyTargetState(window);

        _localKeyTargets.Add(target);

        if (LocalRegistrations.Count > 0)
        {
            AttachLocalKeyDownHandler(target);
        }

        if (LocalHoldReleaseCallbacks.Count > 0)
        {
            AttachLocalKeyUpHandler(target);
        }
    }

    public bool RegisterHotKey(ModifierKeys modifier, Key key, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return UseGlobalHotKeys
            ? RegisterGlobalHotKey(modifier, key, action)
            : RegisterLocalHotKey(modifier, key, action);
    }

    public bool RegisterHoldHotKey(ModifierKeys modifier, Key key, Action pressAction, Action releaseAction)
    {
        ArgumentNullException.ThrowIfNull(pressAction);
        ArgumentNullException.ThrowIfNull(releaseAction);

        return UseGlobalHotKeys
            ? RegisterGlobalHoldHotKey(modifier, key, pressAction, releaseAction)
            : RegisterLocalHoldHotKey(modifier, key, pressAction, releaseAction);
    }

    public void UnregisterAll()
    {
        foreach (var id in GlobalRegistrations.Keys.ToList())
        {
            UnregisterHotKey(MainWindowHandle, id);
        }
            
        GlobalRegistrations.Clear();
        GlobalHoldRegistrations.Clear();
        ActiveGlobalHoldVks.Clear();
        RemoveLowLevelHook();

        foreach (var target in _localKeyTargets)
        {
            if (target.IsKeyDownHandlerAttached)
            {
                target.Window.PreviewKeyDown -= OnLocalPreviewKeyDown;
                target.IsKeyDownHandlerAttached = false;
            }

            if (target.IsKeyUpHandlerAttached)
            {
                target.Window.PreviewKeyUp -= OnLocalPreviewKeyUp;
                target.IsKeyUpHandlerAttached = false;
            }
        }

        LocalRegistrations.Clear();
        LocalHoldReleaseCallbacks.Clear();
        ActiveLocalHoldKeys.Clear();
    }

    private bool RegisterGlobalHotKey(ModifierKeys modifier, Key key, Action action)
    {
        var modifierCode = Convert.ToUInt32(modifier);
        var virtualKeyCode = Convert.ToUInt32(KeyInterop.VirtualKeyFromKey(key));

        CurrentHotKeyId++;

        var registered = RegisterHotKey(MainWindowHandle, CurrentHotKeyId, modifierCode, virtualKeyCode);
        
        if (registered)
        {
            GlobalRegistrations.Add(CurrentHotKeyId, action);
        }

        return registered;
    }

    private bool RegisterGlobalHoldHotKey(ModifierKeys modifier, Key key, Action pressAction, Action releaseAction)
    {
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        var result = RegisterGlobalHotKey(modifier, key, pressAction);

        if (result)
        {
            GlobalHoldRegistrations[CurrentHotKeyId] = (vk, releaseAction);
            EnsureLowLevelHook();
        }

        return result;
    }

    private bool RegisterLocalHotKey(ModifierKeys modifier, Key key, Action action)
    {
        AttachLocalKeyDownHandlersToAllTargets();

        LocalRegistrations[(modifier, key)] = action;
        return true;
    }

    private bool RegisterLocalHoldHotKey(ModifierKeys modifier, Key key, Action pressAction, Action releaseAction)
    {
        var result = RegisterLocalHotKey(modifier, key, pressAction);
        
        if (result)
        {
            LocalHoldReleaseCallbacks[key] = releaseAction;
            AttachLocalKeyUpHandlersToAllTargets();
        }

        return result;
    }

    private void AttachLocalKeyDownHandlersToAllTargets()
    {
        foreach (var target in _localKeyTargets)
        {
            AttachLocalKeyDownHandler(target);
        }
    }

    private void AttachLocalKeyUpHandlersToAllTargets()
    {
        foreach (var target in _localKeyTargets)
        {
            AttachLocalKeyUpHandler(target);
        }
    }

    private void AttachLocalKeyDownHandler(LocalKeyTargetState target)
    {
        if (target.IsKeyDownHandlerAttached)
        {
            return;
        }

        target.Window.PreviewKeyDown += OnLocalPreviewKeyDown;
        target.IsKeyDownHandlerAttached = true;
    }

    private void AttachLocalKeyUpHandler(LocalKeyTargetState target)
    {
        if (target.IsKeyUpHandlerAttached)
        {
            return;
        }

        target.Window.PreviewKeyUp += OnLocalPreviewKeyUp;
        target.IsKeyUpHandlerAttached = true;
    }

    private void EnsureLowLevelHook()
    {
        if (_llHookHandle != IntPtr.Zero) 
        {
            return;
        }

        _llKeyboardProc = LowLevelKeyboardHook;
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        var mainModule = curProcess.MainModule;
        _llHookHandle = SetWindowsHookEx(
            WH_KEYBOARD_LL, 
            Marshal.GetFunctionPointerForDelegate(_llKeyboardProc),
            GetModuleHandle(mainModule?.ModuleName),
            0);
    }

    private void RemoveLowLevelHook()
    {
        if (_llHookHandle == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_llHookHandle);

        _llHookHandle = IntPtr.Zero;
        _llKeyboardProc = null;
    }

    private IntPtr LowLevelKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam.ToInt32() == WM_KEYUP || wParam.ToInt32() == WM_SYSKEYUP))
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            
            if (ActiveGlobalHoldVks.Remove(hookStruct.vkCode))
            {
                foreach (var (Vk, ReleaseAction) in GlobalHoldRegistrations.Values)
                {
                    if (Vk == hookStruct.vkCode)
                    {
                        ReleaseAction.Invoke();
                        break;
                    }
                }
            }
        }

        return CallNextHookEx(_llHookHandle, nCode, wParam, lParam);
    }

    private void OnLocalPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.IsRepeat) 
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var modifier = Keyboard.Modifiers;

        if (LocalRegistrations.TryGetValue((modifier, key), out var action))
        {
            if (LocalHoldReleaseCallbacks.ContainsKey(key))
            {
                ActiveLocalHoldKeys.Add(key);
            }
                
            action.Invoke();
            e.Handled = true;
        }
    }

    private void OnLocalPreviewKeyUp(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (ActiveLocalHoldKeys.Remove(key) && LocalHoldReleaseCallbacks.TryGetValue(key, out var releaseAction))
        {
            releaseAction.Invoke();
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;

        if (msg == WM_HOTKEY)
        {
            var hotKeyId = wParam.ToInt32();

            if (GlobalRegistrations.TryGetValue(hotKeyId, out var action))
            {
                if (GlobalHoldRegistrations.TryGetValue(hotKeyId, out var holdInfo))
                {
                    ActiveGlobalHoldVks.Add(holdInfo.Vk);
                }

                action.Invoke();
            }
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
