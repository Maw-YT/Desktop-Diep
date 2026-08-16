namespace DesktopDiep;

internal static class Collision
{
    public static bool Circles(double x1, double y1, double r1, double x2, double y2, double r2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        var r = r1 + r2;
        return dx * dx + dy * dy <= r * r;
    }

    public static void Knockback(
        ref double x1, ref double y1, ref double vx1, ref double vy1, double absorb1, double push1,
        ref double x2, ref double y2, ref double vx2, ref double vy2, double absorb2, double push2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        double angle;
        if (dx == 0 && dy == 0)
            angle = Random.Shared.NextDouble() * Math.PI * 2;
        else
            angle = Math.Atan2(dy, dx);

        var c = Math.Cos(angle);
        var s = Math.Sin(angle);
        var mag1 = absorb1 * push2;
        var mag2 = absorb2 * push1;
        vx1 += c * mag1;
        vy1 += s * mag1;
        vx2 -= c * mag2;
        vy2 -= s * mag2;
    }
}
