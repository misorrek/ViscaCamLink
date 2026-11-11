namespace ViscaCamLink.Infrastructure.HotKeys;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Microsoft.Extensions.Logging;

public sealed partial class HotKeyManager : IHotKeyManager
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;
    private const int WM_HOTKEY = 0x0312;

    private readonly IntPtr _mainWindowHandle;
    private readonly HwndSource _hwndSource;
    private readonly ILogger<HotKeyManager> _logger;
    private readonly List<LocalKeyTargetState> _localKeyTargets = [];

    private readonly Dictionary<int, Action> _globalRegistrations = [];
    private readonly Dictionary<int, (uint VirtualKeyCode, Action ReleaseAction)> _globalHoldRegistrations = [];
    private readonly HashSet<uint> _activeGlobalHoldKeys = [];

    private readonly Dictionary<(ModifierKeys Modifier, Key Key), Action> _localRegistrations = [];
    private readonly Dictionary<Key, Action> _localHoldReleaseCallbacks = [];
    private readonly HashSet<Key> _activeLocalHoldKeys = [];

    private int _currentHotKeyId;
    private LowLevelKeyboardProc? _lowLevelKeyboardProc;
    private IntPtr _lowLevelHookHandle = IntPtr.Zero;
    private bool _lowLevelHookInstallationFailed;

    public HotKeyManager(Window mainWindow, ILogger<HotKeyManager> logger)
    {
        _logger = logger;
        _localKeyTargets.Add(new LocalKeyTargetState(mainWindow));

        var windowInteropHelper = new WindowInteropHelper(mainWindow);

        windowInteropHelper.EnsureHandle();

        _mainWindowHandle = windowInteropHelper.Handle;
        _hwndSource = HwndSource.FromHwnd(_mainWindowHandle);
        _hwndSource.AddHook(HwndHook);
    }

    public void Dispose()
    {
        _hwndSource.RemoveHook(HwndHook);
        UnregisterAll();
        GC.SuppressFinalize(this);
    }

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

        if (_localRegistrations.Count > 0)
        {
            AttachLocalKeyDownHandler(target);
        }

        if (_localHoldReleaseCallbacks.Count > 0)
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
        _lowLevelHookInstallationFailed = false;

        foreach (var id in _globalRegistrations.Keys.ToList())
        {
            UnregisterHotKey(_mainWindowHandle, id);
        }

        RemoveLowLevelHook();
        _globalRegistrations.Clear();
        _globalHoldRegistrations.Clear();

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

            if (target.IsDeactivatedHandlerAttached)
            {
                target.Window.Deactivated -= OnLocalTargetDeactivated;
                target.IsDeactivatedHandlerAttached = false;
            }
        }

        ReleaseActiveLocalHoldKeys();
        _localRegistrations.Clear();
        _localHoldReleaseCallbacks.Clear();
    }

    private bool RegisterGlobalHotKey(ModifierKeys modifier, Key key, Action action)
    {
        var modifierCode = (uint)modifier;
        var virtualKeyCode = (uint)KeyInterop.VirtualKeyFromKey(key);

        _currentHotKeyId++;

        var registered = RegisterHotKey(_mainWindowHandle, _currentHotKeyId, modifierCode, virtualKeyCode);

        if (registered)
        {
            _globalRegistrations.Add(_currentHotKeyId, action);
        }

        return registered;
    }

    private bool RegisterGlobalHoldHotKey(ModifierKeys modifier, Key key, Action pressAction, Action releaseAction)
    {
        if (!EnsureLowLevelHook())
        {
            return false;
        }

        var virtualKeyCode = (uint)KeyInterop.VirtualKeyFromKey(key);
        var registered = RegisterGlobalHotKey(modifier, key, pressAction);

        if (registered)
        {
            _globalHoldRegistrations[_currentHotKeyId] = (virtualKeyCode, releaseAction);
        }
        else if (_globalHoldRegistrations.Count == 0)
        {
            RemoveLowLevelHook();
        }

        return registered;
    }

    private bool RegisterLocalHotKey(ModifierKeys modifier, Key key, Action action)
    {
        AttachLocalKeyDownHandlersToAllTargets();

        _localRegistrations[(modifier, key)] = action;

        return true;
    }

    private bool RegisterLocalHoldHotKey(ModifierKeys modifier, Key key, Action pressAction, Action releaseAction)
    {
        var registered = RegisterLocalHotKey(modifier, key, pressAction);

        if (registered)
        {
            _localHoldReleaseCallbacks[key] = releaseAction;

            AttachLocalKeyUpHandlersToAllTargets();
        }

        return registered;
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
        target.Window.Deactivated += OnLocalTargetDeactivated;
        target.IsKeyUpHandlerAttached = true;
        target.IsDeactivatedHandlerAttached = true;
    }

    private bool EnsureLowLevelHook()
    {
        if (_lowLevelHookInstallationFailed)
        {
            return false;
        }

        if (_lowLevelHookHandle != IntPtr.Zero)
        {
            return true;
        }

        _lowLevelKeyboardProc = LowLevelKeyboardHook;

        using var currentProcess = Process.GetCurrentProcess();
        var mainModule = currentProcess.MainModule;

        _lowLevelHookHandle = SetWindowsHookEx(
            WH_KEYBOARD_LL,
            Marshal.GetFunctionPointerForDelegate(_lowLevelKeyboardProc),
            GetModuleHandle(mainModule?.ModuleName),
            0);

        if (_lowLevelHookHandle != IntPtr.Zero)
        {
            return true;
        }

        var errorCode = Marshal.GetLastWin32Error();

        LogLowLevelHookInstallationFailed(_logger, errorCode);
        _lowLevelKeyboardProc = null;
        _lowLevelHookInstallationFailed = true;

        return false;
    }

    private void RemoveLowLevelHook()
    {
        if (_lowLevelHookHandle == IntPtr.Zero)
        {
            return;
        }

        var unhooked = UnhookWindowsHookEx(_lowLevelHookHandle);
        var errorCode = unhooked ? 0 : Marshal.GetLastWin32Error();

        _lowLevelHookHandle = IntPtr.Zero;
        _lowLevelKeyboardProc = null;

        ReleaseActiveGlobalHoldKeys();
        ReleaseActiveLocalHoldKeys();

        if (!unhooked)
        {
            LogLowLevelHookRemovalFailed(_logger, errorCode);
        }
    }

    private IntPtr LowLevelKeyboardHook(int hookCode, IntPtr wParam, IntPtr lParam)
    {
        if (hookCode >= 0 && (wParam.ToInt32() == WM_KEYUP || wParam.ToInt32() == WM_SYSKEYUP))
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            ReleaseGlobalHoldKey(hookStruct.vkCode);
        }

        return CallNextHookEx(_lowLevelHookHandle, hookCode, wParam, lParam);
    }

    private void OnLocalPreviewKeyDown(object sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.IsRepeat)
        {
            return;
        }

        var key = eventArgs.Key == Key.System ? eventArgs.SystemKey : eventArgs.Key;
        var modifier = Keyboard.Modifiers;

        if (_localRegistrations.TryGetValue((modifier, key), out var action))
        {
            if (_localHoldReleaseCallbacks.ContainsKey(key))
            {
                _activeLocalHoldKeys.Add(key);
            }

            action.Invoke();
            eventArgs.Handled = true;
        }
    }

    private void OnLocalPreviewKeyUp(object sender, KeyEventArgs eventArgs)
    {
        var key = eventArgs.Key == Key.System ? eventArgs.SystemKey : eventArgs.Key;

        if (_activeLocalHoldKeys.Remove(key) && _localHoldReleaseCallbacks.TryGetValue(key, out var releaseAction))
        {
            releaseAction.Invoke();
        }
    }

    private void OnLocalTargetDeactivated(object? sender, EventArgs eventArgs) =>
        ReleaseActiveLocalHoldKeys();

    private void ReleaseActiveLocalHoldKeys()
    {
        foreach (var key in _activeLocalHoldKeys.ToArray())
        {
            if (_localHoldReleaseCallbacks.TryGetValue(key, out var releaseAction))
            {
                releaseAction.Invoke();
            }
        }

        _activeLocalHoldKeys.Clear();
    }

    private void ReleaseActiveGlobalHoldKeys()
    {
        foreach (var virtualKeyCode in _activeGlobalHoldKeys.ToArray())
        {
            ReleaseGlobalHoldKey(virtualKeyCode);
        }
    }

    private void ReleaseGlobalHoldKey(uint virtualKeyCode)
    {
        if (!_activeGlobalHoldKeys.Remove(virtualKeyCode))
        {
            return;
        }

        foreach (var (registeredVirtualKeyCode, releaseAction) in _globalHoldRegistrations.Values)
        {
            if (registeredVirtualKeyCode == virtualKeyCode)
            {
                releaseAction.Invoke();
                break;
            }
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WM_HOTKEY)
        {
            var hotKeyId = wParam.ToInt32();

            if (_globalRegistrations.TryGetValue(hotKeyId, out var action))
            {
                if (_globalHoldRegistrations.TryGetValue(hotKeyId, out var holdRegistration))
                {
                    _activeGlobalHoldKeys.Add(holdRegistration.VirtualKeyCode);
                }

                action.Invoke();
            }
        }

        return IntPtr.Zero;
    }

    [LibraryImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    private static partial IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll")]
    private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string? lpModuleName);

    [LoggerMessage(EventId = 2304, Level = LogLevel.Error, Message = "Could not install the low-level keyboard hook; global hold hotkeys will not be registered (error {ErrorCode})")]
    private static partial void LogLowLevelHookInstallationFailed(ILogger logger, int errorCode);

    [LoggerMessage(EventId = 2305, Level = LogLevel.Error, Message = "Could not remove the low-level keyboard hook (error {ErrorCode})")]
    private static partial void LogLowLevelHookRemovalFailed(ILogger logger, int errorCode);

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

    private sealed class LocalKeyTargetState(Window window)
    {
        public Window Window { get; } = window;

        public bool IsKeyDownHandlerAttached { get; set; }

        public bool IsKeyUpHandlerAttached { get; set; }

        public bool IsDeactivatedHandlerAttached { get; set; }
    }
}
