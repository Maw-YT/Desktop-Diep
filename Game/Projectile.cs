namespace DesktopDiep;

internal static class Projectile
{
    public static bool IsDroneLike(ProjectileKind kind) =>
        kind is ProjectileKind.Drone or ProjectileKind.Swarm or ProjectileKind.Necrodrone or ProjectileKind.Minion;

    public static bool AlwaysFire(ProjectileKind kind) =>
        kind is ProjectileKind.Drone or ProjectileKind.Swarm or ProjectileKind.Minion;

    public static bool StaysInArena(ProjectileKind kind) =>
        kind is ProjectileKind.Drone or ProjectileKind.Necrodrone or ProjectileKind.Minion or ProjectileKind.Trap;

    public static readonly BarrelDef RocketGun = Gun(Math.PI, 70, 36, 0.15, 3.3, true, 0,
        health: 0.3, damage: 0.6, speed: 1.5, scatter: 5, life: 0.1);

    public static readonly BarrelDef SkimmerGunA = Gun(0, 70, 42, 0.35, 0, false, 0,
        health: 0.3, damage: 0.6, speed: 1.1, scatter: 1, life: 0.25);

    public static readonly BarrelDef SkimmerGunB = Gun(Math.PI, 70, 42, 0.35, 0, false, 0,
        health: 0.3, damage: 0.6, speed: 1.1, scatter: 1, life: 0.25);

    public static readonly BarrelDef CrocGunA = Gun(Math.PI / 2, 70, 42, 0.5, 0, false, 0,
        health: 0.3, damage: 0.6, speed: 0.2, scatter: 1, life: 0.25);

    public static readonly BarrelDef CrocGunB = Gun(Math.PI * 1.5, 70, 42, 0.5, 0, false, 0,
        health: 0.3, damage: 0.6, speed: 0.2, scatter: 1, life: 0.25);

    public static readonly BarrelDef MinionGun = Gun(0, 85, 50.4, 1, 1, false, 0,
        health: 0.4, damage: 0.4, speed: 0.8, scatter: 1, life: 1);

    public static void Configure(BulletEntity shot, TankEntity tank, BarrelDef barrel, double scale)
    {
        var type = barrel.Bullet.Type;
        shot.CanControl = barrel.CanControlDrones;
        switch (type)
        {
            case ProjectileKind.Trap:
                shot.Sides = 3;
                shot.IsStar = true;
                shot.Accel = 0;
                shot.Mass = 3;
                shot.Life = Math.Max(8, (600 * barrel.Bullet.LifeLength) / 8);
                shot.Angle = Random.Shared.NextDouble() * Math.PI * 2;
                shot.Spin = (Random.Shared.NextDouble() - 0.5) * 0.12;
                shot.PushFactor = 4;
                break;
            case ProjectileKind.Drone:
            case ProjectileKind.Swarm:
                shot.Sides = 3;
                shot.Life = barrel.Bullet.LifeLength < 0 ? 1e9 : 88 * barrel.Bullet.LifeLength;
                shot.Vx /= 3;
                shot.Vy /= 3;
                shot.PushFactor = 4;
                shot.CanControl = barrel.CanControlDrones;
                break;
            case ProjectileKind.Necrodrone:
                shot.Sides = 4;
                shot.Life = 1e9;
                shot.Vx = 0;
                shot.Vy = 0;
                shot.PushFactor = 4;
                shot.Fill = DiepColors.NecroSquare;
                shot.CanControl = true;
                break;
            case ProjectileKind.Minion:
                shot.Sides = 1;
                shot.Radius *= 1.2;
                shot.Life = 1e9;
                shot.CanControl = barrel.CanControlDrones;
                shot.Guns = Bind(MinionGun, tank);
                break;
            case ProjectileKind.Rocket:
                shot.Guns = Bind(RocketGun, tank);
                break;
            case ProjectileKind.Skimmer:
                shot.Spin = 0.1 * (Random.Shared.Next(2) == 0 ? 1 : -1);
                shot.Guns = Bind(SkimmerGunA, tank, SkimmerGunB);
                break;
            case ProjectileKind.Croc:
                shot.Guns = Bind(CrocGunA, tank, CrocGunB);
                break;
            case ProjectileKind.Flame:
                shot.Sides = 4;
                shot.Accel = 0;
                shot.Vx *= 2;
                shot.Vy *= 2;
                shot.Life = 25 * Math.Max(0.05, barrel.Bullet.LifeLength);
                shot.PushFactor = 0;
                shot.Absorption = 0;
                shot.NoDestroyAnim = true;
                break;
            case ProjectileKind.Wall:
                shot.Life = 0;
                break;
        }
    }

    public static void Tick(BulletEntity b, TankEntity? owner, IReadOnlyList<ShapeEntity> shapes, IReadOnlyList<TankEntity> tanks)
    {
        b.Age++;
        if (b.Spin != 0)
            b.Angle += b.Spin;
        if (b.Kind == ProjectileKind.Flame)
            b.Opacity = Math.Max(0, b.Opacity - 1.0 / 25);

        if (IsDroneLike(b.Kind))
            TickDrone(b, owner, shapes, tanks);
        else if (b.Kind is ProjectileKind.Bullet or ProjectileKind.Rocket or ProjectileKind.Skimmer or ProjectileKind.Croc)
        {
            if (b.Age > 1 && b.Accel > 0)
                DiepPhysics.MaintainVelocity(ref b.Vx, ref b.Vy, b.MovementAngle, b.Accel);
        }
    }

