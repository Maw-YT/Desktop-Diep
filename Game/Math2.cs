namespace DesktopDiep;

internal static class Math2
{
    public static bool Overlaps(double x1, double y1, double r1, double x2, double y2, double r2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        var rr = r1 + r2;
        return dx * dx + dy * dy < rr * rr;
    }

    public static double NormalizeAngle(double a)
    {
        while (a > Math.PI) a -= Math.PI * 2;
        while (a < -Math.PI) a += Math.PI * 2;
        return a;
    }

    public static void ClampPos(ref double x, ref double y, double radius, double width, double height)
    {
        x = Math.Clamp(x, radius, Math.Max(radius, width - radius));
        y = Math.Clamp(y, radius, Math.Max(radius, height - radius));
    }

    public static void Bounce(ref double x, ref double y, ref double vx, ref double vy, double radius, double width, double height)
    {
        if (x < radius) { x = radius; vx = Math.Abs(vx); }
        if (y < radius) { y = radius; vy = Math.Abs(vy); }
        if (x > width - radius) { x = width - radius; vx = -Math.Abs(vx); }
        if (y > height - radius) { y = height - radius; vy = -Math.Abs(vy); }
    }

    public static bool SweepCircle(double x0, double y0, double x1, double y1, double cx, double cy, double radius, out double tHit)
    {
        tHit = 1;
        var dx = x1 - x0;
        var dy = y1 - y0;
        var fx = x0 - cx;
        var fy = y0 - cy;
        var a = dx * dx + dy * dy;
        var b = 2 * (fx * dx + fy * dy);
        var c = fx * fx + fy * fy - radius * radius;
        if (c <= 0)
        {
            tHit = 0;
            return true;
        }
        if (a < 1e-12)
            return false;
        var disc = b * b - 4 * a * c;
        if (disc < 0)
            return false;
        var t = (-b - Math.Sqrt(disc)) / (2 * a);
        if (t is < 0 or > 1)
            return false;
        tHit = t;
        return true;
    }
}
