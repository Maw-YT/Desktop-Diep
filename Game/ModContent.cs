using System.Windows.Media;
using MoonSharp.Interpreter;

namespace DesktopDiep;

internal sealed class CustomProjectileDef
{
    public string Id = "";
    public int Sides = 1;
    public bool Star;
    public double Size = 1;
    public double Health = 1;
    public double Damage = 1;
    public double Speed = 1;
    public double Life = 1;
    public double Spin;
    public bool Scripted;
    public bool HasColor;
    public Color Fill;
    public Closure? Tick;
    public Closure? Hit;
}

internal sealed class CustomShapeDef
{
    public string Id = "";
    public int Sides = 6;
    public double Radius = 36;
    public double Health = 80;
    public int Xp = 40;
    public double Mass = 4;
    public double Spin = 0.01;
    public double Ram;
    public Color Fill = DiepColors.Pentagon;
    public Closure? Tick;
}

internal sealed class CustomAddonDef
{
    public string Id = "";
    public List<GuardAddon> Guards = [];
    public int Turrets;
    public Closure? Tick;
}

internal readonly struct ShadeResult
{
    public Color Color { get; init; }
    public double Glow { get; init; }
    public double Opacity { get; init; }
}

internal static class ModContent
{
    private static readonly Dictionary<string, Closure> Shaders = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CustomProjectileDef> Projectiles = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CustomShapeDef> Shapes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CustomAddonDef> Addons = new(StringComparer.OrdinalIgnoreCase);

    public static void Clear()
    {
        Shaders.Clear();
        Projectiles.Clear();
        Shapes.Clear();
        Addons.Clear();
    }

    public static IEnumerable<string> ShaderIds => Shaders.Keys;
    public static IEnumerable<string> ProjectileIds => Projectiles.Keys;
    public static IEnumerable<string> ShapeIds => Shapes.Keys;
    public static IEnumerable<string> AddonIds => Addons.Keys;

    public static bool RegisterShader(Table table)
    {
        var id = IdOf(table);
        var apply = Fn(table, "apply") ?? Fn(table, "fragment");
        if (id.Length == 0 || apply is null)
            return false;
        Shaders[id] = apply;
        return true;
    }

    public static bool RegisterProjectile(Table table)
    {
        var id = IdOf(table);
        if (id.Length == 0 || Enum.TryParse<ProjectileKind>(id, true, out _))
            return false;
        Projectiles[id] = new CustomProjectileDef
        {
            Id = id,
            Sides = Math.Max(1, (int)Num(table, "sides", 1)),
            Star = Bool(table, "star"),
            Size = Num(table, "size", 1),
            Health = Num(table, "health", 1),
            Damage = Num(table, "damage", 1),
            Speed = Num(table, "speed", 1),
            Life = Num(table, "life", 1),
            Spin = Num(table, "spin", 0),
            Scripted = Bool(table, "scripted"),
            HasColor = TryColor(table, out var fill),
            Fill = fill,
            Tick = Fn(table, "tick"),
            Hit = Fn(table, "hit")
        };
        return true;
    }

    public static bool RegisterShape(Table table)
    {
        var id = IdOf(table);
        if (id.Length == 0 || Enum.TryParse<ShapeKind>(id, true, out _))
            return false;
        TryColor(table, out var fill);
        Shapes[id] = new CustomShapeDef
        {
            Id = id,
            Sides = Math.Max(3, (int)Num(table, "sides", 6)),
            Radius = Num(table, "radius", 36),
            Health = Num(table, "health", 80),
            Xp = (int)Num(table, "xp", 40),
            Mass = Num(table, "mass", 4),
            Spin = Num(table, "spin", 0.01),
            Ram = Num(table, "ram", 0),
            Fill = fill,
            Tick = Fn(table, "tick")
        };
        return true;
    }

    public static bool RegisterAddon(Table table)
    {
        var id = IdOf(table);
        if (id.Length == 0)
            return false;
        var guards = new List<GuardAddon>();
        var g = table.Get("guards");
        if (g.Type == DataType.Table)
        {
            foreach (var pair in g.Table.Values)
            {
                if (pair.Type != DataType.Table)
                    continue;
                var row = pair.Table;
                guards.Add(new GuardAddon
                {
                    Sides = Math.Max(3, (int)Num(row, "sides", 6)),
                    SizeRatio = Num(row, "size", 1.15),
                    Angle = Num(row, "angle", 0),
                    Spin = Num(row, "spin", 0.1)
                });
            }
        }
        Addons[id] = new CustomAddonDef
        {
            Id = id,
            Guards = guards,
            Turrets = Math.Max(0, (int)Num(table, "turrets", 0)),
            Tick = Fn(table, "tick")
        };
        return true;
    }