    private static void TickDrone(BulletEntity b, TankEntity? owner, IReadOnlyList<ShapeEntity> shapes, IReadOnlyList<TankEntity> tanks)
    {
        var accel = b.Accel > 0 ? b.Accel : 8;
        var controlling = b.CanControl && owner is { Alive: true, Brain.HasTarget: true };
        if (controlling)
        {
            SteerToward(b, owner!.Brain.AimX, owner.Brain.AimY, accel);
            b.RestCycle = false;
            return;
        }

        var hunt = NearestHunt(b, owner, shapes, tanks, b.Kind == ProjectileKind.Swarm ? 800 : 400);
        if (hunt is { } t)
        {
            b.RestCycle = false;
            SteerToward(b, t.x, t.y, accel);
            return;
        }

        if (owner is { Alive: true })
            TickRest(b, owner, accel);
    }

    private static void SteerToward(BulletEntity b, double aimX, double aimY, double accel)
    {
        var angle = Math.Atan2(aimY - b.Y, aimX - b.X);
        if (b.Kind == ProjectileKind.Minion)
        {
            b.Angle = angle;
            var dist2 = Dist2(b.X, b.Y, aimX, aimY);
            var focus = 240.0 * 240.0;
            if (dist2 < focus / 7)
                angle += Math.PI;
            else if (dist2 < focus)
                angle += Math.PI / 2;
            b.MovementAngle = angle;
            DiepPhysics.MaintainVelocity(ref b.Vx, ref b.Vy, angle, accel);
            return;
        }

        b.Angle = b.MovementAngle = angle;
        DiepPhysics.MaintainVelocity(ref b.Vx, ref b.Vy, angle, accel);
    }

    private static void TickRest(BulletEntity b, TankEntity owner, double accel)
    {
        var dx = b.X - owner.X;
        var dy = b.Y - owner.Y;
        var dist2 = dx * dx + dy * dy;
        var rest = 160.0 * 160.0;
        if (dist2 <= rest && b.RestCycle)
        {
            DiepPhysics.MaintainVelocity(ref b.Vx, ref b.Vy, b.Angle, accel / 6);
            b.Angle += 0.01 + 0.012 * (dist2 / rest);
            b.MovementAngle = b.Angle;
            return;
        }

        var offset = Math.Atan2(dy, dx) + Math.PI / 2;
        var tx = owner.X + Math.Cos(offset) * owner.Radius * 1.2 - b.X;
        var ty = owner.Y + Math.Sin(offset) * owner.Radius * 1.2 - b.Y;
        b.Angle = b.MovementAngle = Math.Atan2(ty, tx);
        DiepPhysics.MaintainVelocity(ref b.Vx, ref b.Vy, b.MovementAngle, dist2 < rest * 0.5 ? accel / 3 : accel);
        b.RestCycle = tx * tx + ty * ty <= 4 * owner.Radius * owner.Radius;
        if (b.Kind == ProjectileKind.Minion)
            b.Angle = Math.Atan2(b.Vy, b.Vx);
    }

    private static (double x, double y)? NearestHunt(BulletEntity b, TankEntity? owner, IReadOnlyList<ShapeEntity> shapes, IReadOnlyList<TankEntity> tanks, double range)
    {
        var ox = owner?.X ?? b.X;
        var oy = owner?.Y ?? b.Y;
        var nearOwner = range * range;
        var best = double.MaxValue;
        double? hx = null, hy = null;
        foreach (var tank in tanks)
        {
            if (!tank.Alive || tank.Destroy.Active || (owner is not null && tank.Id == owner.Id))
                continue;
            if (Dist2(ox, oy, tank.X, tank.Y) > nearOwner)
                continue;
            var d = Dist2(b.X, b.Y, tank.X, tank.Y);
            if (d >= best) continue;
            best = d;
            hx = tank.X;
            hy = tank.Y;
        }
        foreach (var s in shapes)
        {
            if (s.Destroy.Active) continue;
            if (Dist2(ox, oy, s.X, s.Y) > nearOwner)
                continue;
            var d = Dist2(b.X, b.Y, s.X, s.Y);
            if (d >= best) continue;
            best = d;
            hx = s.X;
            hy = s.Y;
        }
        return hx is null ? null : (hx.Value, hy!.Value);
    }

    private static double Dist2(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    private static BarrelState[] Bind(BarrelDef a, TankEntity tank, BarrelDef? b = null)
    {
        var reload = TankStats.ReloadTicks(tank);
        var first = new BarrelState();
        first.Bind(a, reload);
        if (b is null)
            return [first];
        var second = new BarrelState();
        second.Bind(b, reload);
        return [first, second];
    }

    private static BarrelDef Gun(double angle, double size, double width, double reload, double recoil, bool trap, double trapDir,
        double health, double damage, double speed, double scatter, double life) => new()
    {
        Angle = angle,
        Size = size,
        Width = width,
        Reload = reload,
        Recoil = recoil,
        IsTrapezoid = trap,
        TrapezoidDirection = trapDir,
        Bullet = new()
        {
            Type = ProjectileKind.Bullet,
            SizeRatio = 1,
            Health = health,
            Damage = damage,
            Speed = speed,
            ScatterRate = scatter,
            LifeLength = life,
            Absorption = 1
        }
    };
}
