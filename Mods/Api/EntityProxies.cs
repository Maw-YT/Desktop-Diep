using System.Windows.Media;
using MoonSharp.Interpreter;

namespace DesktopDiep;

[MoonSharpUserData]
internal sealed class TankProxy
{
    private readonly TankEntity _t;
    private readonly GameWorld _w;
    private readonly Script _script;

    public TankProxy(TankEntity t, GameWorld w, Script script)
    {
        _t = t;
        _w = w;
        _script = script;
    }

    public TankEntity Raw => _t;

    public int id => _t.Id;
    public string kind => "tank";
    public double x { get => _t.X; set => _t.X = value; }
    public double y { get => _t.Y; set => _t.Y = value; }
    public double vx { get => _t.Vx; set => _t.Vx = value; }
    public double vy { get => _t.Vy; set => _t.Vy = value; }
    public double angle { get => _t.Angle; set => _t.Angle = value; }
    public double radius { get => _t.Radius; set => _t.Radius = value; }
    public double mass { get => _t.Mass; set => _t.Mass = value; }
    public double health { get => _t.Health; set => _t.Health = value; }
    public double max_health { get => _t.MaxHealth; set => _t.MaxHealth = value; }
    public int level
    {
        get => _t.Level;
        set => _w.ModSetLevel(_t, value);
    }
    public int score { get => _t.Score; set => _t.Score = value; }
    public int skill_points { get => _t.SkillPoints; set => _t.SkillPoints = value; }
    public int xp_into { get => _t.XpIntoLevel; set => _t.XpIntoLevel = Math.Max(0, value); }
    public int xp_for_next => _t.XpForNext;
    public string class_id => _t.ClassId.ToString();
    public string class_name => _t.Class.Name;
    public bool alive { get => _t.Alive; set => _t.Alive = value; }
    public bool ai { get => _t.AiEnabled; set => _t.AiEnabled = value; }
    public bool is_boss => _t.IsBoss;
    public bool is_closer => _t.IsArenaCloser;
    public bool is_ram => TankStats.IsRam(_t);
    public bool is_selected => _w.Selected == _t;
    public bool wants_shot { get => _t.Brain.WantsShot; set => _t.Brain.WantsShot = value; }
    public double aim_x { get => _t.Brain.AimX; set => _t.Brain.AimX = value; }
    public double aim_y { get => _t.Brain.AimY; set => _t.Brain.AimY = value; }
    public bool fleeing => _t.Brain.Fleeing;
    public bool has_target => _t.Brain.HasTarget;
    public double combat_timer { get => _t.CombatTimer; set => _t.CombatTimer = value; }
    public double respawn { get => _t.Respawn; set => _t.Respawn = value; }
    public double body_damage => TankStats.BodyDamage(_t);
    public double move_speed => TankStats.MoveSpeed(_t);
    public int barrel_count => _t.Barrels.Length;

    public double fill_r
    {
        get => _t.Fill.R / 255.0;
        set => _t.Fill = Color.FromRgb(ToByte(value), _t.Fill.G, _t.Fill.B);
    }
    public double fill_g
    {
        get => _t.Fill.G / 255.0;
        set => _t.Fill = Color.FromRgb(_t.Fill.R, ToByte(value), _t.Fill.B);
    }
    public double fill_b
    {
        get => _t.Fill.B / 255.0;
        set => _t.Fill = Color.FromRgb(_t.Fill.R, _t.Fill.G, ToByte(value));
    }

    public void set_fill(double r, double g, double b) =>
        _t.Fill = Color.FromRgb(ToByte(r), ToByte(g), ToByte(b));

    public int get_stat(int index) =>
        index >= 0 && index < 8 ? _t.Stats[index] : 0;

    public void set_stat(int index, int value)
    {
        if (index < 0 || index >= 8) return;
        TankStats.SetLevel(_t, index, value);
    }

    public bool upgrade_stat(int index) => TankStats.TryUpgrade(_t, index);

    public DynValue stats()
    {
        var t = new Table(_script);
        for (var i = 0; i < 8; i++)
        {
            t[i + 1] = _t.Stats[i];
            t[StatKey(i)] = _t.Stats[i];
        }
        return DynValue.NewTable(t);
    }

    public DynValue barrels()
    {
        var list = new Table(_script);
        for (var i = 0; i < _t.Barrels.Length; i++)
            list[i + 1] = UserData.Create(new BarrelProxy(_t.Barrels[i], i));
        return DynValue.NewTable(list);
    }

    public DynValue enemy() => EntityProxies.Tank(_script, _t.Brain.Enemy, _w);
    public DynValue shape_target() => EntityProxies.Shape(_script, _t.Brain.ShapeTarget, _w);

