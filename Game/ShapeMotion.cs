namespace DesktopDiep;

internal static class ShapeMotion
{
    public const double BaseOrbit = 0.005;
    public const double BaseVelocity = 1;

    public static void TickIdle(ShapeEntity s)
    {
        s.Angle += s.Spin;
        s.OrbitAngle += s.OrbitSpeed;
        DiepPhysics.MaintainVelocity(ref s.Vx, ref s.Vy, s.OrbitAngle, s.ShapeVelocity);
    }

    public static void TickCrasher(ShapeEntity s, IReadOnlyList<TankEntity> tanks)
    {
        TankEntity? target = null;
        var best = 640.0 * 640.0;
        foreach (var tank in tanks)
        {
            if (!tank.Alive || tank.Destroy.Active || tank.IsArenaCloser)
                continue;
            var dx = tank.X - s.X;
            var dy = tank.Y - s.Y;
            var d = dx * dx + dy * dy;
            if (d >= best)
                continue;
            best = d;
            target = tank;
        }

        if (target is null)
        {
            TickIdle(s);
            return;
        }

        var angle = Math.Atan2(target.Y - s.Y, target.X - s.X);
        s.Angle = angle;
        DiepPhysics.MaintainVelocity(ref s.Vx, ref s.Vy, angle, s.ShapeVelocity);
    }

    public static void NudgeOrbit(ShapeEntity s, double pull)
    {
        s.OrbitAngle = Math.Atan2(s.Vy, s.Vx);
        _ = pull;
    }
}
