namespace DesktopDiep;

public sealed class GuardAddon
{
    public int Sides;
    public double SizeRatio;
    public double Angle;
    public double PrevAngle;
    public double Spin;

    public void Capture() => PrevAngle = Angle;

    public void Tick() => Angle += Spin;

    public double DrawAngle(double t) => Interp.LerpAngle(PrevAngle, Angle, t);
}

public sealed class AutoTurretState
{
    public double MountAngle;
    public double Orbit = 0.8;
    public double Angle;
    public double PrevAngle;
    public BarrelState Barrel = new();

    public void Capture()
    {
        PrevAngle = Angle;
        Barrel.Capture();
    }

    public double DrawAngle(double t) => Interp.LerpAngle(PrevAngle, Angle, t);
}

internal static class TankAddons
{
    public static readonly BarrelDef AutoTurretBarrel = Mini(0.3);
    public static readonly BarrelDef AutoTurretMiniBarrel = Mini(0.4);

    public static void Build(TankEntity tank)
    {
        var guards = new List<GuardAddon>();
        var turrets = new List<AutoTurretState>();
        Apply(tank.Class.PreAddon, guards, turrets);
        Apply(tank.Class.PostAddon, guards, turrets);
        tank.Guards = [.. guards];
        tank.Turrets = [.. turrets];
    }

    private static void Apply(string? id, List<GuardAddon> guards, List<AutoTurretState> turrets)
    {
        switch (id)
        {
            case "smasher":
                guards.Add(Guard(6, 1.15, 0, 0.1));
                break;
            case "landmine":
                guards.Add(Guard(6, 1.15, 0, 0.1));
                guards.Add(Guard(6, 1.15, 0, 0.05));
                break;
            case "spike":
                guards.Add(Guard(3, 1.3, 0, 0.17));
                guards.Add(Guard(3, 1.3, Math.PI / 3, 0.17));
                guards.Add(Guard(3, 1.3, Math.PI / 6, 0.17));
                guards.Add(Guard(3, 1.3, Math.PI / 2, 0.17));
                break;
            case "weirdspike":
                guards.Add(Guard(3, 1.5, 0, 0.17));
                guards.Add(Guard(3, 1.5, 0, -0.16));
                break;
            case "spiesk":
                guards.Add(Guard(4, 1.3, 0, 0.17));
                guards.Add(Guard(4, 1.3, Math.PI / 6, 0.17));
                guards.Add(Guard(4, 1.3, Math.PI / 3, 0.17));
                break;
            case "dombase":
                guards.Add(Guard(6, 1.24, 0, 0));
                break;
            case "autosmasher":
                guards.Add(Guard(6, 1.15, 0, 0.1));
                turrets.Add(Center());
                break;
            case "autoturret":
                turrets.Add(Center());
                break;
            case "auto2":
                Orbit(turrets, 2);
                break;
            case "auto3":
                Orbit(turrets, 3);
                break;
            case "auto5":
                Orbit(turrets, 5);
                break;
            case "auto7":
                Orbit(turrets, 7);
                break;
        }
    }

    private static GuardAddon Guard(int sides, double size, double angle, double spin) => new()
    {
        Sides = sides,
        SizeRatio = size,
        Angle = angle,
        PrevAngle = angle,
        Spin = spin
    };

    private static AutoTurretState Center()
    {
        var t = new AutoTurretState { Orbit = 0 };
        t.Barrel.Bind(AutoTurretBarrel, 15);
        return t;
    }

    private static void Orbit(List<AutoTurretState> turrets, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var t = new AutoTurretState
            {
                MountAngle = Math.PI * 2 * i / count,
                Orbit = 0.8
            };
            t.Angle = t.MountAngle;
            t.PrevAngle = t.MountAngle;
            t.Barrel.Bind(AutoTurretMiniBarrel, 15);
            turrets.Add(t);
        }
    }

    private static BarrelDef Mini(double damage) => new()
    {
        Angle = 0,
        Offset = 0,
        Size = 55,
        Width = 42 * 0.7,
        Delay = 0.01,
        Reload = 1,
        Recoil = 0.3,
        Bullet = new()
        {
            Type = ProjectileKind.Bullet,
            SizeRatio = 1,
            Health = 1,
            Damage = damage,
            Speed = 1.2,
            ScatterRate = 1,
            LifeLength = 1,
            Absorption = 1
        }
    };
}