    public static CustomProjectileDef? Projectile(string? id) =>
        id is not null && Projectiles.TryGetValue(id, out var def) ? def : null;

    public static CustomShapeDef? Shape(string? id) =>
        id is not null && Shapes.TryGetValue(id, out var def) ? def : null;

    public static CustomAddonDef? Addon(string? id) =>
        id is not null && Addons.TryGetValue(id, out var def) ? def : null;

    public static void ConfigureShot(BulletEntity shot, string? id)
    {
        var def = Projectile(id);
        if (def is null)
            return;
        shot.Kind = ProjectileKind.Custom;
        shot.CustomId = def.Id;
        shot.Sides = def.Sides;
        shot.IsStar = def.Star;
        shot.Spin = def.Spin;
        shot.Radius = Math.Max(DiepView.Px(3), shot.Radius * def.Size);
        shot.Health *= def.Health;
        shot.Damage *= def.Damage;
        shot.Life *= def.Life;
        shot.Vx *= def.Speed;
        shot.Vy *= def.Speed;
        shot.Accel *= def.Speed;
        if (def.Scripted)
            shot.Accel = 0;
        if (def.HasColor)
            shot.Fill = def.Fill;
    }

    public static void TickBullet(GameWorld world, BulletEntity bullet, double dt)
    {
        var tick = Projectile(bullet.CustomId)?.Tick;
        if (tick is null)
            return;
        Call(tick, EntityProxies.Bullet(tick.OwnerScript, bullet, world), DynValue.NewNumber(dt));
    }

    public static bool AllowHit(GameWorld world, BulletEntity bullet, object other)
    {
        var hit = Projectile(bullet.CustomId)?.Hit;
        if (hit is null)
            return true;
        var script = hit.OwnerScript;
        var otherValue = other switch
        {
            TankEntity tank => EntityProxies.Tank(script, tank, world),
            ShapeEntity shape => EntityProxies.Shape(script, shape, world),
            BulletEntity shot => EntityProxies.Bullet(script, shot, world),
            ArenaObject obj => EntityProxies.Obj(script, obj, world),
            _ => DynValue.Nil
        };
        var result = Call(hit, EntityProxies.Bullet(script, bullet, world), otherValue);
        return result.Type != DataType.Boolean || result.Boolean;
    }

    public static void TickShape(GameWorld world, ShapeEntity shape, double dt)
    {
        var tick = Shape(shape.CustomId)?.Tick;
        if (tick is null)
            return;
        Call(tick, EntityProxies.Shape(tick.OwnerScript, shape, world), DynValue.NewNumber(dt));
    }

    public static void TickAddons(GameWorld world, TankEntity tank, double dt)
    {
        TickAddon(world, tank, dt, tank.Class.PreAddon);
        TickAddon(world, tank, dt, tank.Class.PostAddon);
    }

    public static void TickObjects(GameWorld world, double dt)
    {
        foreach (var obj in world.Objects)
        {
            if (!obj.Alive || obj.TickFn is null)
                continue;
            if (!TryCall(obj.TickFn, EntityProxies.Obj(obj.TickFn.OwnerScript, obj, world), DynValue.NewNumber(dt)))
                obj.TickFn = null;
        }
    }

    public static ShadeResult Shade(string? id, string kind, double x, double y, double time, double radius, int team, Color color)
    {
        if (string.IsNullOrEmpty(id) || !Shaders.TryGetValue(id, out var fn))
            return new ShadeResult { Color = color, Glow = 0, Opacity = 1 };
        var script = fn.OwnerScript;
        var ctx = new Table(script)
        {
            ["kind"] = kind,
            ["x"] = x,
            ["y"] = y,
            ["time"] = time,
            ["radius"] = radius,
            ["team"] = team,
            ["r"] = color.R / 255.0,
            ["g"] = color.G / 255.0,
            ["b"] = color.B / 255.0,
            ["a"] = color.A / 255.0
        };
        var result = Call(fn, DynValue.NewTable(ctx));
        if (result.Type != DataType.Table)
            return new ShadeResult { Color = color, Glow = 0, Opacity = 1 };
        var t = result.Table;
        var r = Num(t, "r", color.R / 255.0);
        var g = Num(t, "g", color.G / 255.0);
        var b = Num(t, "b", color.B / 255.0);
        var a = Num(t, "a", 1);
        return new ShadeResult
        {
            Color = Color.FromRgb(ToByte(r), ToByte(g), ToByte(b)),
            Glow = Math.Max(0, Num(t, "glow", 0)),
            Opacity = Math.Clamp(a, 0, 1)
        };
    }

