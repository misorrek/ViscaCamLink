namespace ViscaCamLink.Util
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Interop;

    public class GlobalHotKey : IDisposable
    {
        public static bool RegisterHotKey(ModifierKeys aModifier, Key aKey, Action aAction)
        {
            if (aAction is null)
            {
                throw new ArgumentNullException(nameof(aAction));
            }

            var aVirtualKeyCode = Convert.ToUInt32(KeyInterop.VirtualKeyFromKey(aKey));
            currentID = currentID + 1;
            bool aRegistered = RegisterHotKey(window.Handle, currentID,
                                        (uint)aModifier | MOD_NOREPEAT,
                                        (uint)aVirtualKeyCode);

            if (aRegistered)
            {
                registeredHotKeys.Add(new HotKeyWithAction(aModifier, aKey, aAction));
            }
            return aRegistered;
        }

        public void Dispose()
        {
            // unregister all the registered hot keys.
            for (int i = currentID; i > 0; i--)
            {
                UnregisterHotKey(window.Handle, i);
            }

            // dispose the inner native window.
            window.Dispose();
        }

        static GlobalHotKey()
        {
            window.KeyPressed += (s, e) =>
            {
                registeredHotKeys.ForEach(x =>
                {
                    if (e.Modifier == x.Modifier && e.Key == x.Key)
                    {
                        x.Action();
                    }
                });
            };
        }

        private static readonly InvisibleWindowForMessages window = new InvisibleWindowForMessages();
        private static int currentID;
        private static uint MOD_NOREPEAT = 0x4000;
        private static List<HotKeyWithAction> registeredHotKeys = new List<HotKeyWithAction>();

        private class HotKeyWithAction
        {

            public HotKeyWithAction(ModifierKeys modifier, Key key, Action action)
            {
                Modifier = modifier;
                Key = key;
                Action = action;
            }

            public ModifierKeys Modifier { get; }
            public Key Key { get; }
            public Action Action { get; }
        }

        // Registers a hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        // Unregisters the hot key with Windows.
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private class InvisibleWindowForMessages : Window, IDisposable
        {
            public InvisibleWindowForMessages()
            {              
                var helper = new WindowInteropHelper(this);

                helper.EnsureHandle();

                Handle = helper.Handle;
                Source = HwndSource.FromHwnd(Handle);

                Source.AddHook(HwndHook);
            }

            private HwndSource Source { get; }

            public IntPtr Handle { get; } 

            private static int WM_HOTKEY = 0x0312;
            private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == WM_HOTKEY)
                {
                    var aWPFKey = KeyInterop.KeyFromVirtualKey(((int)lParam >> 16) & 0xFFFF);
                    ModifierKeys modifier = (ModifierKeys)((int)lParam & 0xFFFF);

                    if (KeyPressed != null)
                    {
                        KeyPressed(this, new HotKeyPressedEventArgs(modifier, aWPFKey));
                        handled = true;
                    }
                }
                return IntPtr.Zero;
            }

            public class HotKeyPressedEventArgs : EventArgs
            {
                private ModifierKeys _modifier;
                private Key _key;

                internal HotKeyPressedEventArgs(ModifierKeys modifier, Key key)
                {
                    _modifier = modifier;
                    _key = key;
                }

                public ModifierKeys Modifier
                {
                    get { return _modifier; }
                }

                public Key Key
                {
                    get { return _key; }
                }
            }


            public event EventHandler<HotKeyPressedEventArgs>? KeyPressed;

            #region IDisposable Members

            public void Dispose()
            {
                Source.RemoveHook(HwndHook);
            }

            #endregion
        }
    }
}
