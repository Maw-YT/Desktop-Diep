namespace DesktopDiep;

internal static class TankClasses
{
    public static void Set(TankEntity tank, TankId id)
    {
        tank.ClassId = id;
        RebuildBarrels(tank);
        TankAddons.Build(tank);
    }

    public static void RebuildBarrels(TankEntity tank)
    {
        var barrels = tank.Class.Barrels;
        var reload = TankStats.ReloadTicks(tank);
        tank.Barrels = new BarrelState[barrels.Length];
        for (var i = 0; i < barrels.Length; i++)
        {
            tank.Barrels[i] = new BarrelState();
            tank.Barrels[i].Bind(barrels[i], reload);
        }
    }

    /// <summary>
    /// Instantly rolls the full future upgrade path (ignoring level gates),
    /// so branches like Smasher can be chosen instead of being locked out at 15.
    /// </summary>
    public static void PlanUpgrades(TankEntity tank, Random rng)
    {
        tank.ClassPlan.Clear();
        var current = tank.ClassId;
        while (true)
        {
            if (!TankCatalog.TryGet(current, out var def) || def.Upgrades.Length == 0)
                break;
            var next = def.Upgrades[rng.Next(def.Upgrades.Length)];
            tank.ClassPlan.Add(next);
            current = next;
        }
    }

    public static bool TryUpgrade(TankEntity tank, Random rng)
    {
        _ = rng;
        var changed = false;
        while (tank.ClassPlan.Count > 0)
        {
            var next = tank.ClassPlan[0];
            if (!TankCatalog.TryGet(next, out var def))
            {
                tank.ClassPlan.RemoveAt(0);
                continue;
            }
            if (tank.Level < def.LevelRequirement)
                break;
            tank.ClassPlan.RemoveAt(0);
            Set(tank, next);
            changed = true;
        }
        return changed;
    }
}
