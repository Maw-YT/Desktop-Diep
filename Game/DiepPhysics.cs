namespace DesktopDiep;

internal static class DiepPhysics
{
    public static void AddVelocity(ref double vx, ref double vy, double angle, double magnitude)
    {
        vx += Math.Cos(angle) * magnitude;
        vy += Math.Sin(angle) * magnitude;
    }

    public static void MaintainVelocity(ref double vx, ref double vy, double angle, double maxSpeed) =>
        AddVelocity(ref vx, ref vy, angle, maxSpeed * 0.1);

    public static void ApplyPhysics(ref double x, ref double y, ref double vx, ref double vy, bool deleting, double radius, double width, double height)
    {
        Step(ref x, ref y, ref vx, ref vy, deleting);
        Math2.ClampPos(ref x, ref y, radius, width, height);
    }

    public static void ApplyDronePhysics(ref double x, ref double y, ref double vx, ref double vy, bool deleting, double radius, double width, double height, bool bounce)
    {
        Step(ref x, ref y, ref vx, ref vy, deleting);
        if (bounce)
            Math2.Bounce(ref x, ref y, ref vx, ref vy, radius, width, height);
    }

    private static void Step(ref double x, ref double y, ref double vx, ref double vy, bool deleting)
    {
        var mag = Math.Sqrt(vx * vx + vy * vy);
        if (mag < 0.01)
        {
            vx = 0;
            vy = 0;
        }
        else if (deleting)
        {
            vx *= 0.5;
            vy *= 0.5;
            mag *= 0.5;
        }

        x += vx;
        y += vy;

        if (mag > 0)
            AddVelocity(ref vx, ref vy, Math.Atan2(vy, vx), mag * -0.1);
    }
}
