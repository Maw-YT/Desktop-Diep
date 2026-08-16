using System.Windows.Input;
using System.Windows.Interop;

namespace DesktopDiep;

internal sealed class HotkeyService : IDisposable
{
    private readonly HwndSource _source;
    public event Action? ToggleDebug;
    public event Action? TogglePause;
    public event Action? Reset;

    public HotkeyService(HwndSource source)
    {
        _source = source;
        _source.AddHook(WndProc);
        var mods = Win32.ModControl | Win32.ModShift;
        Win32.RegisterHotKey(_source.Handle, Win32.HotkeyDebug, mods, KeyInterop.VirtualKeyFromKey(Key.D));
        Win32.RegisterHotKey(_source.Handle, Win32.HotkeyPause, mods, KeyInterop.VirtualKeyFromKey(Key.P));
        Win32.RegisterHotKey(_source.Handle, Win32.HotkeyReset, mods, KeyInterop.VirtualKeyFromKey(Key.R));
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != Win32.WmHotkey)
            return 0;
        switch ((int)wParam)
        {
            case Win32.HotkeyDebug:
                ToggleDebug?.Invoke();
                handled = true;
                break;
            case Win32.HotkeyPause:
                TogglePause?.Invoke();
                handled = true;
                break;
            case Win32.HotkeyReset:
                Reset?.Invoke();
                handled = true;
                break;
        }
        return 0;
    }

    public void Dispose()
    {
        Win32.UnregisterHotKey(_source.Handle, Win32.HotkeyDebug);
        Win32.UnregisterHotKey(_source.Handle, Win32.HotkeyPause);
        Win32.UnregisterHotKey(_source.Handle, Win32.HotkeyReset);
        _source.RemoveHook(WndProc);
    }
}