    public void heal(double amount = -1)
    {
        if (amount < 0)
            _t.Health = _t.MaxHealth;
        else
            _t.Health = Math.Min(_t.MaxHealth, _t.Health + amount);
    }

    public void hurt(double amount, TankProxy? killer = null) =>
        _w.HurtTank(_t, amount, killer?.Raw);

    public void kill(TankProxy? killer = null) =>
        _w.HurtTank(_t, _t.Health + 1, killer?.Raw);

    public void set_class(string id)
    {
        if (!TankCatalog.TryParseId(id, out var tankId))
            return;
        TankClasses.Set(_t, tankId);
        TankStats.Recalc(_t);
    }

    public void add_xp(int xp) => _w.ModAddScore(_t, xp);

    public void teleport(double x, double y)
    {
        _t.X = x;
        _t.Y = y;
        _t.Snap();
    }

    public void push(double dx, double dy)
    {
        _t.Vx += dx;
        _t.Vy += dy;
    }

    public void impulse(double angle, double mag) =>
        DiepPhysics.AddVelocity(ref _t.Vx, ref _t.Vy, angle, mag);

    public void look_at(double tx, double ty) =>
        _t.Angle = Math.Atan2(ty - _t.Y, tx - _t.X);

    public void aim_at(double tx, double ty)
    {
        _t.Brain.AimX = tx;
        _t.Brain.AimY = ty;
        _t.Angle = Math.Atan2(ty - _t.Y, tx - _t.X);
    }

    public void shoot() => _w.ModForceShoot(_t);

    /// <summary>Apply diep-style acceleration toward (ax, ay). Use with <c>ai = false</c>.</summary>
    public void steer(double ax, double ay) =>
        DiepPhysics.MaintainVelocity(ref _t.Vx, ref _t.Vy, Math.Atan2(ay, ax),
            (ax == 0 && ay == 0) ? 0 : TankStats.MoveSpeed(_t));

    public void select() => _w.SelectById(_t.Id);

    public bool remove() => _w.TryRemoveTank(_t);

    private static string StatKey(int i) => i switch
    {
        TankStats.Regen => "regen",
        TankStats.MaxHealth => "max_health",
        TankStats.Body => "body",
        TankStats.BulletSpeed => "bullet_speed",
        TankStats.Pen => "pen",
        TankStats.BulletDamage => "bullet_damage",
        TankStats.Reload => "reload",
        TankStats.Move => "move",
        _ => i.ToString()
    };

    private static byte ToByte(double t) =>
        (byte)Math.Clamp(t <= 1.0 && t >= 0 ? t * 255.0 : t, 0, 255);
}

[MoonSharpUserData]
internal sealed class BarrelProxy
{
    private readonly BarrelState _b;

    public BarrelProxy(BarrelState b, int index)
    {
        _b = b;
        this.index = index;
    }

    public int index { get; }
    public double angle => _b.Def.Angle;
    public double offset => _b.Def.Offset;
    public double size => _b.Def.Size;
    public double width => _b.Def.Width;
    public double reload => _b.ReloadTime;
    public double pos { get => _b.Pos; set => _b.Pos = value; }
    public bool ready => _b.Pos >= _b.ReloadTime;
    public string projectile => _b.Def.Bullet.Type.ToString();
}

[MoonSharpUserData]
internal sealed class ShapeProxy
{
    private readonly ShapeEntity _s;
    private readonly GameWorld _w;

    public ShapeProxy(ShapeEntity s, GameWorld w)
    {
        _s = s;
        _w = w;
    }

    public ShapeEntity Raw => _s;

    public string kind => "shape";
    public string shape { get => _s.Kind.ToString(); set => SetKind(value); }
    public double x { get => _s.X; set => _s.X = value; }
    public double y { get => _s.Y; set => _s.Y = value; }
    public double vx { get => _s.Vx; set => _s.Vx = value; }
    public double vy { get => _s.Vy; set => _s.Vy = value; }
    public double angle { get => _s.Angle; set => _s.Angle = value; }
    public double radius { get => _s.Radius; set => _s.Radius = value; }
    public double mass { get => _s.Mass; set => _s.Mass = value; }
    public double health { get => _s.Health; set => _s.Health = value; }
    public double max_health { get => _s.MaxHealth; set => _s.MaxHealth = value; }
    public double ram_damage { get => _s.RamDamage; set => _s.RamDamage = value; }
    public double spin { get => _s.Spin; set => _s.Spin = value; }
    public int xp { get => _s.Xp; set => _s.Xp = value; }
    public bool destroying => _s.Destroy.Active;

