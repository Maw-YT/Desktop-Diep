namespace DesktopDiep;

internal static class TankStats
{
    public const int Regen = 0;
    public const int MaxHealth = 1;
    public const int Body = 2;
    public const int BulletSpeed = 3;
    public const int Pen = 4;
    public const int BulletDamage = 5;
    public const int Reload = 6;
    public const int Move = 7;

    public static readonly string[] Names =
    [
        "Health Regen",
        "Max Health",
        "Body Damage",
        "Bullet Speed",
        "Bullet Penetration",
        "Bullet Damage",
        "Reload",
        "Movement Speed"
    ];

    public static void Recalc(TankEntity tank)
    {
        tank.MaxHealth = 48 + tank.Stats[MaxHealth] * 20 + (tank.Level - 1) * 2;
        tank.Radius = 20 + Math.Min(8, tank.Level * 0.12);
        tank.Mass = 9 + tank.Level * 0.08;
        if (tank.Health > tank.MaxHealth)
            tank.Health = tank.MaxHealth;

        var reload = ReloadTicks(tank);
        foreach (var barrel in tank.Barrels)
            ScaleReload(barrel, reload);
        foreach (var turret in tank.Turrets)
            ScaleReload(turret.Barrel, reload);
    }

    private static void ScaleReload(BarrelState barrel, double tankReload)
    {
        var next = Math.Max(1, tankReload * barrel.Def.Reload);
        if (barrel.ReloadTime > 0)
            barrel.Pos *= next / barrel.ReloadTime;
        barrel.ReloadTime = next;
    }

    public static bool IsRam(TankEntity tank) =>
        tank.Class.PostAddon is "smasher" or "landmine" or "spike" or "autosmasher";

    public static double BodyDamage(TankEntity tank)
    {
        var dmg = 8 + tank.Stats[Body] * 4.5;
        if (IsRam(tank))
            dmg *= tank.Class.PostAddon == "spike" ? 1.5 : 1.25;
        return dmg;
    }

    public static double MoveSpeed(TankEntity tank) =>
        (210 / 25.0) * tank.Class.Speed * (1 + tank.Stats[Move] * 0.155) * (1 - Math.Min(0.18, tank.Level * 0.003));

    public static double ReloadTicks(TankEntity tank) =>
        15 * Math.Pow(0.914, tank.Stats[Reload]);

    public static int XpNeeded(int level) =>
        (int)Math.Round(10 + level * 12 + Math.Pow(level, 1.55) * 2);

    public static bool SetLevel(TankEntity tank, int stat, int value)
    {
        if (stat is < 0 or > 7) return false;
        value = Math.Clamp(value, 0, 7);
        if (tank.Stats[stat] == value) return false;
        tank.ManualStats = true;
        var ratio = tank.Health / Math.Max(1, tank.MaxHealth);
        tank.Stats[stat] = value;
        Recalc(tank);
        tank.Health = Math.Clamp(ratio * tank.MaxHealth, 1, tank.MaxHealth);
        return true;
    }

    public static bool TryUpgrade(TankEntity tank, int stat)
    {
        if (stat is < 0 or > 7) return false;
        if (tank.SkillPoints <= 0) return false;
        if (tank.Stats[stat] >= 7) return false;
        tank.Stats[stat]++;
        tank.SkillPoints--;
        var ratio = tank.Health / tank.MaxHealth;
        Recalc(tank);
        tank.Health = Math.Clamp(ratio * tank.MaxHealth, 1, tank.MaxHealth);
        return true;
    }
}