    public static ArenaObject? ReadObject(Table table, Closure? tick)
    {
        var obj = new ArenaObject
        {
            Kind = Str(table, "kind", "object"),
            CustomId = Str(table, "id", ""),
            Label = Str(table, "label", ""),
            Team = (int)Num(table, "team", 0),
            X = Num(table, "x", 0),
            Y = Num(table, "y", 0),
            W = Num(table, "w", 40),
            H = Num(table, "h", 40),
            Radius = Num(table, "radius", 0),
            Vx = Num(table, "vx", 0),
            Vy = Num(table, "vy", 0),
            Angle = Num(table, "angle", 0),
            Spin = Num(table, "spin", 0),
            Solid = Bool(table, "solid"),
            DamagePerTick = Num(table, "damage", 0),
            HealPerTick = Num(table, "heal", 0),
            Health = Num(table, "health", 0),
            MaxHealth = Num(table, "max_health", Num(table, "health", 0)),
            Opacity = Num(table, "opacity", 1),
            Sides = (int)Num(table, "sides", 0),
            Shader = NullIfEmpty(Str(table, "shader", "")),
            TickFn = tick ?? Fn(table, "tick")
        };
        if (obj.CustomId?.Length == 0)
            obj.CustomId = null;
        if (TryColor(table, out var fill))
            obj.Fill = fill;
        else if (obj.Team != 0)
            obj.Fill = Teams.Color(obj.Team);
        obj.Snap();
        return obj;
    }

    private static void TickAddon(GameWorld world, TankEntity tank, double dt, string? id)
    {
        var tick = Addon(id)?.Tick;
        if (tick is null)
            return;
        Call(tick, EntityProxies.Tank(tick.OwnerScript, tank, world), DynValue.NewNumber(dt));
    }

    private static DynValue Call(Closure fn, params object[] args)
    {
        TryCall(fn, out var result, args);
        return result;
    }

    private static bool TryCall(Closure fn, params object[] args) => TryCall(fn, out _, args);

    private static bool TryCall(Closure fn, out DynValue result, params object[] args)
    {
        try
        {
            result = fn.OwnerScript.Call(fn, args);
            return true;
        }
        catch (ScriptRuntimeException ex)
        {
            ModHost.Current?.Log(ex.DecoratedMessage);
            result = DynValue.Nil;
            return false;
        }
        catch (Exception ex)
        {
            ModHost.Current?.Log(ex.Message);
            result = DynValue.Nil;
            return false;
        }
    }

    private static string IdOf(Table table)
    {
        var id = Str(table, "id", "");
        return string.IsNullOrWhiteSpace(id) ? "" : id.Trim();
    }

    private static Closure? Fn(Table table, string key)
    {
        var v = table.Get(key);
        return v.Type == DataType.Function ? v.Function : null;
    }

    private static bool Bool(Table table, string key) =>
        table.Get(key).Type == DataType.Boolean && table.Get(key).Boolean;

    private static double Num(Table table, string key, double fallback)
    {
        var v = table.Get(key);
        return v.Type == DataType.Number ? v.Number : fallback;
    }

    private static string Str(Table table, string key, string fallback)
    {
        var v = table.Get(key);
        return v.Type == DataType.String ? v.String : fallback;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static bool TryColor(Table table, out Color color)
    {
        color = DiepColors.Tank;
        var fill = table.Get("color");
        if (fill.Type != DataType.Table)
            fill = table.Get("fill");
        if (fill.Type == DataType.Table)
        {
            var t = fill.Table;
            color = Color.FromRgb(ToByte(Num(t, "r", 1)), ToByte(Num(t, "g", 1)), ToByte(Num(t, "b", 1)));
            return true;
        }
        if (table.Get("r").Type == DataType.Number)
        {
            color = Color.FromRgb(
                ToByte(Num(table, "r", 1)),
                ToByte(Num(table, "g", 1)),
                ToByte(Num(table, "b", 1)));
            return true;
        }
        return false;
    }

    private static byte ToByte(double t) =>
        (byte)Math.Clamp(t <= 1.0 && t >= 0 ? t * 255.0 : t, 0, 255);
}