    public double fill_r
    {
        get => _s.Fill.R / 255.0;
        set => _s.Fill = Color.FromRgb(ToByte(value), _s.Fill.G, _s.Fill.B);
    }
    public double fill_g
    {
        get => _s.Fill.G / 255.0;
        set => _s.Fill = Color.FromRgb(_s.Fill.R, ToByte(value), _s.Fill.B);
    }
    public double fill_b
    {
        get => _s.Fill.B / 255.0;
        set => _s.Fill = Color.FromRgb(_s.Fill.R, _s.Fill.G, ToByte(value));
    }

    public void set_fill(double r, double g, double b) =>
        _s.Fill = Color.FromRgb(ToByte(r), ToByte(g), ToByte(b));

    public void hurt(double amount) => _s.Hurt(amount);

    public void kill(TankProxy? killer = null) =>
        _w.KillShape(_s, killer?.Raw);

    public void teleport(double x, double y)
    {
        _s.X = x;
        _s.Y = y;
        _s.OrbitCx = x;
        _s.OrbitCy = y;
        _s.Snap();
    }

    public void push(double dx, double dy)
    {
        _s.Vx += dx;
        _s.Vy += dy;
    }

    private void SetKind(string name)
    {
        if (Enum.TryParse<ShapeKind>(name, true, out var k))
            _s.Kind = k;
    }

    private static byte ToByte(double t) =>
        (byte)Math.Clamp(t <= 1.0 && t >= 0 ? t * 255.0 : t, 0, 255);
}

[MoonSharpUserData]
internal sealed class BulletProxy
{
    private readonly BulletEntity _b;
    private readonly GameWorld _w;
    private readonly Script _script;

    public BulletProxy(BulletEntity b, GameWorld w, Script script)
    {
        _b = b;
        _w = w;
        _script = script;
    }

    public BulletEntity Raw => _b;

    public string kind => "bullet";
    public string projectile { get => _b.Kind.ToString(); set => SetKind(value); }
    public double x { get => _b.X; set => _b.X = value; }
    public double y { get => _b.Y; set => _b.Y = value; }
    public double vx { get => _b.Vx; set => _b.Vx = value; }
    public double vy { get => _b.Vy; set => _b.Vy = value; }
    public double angle { get => _b.Angle; set => _b.Angle = value; }
    public double radius { get => _b.Radius; set => _b.Radius = value; }
    public double mass { get => _b.Mass; set => _b.Mass = value; }
    public double damage { get => _b.Damage; set => _b.Damage = value; }
    public double health { get => _b.Health; set => _b.Health = value; }
    public double life { get => _b.Life; set => _b.Life = value; }
    public double accel { get => _b.Accel; set => _b.Accel = value; }
    public double spin { get => _b.Spin; set => _b.Spin = value; }
    public double opacity { get => _b.Opacity; set => _b.Opacity = value; }
    public int sides { get => _b.Sides; set => _b.Sides = value; }
    public int age => _b.Age;
    public bool visible { get => _b.Visible; set => _b.Visible = value; }
    public bool can_control { get => _b.CanControl; set => _b.CanControl = value; }
    public int owner_id { get => _b.OwnerId; set => _b.OwnerId = value; }

    public DynValue owner() => EntityProxies.Tank(_script, _w.FindTank(_b.OwnerId), _w);

    public void set_owner(TankProxy? tank) => _b.OwnerId = tank?.id ?? 0;

    public void set_fill(double r, double g, double b) =>
        _b.Fill = Color.FromRgb(ToByte(r), ToByte(g), ToByte(b));

    public void teleport(double x, double y)
    {
        _b.X = x;
        _b.Y = y;
        _b.Snap();
    }

    public void push(double dx, double dy)
    {
        _b.Vx += dx;
        _b.Vy += dy;
    }

    public void destroy()
    {
        _b.Health = 0;
        _b.Life = 0;
    }

    private void SetKind(string name)
    {
        if (Enum.TryParse<ProjectileKind>(name, true, out var k))
            _b.Kind = k;
    }

    private static byte ToByte(double t) =>
        (byte)Math.Clamp(t <= 1.0 && t >= 0 ? t * 255.0 : t, 0, 255);
}

internal static class EntityProxies
{
    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered) return;
        UserData.RegisterType<TankProxy>();
        UserData.RegisterType<ShapeProxy>();
        UserData.RegisterType<BulletProxy>();
        UserData.RegisterType<BarrelProxy>();
        _registered = true;
    }

    public static DynValue Tank(Script script, TankEntity? t, GameWorld w) =>
        t is null ? DynValue.Nil : UserData.Create(new TankProxy(t, w, script));

    public static DynValue Shape(Script script, ShapeEntity? s, GameWorld w) =>
        s is null ? DynValue.Nil : UserData.Create(new ShapeProxy(s, w));

    public static DynValue Bullet(Script script, BulletEntity? b, GameWorld w) =>
        b is null ? DynValue.Nil : UserData.Create(new BulletProxy(b, w, script));
}
