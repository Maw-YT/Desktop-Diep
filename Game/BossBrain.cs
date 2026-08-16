namespace DesktopDiep;

/// <summary>Diepcustom-style boss patrol + aim (see AbstractBoss / Guardian / etc.).</summary>
internal static class BossBrain
{
    private const double CornerReach = 300 * 300;

    public static void Think(TankEntity tank, GameWorld world, out double ax, out double ay)
    {
        ax = 0;
        ay = 0;
        tank.Brain.ClearBossAim();

        var foe = NearestPlayer(tank, world);
        if (foe is not null)
            tank.Brain.SetBossAim(foe.X, foe.Y, foe);

        var spin = tank.ClassId is TankId.Summoner or TankId.Defender or TankId.FallenOverlord;
        var faceMove = tank.ClassId is TankId.Guardian or TankId.FallenBooster;

        if (faceMove && foe is not null && tank.ClassId == TankId.FallenBooster)
        {
            tank.Angle = Math.Atan2(foe.Y - tank.Y, foe.X - tank.X);
            tank.Brain.WantsShot = true;
            Patrol(tank, world, out ax, out ay);
            return;
        }

        Patrol(tank, world, out ax, out ay);

        if (spin)
            tank.Angle += tank.ClassId == TankId.Defender ? 0.04 : 0.02;
        else if (faceMove && (ax != 0 || ay != 0))
            tank.Angle = Math.Atan2(ay, ax);

        tank.Brain.WantsShot = foe is not null || tank.ClassId is TankId.Guardian or TankId.Summoner
            or TankId.FallenOverlord or TankId.Defender;
    }

    private static void Patrol(TankEntity tank, GameWorld world, out double ax, out double ay)
    {
        ax = 0;
        ay = 0;
        if (world.Width < 80 || world.Height < 80)
            return;

        var corners = new (double X, double Y)[]
        {
            (world.Width * 0.75, world.Height * 0.75),
            (world.Width * 0.25, world.Height * 0.75),
            (world.Width * 0.25, world.Height * 0.25),
            (world.Width * 0.75, world.Height * 0.25),
        };

        var i = tank.Brain.BossCorner;
        if (i < 0 || i > 3)
        {
            i = Quadrant(tank.X, tank.Y, world.Width, world.Height);
            tank.Brain.BossCorner = i;
        }

        var (tx, ty) = corners[i];
        var dx = tx - tank.X;
        var dy = ty - tank.Y;
        if (dx * dx + dy * dy < CornerReach)
        {
            i = (i + 1) % 4;
            tank.Brain.BossCorner = i;
            (tx, ty) = corners[i];
            dx = tx - tank.X;
            dy = ty - tank.Y;
        }

        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1)
            return;
        ax = dx / len;
        ay = dy / len;
    }

    private static int Quadrant(double x, double y, double w, double h)
    {
        var right = x >= w * 0.5;
        var bottom = y >= h * 0.5;
        if (right && bottom) return 0;
        if (!right && bottom) return 1;
        if (!right && !bottom) return 2;
        return 3;
    }

    private static TankEntity? NearestPlayer(TankEntity boss, GameWorld world)
    {
        TankEntity? best = null;
        var bestD = double.MaxValue;
        foreach (var t in world.Tanks)
        {
            if (t.Id == boss.Id || !t.Alive || t.Destroy.Active || t.IsArenaCloser || t.IsBoss)
                continue;
            var dx = t.X - boss.X;
            var dy = t.Y - boss.Y;
            var d = dx * dx + dy * dy;
            if (d >= bestD)
                continue;
            bestD = d;
            best = t;
        }
        return best;
    }
}
