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

    public static bool TryUpgrade(TankEntity tank, Random rng)
    {
        var changed = false;
        while (true)
        {
            var options = new List<TankId>();
            foreach (var id in tank.Class.Upgrades)
            {
                if (!TankCatalog.TryGet(id, out var next))
                    continue;
                if (tank.Level < next.LevelRequirement)
                    continue;
                options.Add(id);
            }
            if (options.Count == 0)
                break;
            Set(tank, options[rng.Next(options.Count)]);
            changed = true;
        }
        return changed;
    }
}
