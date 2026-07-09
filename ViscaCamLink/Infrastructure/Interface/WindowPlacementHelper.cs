namespace ViscaCamLink.Infrastructure.Interface;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using ViscaCamLink.Repositories.AppSettings;

public static partial class WindowPlacementHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWPLACEMENT
    {
        public uint length;
        public uint flags;
        public uint showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT lpwndpl);

    private const uint SW_SHOWNORMAL = 1;
    private const uint SW_SHOWMINIMIZED = 2;
    private const uint SW_SHOWMAXIMIZED = 3;

    public static WindowPlacementData Save(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var wp = new WINDOWPLACEMENT { length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>() };

        GetWindowPlacement(hwnd, ref wp);

        if (wp.showCmd == SW_SHOWMINIMIZED)
        {
            wp.showCmd = SW_SHOWNORMAL;
        }

        return new WindowPlacementData
        {
            ShowCmd = wp.showCmd,
            NormalLeft = wp.rcNormalPosition.Left,
            NormalTop = wp.rcNormalPosition.Top,
            NormalRight = wp.rcNormalPosition.Right,
            NormalBottom = wp.rcNormalPosition.Bottom,
        };
    }

    /// <summary>
    /// Must be called from the <see cref="Window.SourceInitialized"/> event.
    /// </summary>
    public static WindowState Restore(Window window, WindowPlacementData data)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var wp = new WINDOWPLACEMENT
        {
            length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>(),
            flags = 0,
            showCmd = SW_SHOWNORMAL,
            ptMinPosition = new POINT { X = -1, Y = -1 },
            ptMaxPosition = new POINT { X = -1, Y = -1 },
            rcNormalPosition = new RECT
            {
                Left = data.NormalLeft,
                Top = data.NormalTop,
                Right = data.NormalRight,
                Bottom = data.NormalBottom,
            }
        };

        SetWindowPlacement(hwnd, ref wp);

        return data.ShowCmd == SW_SHOWMAXIMIZED
            ? WindowState.Maximized
            : WindowState.Normal;
    }
}
