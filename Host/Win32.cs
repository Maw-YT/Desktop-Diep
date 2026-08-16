using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace DesktopDiep;

internal static class Win32
{
    public const int GwlExStyle = -20;
    public const int WsExTransparent = 0x00000020;
    public const int WsExToolWindow = 0x00000080;
    public const int WsExNoActivate = 0x08000000;
    public const int WsExLayered = 0x00080000;
    public const int WmHotkey = 0x0312;
    public const int ModControl = 0x0002;
    public const int ModShift = 0x0004;
    public const int HotkeyDebug = 1;
    public const int HotkeyPause = 2;
    public const int HotkeyReset = 3;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern nint GetWindowLongPtr64(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern nint SetWindowLongPtr64(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(nint hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(nint hWnd, int id);

    public static nint GetWindowLong(nint hWnd, int nIndex) =>
        nint.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);

    public static nint SetWindowLong(nint hWnd, int nIndex, nint dwNewLong) =>
        nint.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, (int)dwNewLong);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out PointI lpPoint);

    public const int VkLButton = 0x01;

    [StructLayout(LayoutKind.Sequential)]
    public struct PointI
    {
        public int X;
        public int Y;
    }

    public static void SetClickThrough(HwndSource source, bool clickThrough)
    {
        var hwnd = source.Handle;
        var style = GetWindowLong(hwnd, GwlExStyle);
        style |= WsExToolWindow | WsExLayered | WsExNoActivate;
        if (clickThrough)
            style |= WsExTransparent;
        else
            style &= ~WsExTransparent;
        SetWindowLong(hwnd, GwlExStyle, style);
    }

    public static void ApplyPetWindowStyle(HwndSource source) => SetClickThrough(source, true);
}
