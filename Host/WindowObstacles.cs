using System.Text;
using System.Windows;

namespace DesktopDiep;

public readonly struct WindowBox
{
    public readonly double Left, Top, Right, Bottom;

    public WindowBox(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public bool Overlaps(double left, double top, double right, double bottom) =>
        Left < right && Right > left && Top < bottom && Bottom > top;

    public bool HitsSegment(double x0, double y0, double x1, double y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        var t0 = 0.0;
        var t1 = 1.0;
        return Clip(-dx, x0 - Left, ref t0, ref t1)
            && Clip(dx, Right - x0, ref t0, ref t1)
            && Clip(-dy, y0 - Top, ref t0, ref t1)
            && Clip(dy, Bottom - y0, ref t0, ref t1)
            && t0 <= t1;
    }

    private static bool Clip(double p, double q, ref double t0, ref double t1)
    {
        if (Math.Abs(p) < 1e-12)
            return q >= 0;
        var r = q / p;
        if (p < 0)
        {
            if (r > t1)
                return false;
            if (r > t0)
                t0 = r;
        }
        else
        {
            if (r < t0)
                return false;
            if (r < t1)
                t1 = r;
        }
        return true;
    }
}

public sealed class WindowObstacles
{
    private readonly List<WindowBox> _boxes = [];
    private readonly Win32.EnumWindowsProc _enum;
    private readonly StringBuilder _className = new(64);
    private double _dpiX = 1, _dpiY = 1;
    private nint _overlay;
    private double _originX, _originY, _screenW, _screenH;

    public void SetDpi(double dpiX, double dpiY)
    {
        _dpiX = dpiX > 0.5 ? dpiX : 1;
        _dpiY = dpiY > 0.5 ? dpiY : 1;
    }

    public IReadOnlyList<WindowBox> Boxes => _boxes;

    public bool CanSee(double x0, double y0, double x1, double y1, double thick = 0)
    {
        if (Blocked(x0, y0, x1, y1))
            return false;
        if (thick <= 0.5)
            return true;
        var dx = x1 - x0;
        var dy = y1 - y0;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 4)
            return true;
        var nx = -dy / len * thick;
        var ny = dx / len * thick;
        return !Blocked(x0 + nx, y0 + ny, x1 + nx, y1 + ny)
            && !Blocked(x0 - nx, y0 - ny, x1 - nx, y1 - ny);
    }

    public bool Blocked(double x0, double y0, double x1, double y1)
    {
        foreach (var box in _boxes)
        {
            if (box.HitsSegment(x0, y0, x1, y1))
                return true;
        }
        return false;
    }

    public WindowObstacles() => _enum = OnEnum;

    public void Refresh(nint overlayHwnd)
    {
        _boxes.Clear();
        _overlay = overlayHwnd;
        _originX = Win32.GetSystemMetrics(Win32.SmXVirtualScreen);
        _originY = Win32.GetSystemMetrics(Win32.SmYVirtualScreen);
        _screenW = Win32.GetSystemMetrics(78); // SM_CXVIRTUALSCREEN
        _screenH = Win32.GetSystemMetrics(79);
        if (overlayHwnd == 0)
            return;
        Win32.EnumWindows(_enum, 0);
    }

    private bool OnEnum(nint hwnd, nint _)
    {
        if (hwnd == _overlay || !Win32.IsWindowVisible(hwnd) || Win32.IsIconic(hwnd) || Win32.IsZoomed(hwnd))
            return true;
        if (IsCloaked(hwnd))
            return true;
        var style = (int)Win32.GetWindowLong(hwnd, Win32.GwlExStyle);
        if ((style & Win32.WsExTransparent) != 0 && (style & Win32.WsExLayered) != 0)
            return true;

        _className.Clear();
        if (Win32.GetClassName(hwnd, _className, _className.Capacity) <= 0)
            return true;
        var cls = _className.ToString();
        if (cls is "Progman" or "WorkerW" or "ForegroundStaging" or "Shell_SecondaryTrayWnd")
            return true;

        if (!Win32.GetWindowRect(hwnd, out var r))
            return true;
        var w = r.Right - r.Left;
        var h = r.Bottom - r.Top;
        if (w < 80 * _dpiX || h < 80 * _dpiY)
            return true;
        if (w >= _screenW - 8 && h >= _screenH - 8)
            return true;

        _boxes.Add(new WindowBox(
            (r.Left - _originX) / _dpiX,
            (r.Top - _originY) / _dpiY,
            (r.Right - _originX) / _dpiX,
            (r.Bottom - _originY) / _dpiY));
        return true;
    }

    private static bool IsCloaked(nint hwnd)
    {
        if (Win32.DwmGetWindowAttribute(hwnd, Win32.DwmwaCloaked, out var cloaked, sizeof(int)) != 0)
            return false;
        return cloaked != 0;
    }
}
