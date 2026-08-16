namespace DesktopDiep;

public sealed class PetBrain
{
    private static readonly int[] RamUpgradeOrder =
    [
        TankStats.Body,
        TankStats.MaxHealth,
        TankStats.Move,
        TankStats.Regen,
        TankStats.Reload,
        TankStats.BulletDamage,
        TankStats.BulletSpeed,
        TankStats.Pen
    ];

    private static readonly int[] UpgradeOrder =
    [
        TankStats.Reload,
        TankStats.BulletDamage,
        TankStats.BulletSpeed,
        TankStats.Pen,
        TankStats.Move,
        TankStats.Regen,
        TankStats.MaxHealth,
        TankStats.Body
    ];

    public ShapeEntity? ShapeTarget { get; private set; }
    public TankEntity? Enemy { get; private set; }
    public bool Fleeing { get; private set; }
    public bool WantsShot { get; private set; }
    public double AimX { get; private set; }
    public double AimY { get; private set; }
    public bool HasTarget => Enemy is not null || ShapeTarget is not null;

    public void Think(TankEntity tank, IReadOnlyList<TankEntity> tanks, IReadOnlyList<ShapeEntity> shapes, out double ax, out double ay)
    {
        ax = 0;
        ay = 0;
        WantsShot = false;
        Fleeing = !TankStats.IsRam(tank) && tank.Health < tank.MaxHealth * 0.28;
        Enemy = PickEnemy(tank, tanks);
        ShapeTarget = Enemy is null ? PickShape(tank, shapes) : null;

        double tx, ty, tr;
        if (Enemy is { } foe)
        {
            tx = foe.X - tank.X;
            ty = foe.Y - tank.Y;
            tr = foe.Radius;
            AimX = foe.X;
            AimY = foe.Y;
        }
        else if (ShapeTarget is { } shape)
        {
            tx = shape.X - tank.X;
            ty = shape.Y - tank.Y;
            tr = shape.Radius;
            AimX = shape.X;
            AimY = shape.Y;
        }
        else
        {
            tank.Angle += 0.02;
            return;
        }

        var dist = Math.Sqrt(tx * tx + ty * ty);
        var angle = Math.Atan2(ty, tx);
        tank.Angle = angle;

        if (TankStats.IsRam(tank) && !Fleeing)
        {
            ax = Math.Cos(angle);
            ay = Math.Sin(angle);
            WantsShot = true;
            return;
        }

        var preferred = (Enemy is null ? 95 : 140) + tank.Radius + tr;
        if (Fleeing)
        {
            ax = -Math.Cos(angle);
            ay = -Math.Sin(angle);
        }
        else if (dist > preferred + 30)
        {
            ax = Math.Cos(angle);
            ay = Math.Sin(angle);
        }
        else if (dist < preferred - 20)
        {
            ax = -Math.Cos(angle);
            ay = -Math.Sin(angle);
        }
        else
        {
            ax = -Math.Sin(angle) * 0.35;
            ay = Math.Cos(angle) * 0.35;
        }

        WantsShot = dist <= (Enemy is null ? 280 : 360);
    }

    public void SpendPoints(TankEntity tank)
    {
        if (tank.ManualStats)
            return;
        var guard = 0;
        while (tank.SkillPoints > 0 && guard++ < 16)
        {
            var spent = false;
            foreach (var stat in TankStats.IsRam(tank) ? RamUpgradeOrder : UpgradeOrder)
            {
                if (TankStats.TryUpgrade(tank, stat))
                {
                    spent = true;
                    break;
                }
            }
            if (!spent)
                break;
        }
    }

    private static TankEntity? PickEnemy(TankEntity tank, IReadOnlyList<TankEntity> tanks)
    {
        TankEntity? best = null;
        var bestD = double.MaxValue;
        foreach (var other in tanks)
        {
            if (other.Id == tank.Id || !other.Alive || other.Destroy.Active)
                continue;
            var dx = other.X - tank.X;
            var dy = other.Y - tank.Y;
            var d = dx * dx + dy * dy;
            if (d >= bestD)
                continue;
            bestD = d;
            best = other;
        }
        return best;
    }

    private static ShapeEntity? PickShape(TankEntity tank, IReadOnlyList<ShapeEntity> shapes)
    {
        ShapeEntity? best = null;
        var bestWeight = double.MaxValue;
        foreach (var s in shapes)
        {
            if (s.Destroy.Active)
                continue;
            var dx = s.X - tank.X;
            var dy = s.Y - tank.Y;
            var threat = s.Kind == ShapeKind.Crasher ? 0.2 : 1;
            var weight = (dx * dx + dy * dy) / (s.Xp + 8.0) * threat;
            if (weight >= bestWeight)
                continue;
            bestWeight = weight;
            best = s;
        }
        return best;
    }
}
