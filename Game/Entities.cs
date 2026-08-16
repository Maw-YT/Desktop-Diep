using System.Windows.Media;

namespace DesktopDiep;

public enum ShapeKind
{
    Square,
    Triangle,
    Pentagon,
    AlphaPentagon,
    Crasher
}

public sealed class ShapeEntity
{
    public double PrevX, PrevY, PrevAngle;
    public double X, Y, Vx, Vy, Angle, Spin;
    public double Radius, Mass, Health, MaxHealth;
    public double PrevHealth, PrevMaxHealth;
    public double PushFactor = 8;
    public double Absorption = 1;
    public double OrbitCx, OrbitCy, OrbitRadius, OrbitAngle, OrbitSpeed;
    public double ShapeVelocity = 1;
    public double RamDamage;
    public int Xp;
    public ShapeKind Kind;
    public Color Fill;
    public DestroyAnim Destroy { get; } = new();
    public DamageFlash Flash { get; } = new();

    public double DrawX(double t) => Interp.Lerp(PrevX, X, t);
    public double DrawY(double t) => Interp.Lerp(PrevY, Y, t);
    public double DrawAngle(double t) => Interp.LerpAngle(PrevAngle, Angle, t);
    public double DrawHealthRatio(double t)
    {
        var a = PrevMaxHealth <= 0.001 ? 1 : PrevHealth / PrevMaxHealth;
        var b = MaxHealth <= 0.001 ? 1 : Health / MaxHealth;
        return Interp.Lerp(a, b, t);
    }

    public void Capture()
    {
        PrevX = X;
        PrevY = Y;
        PrevAngle = Angle;
        PrevHealth = Health;
        PrevMaxHealth = MaxHealth;
        Destroy.Capture();
        Flash.Capture();
    }

    public void Snap()
    {
        PrevX = X;
        PrevY = Y;
        PrevAngle = Angle;
        PrevHealth = Health;
        PrevMaxHealth = MaxHealth;
    }

    public void Hurt(double amount)
    {
        if (amount <= 0)
            return;
        Health -= amount;
        Flash.Hit();
    }
}

public sealed class BulletEntity
{
    public double PrevX, PrevY, PrevAngle;
    public double X, Y, Vx, Vy, Angle;
    public double Radius, Mass, Damage, Health, Life;
    public double PushFactor = 8;
    public double Absorption = 1;
    public double Accel;
    public double MovementAngle;
    public double Opacity = 1;
    public double PrevOpacity = 1;
    public double Spin;
    public int BarrelIndex;
    public int OwnerId;
    public int Sides = 1;
    public int Age;
    public bool IsStar;
    public bool CanControl;
    public bool RestCycle = true;
    public bool NoDestroyAnim;
    public ProjectileKind Kind;
    public Color Fill;
    public BarrelState[] Guns = [];
    public DestroyAnim Destroy { get; } = new();

    public double DrawX(double t) => Interp.Lerp(PrevX, X, t);
    public double DrawY(double t) => Interp.Lerp(PrevY, Y, t);
    public double DrawAngle(double t) => Interp.LerpAngle(PrevAngle, Angle, t);
    public double DrawOpacity(double t) => Interp.Lerp(PrevOpacity, Opacity, t);

    public void Capture()
    {
        PrevX = X;
        PrevY = Y;
        PrevAngle = Angle;
        PrevOpacity = Opacity;
        Destroy.Capture();
        foreach (var g in Guns)
            g.Capture();
    }

    public void Snap()
    {
        PrevX = X;
        PrevY = Y;
        PrevAngle = Angle;
        PrevOpacity = Opacity;
    }
}

public sealed class TankEntity
{
    public double PrevX, PrevY, PrevAngle;
    public double X, Y, Vx, Vy, Angle;
    public double Radius = 22;
    public double Mass = 8;
    public double PushFactor = 8;
    public double Absorption = 1;
    public double Health = 50;
    public double MaxHealth = 50;
    public double PrevHealth = 50;
    public double PrevMaxHealth = 50;
    public int PrevXpIntoLevel;
    public int PrevXpForNext = 10;
    public double CombatTimer;
    public int Id;
    public Color Fill = DiepColors.Tank;
    public bool ManualStats;
    public PetBrain Brain { get; } = new();
    public TankId ClassId = TankId.Basic;
    /// <summary>Remaining class upgrades chosen at spawn; applied when level allows.</summary>
    public readonly List<TankId> ClassPlan = [];
    public BarrelState[] Barrels = [];
    public GuardAddon[] Guards = [];
    public AutoTurretState[] Turrets = [];
    public double RotatorAngle;
    public double PrevRotatorAngle;
    public int Level = 1;
    public int Score;
    public int SkillPoints;
    public int XpIntoLevel;
    public int XpForNext = 10;
    public readonly int[] Stats = new int[8];
    public bool Alive = true;
    public bool IsArenaCloser;
    public bool IsBoss;
    public string? BossAltName;
    public int BossXp = 3000;
    public double Respawn;
    public DestroyAnim Destroy { get; } = new();
    public DamageFlash Flash { get; } = new();
    public TankDef Class => TankCatalog.Get(ClassId);

    public double DrawX(double t) => Interp.Lerp(PrevX, X, t);
    public double DrawY(double t) => Interp.Lerp(PrevY, Y, t);
    public double DrawAngle(double t) => Interp.LerpAngle(PrevAngle, Angle, t);
    public double DrawRotator(double t) => Interp.LerpAngle(PrevRotatorAngle, RotatorAngle, t);
    public double DrawHealthRatio(double t)
    {
        var a = PrevMaxHealth <= 0.001 ? 1 : PrevHealth / PrevMaxHealth;
        var b = MaxHealth <= 0.001 ? 1 : Health / MaxHealth;
        return Interp.Lerp(a, b, t);
    }
    public double DrawXpRatio(double t)
    {
        var a = PrevXpForNext <= 0 ? 0 : PrevXpIntoLevel / (double)PrevXpForNext;
        var b = XpForNext <= 0 ? 0 : XpIntoLevel / (double)XpForNext;
        return Interp.Lerp(a, b, t);
    }

    public void Capture()
    {
        PrevX = X;
        PrevY = Y;
        PrevAngle = Angle;
        PrevRotatorAngle = RotatorAngle;
        PrevHealth = Health;
        PrevMaxHealth = MaxHealth;
        PrevXpIntoLevel = XpIntoLevel;
        PrevXpForNext = XpForNext;
        Destroy.Capture();
        Flash.Capture();
        foreach (var b in Barrels)
            b.Capture();
        foreach (var g in Guards)
            g.Capture();
        foreach (var turret in Turrets)
            turret.Capture();
    }

    public void Snap()
    {
        PrevX = X;
        PrevY = Y;
        PrevAngle = Angle;
        PrevRotatorAngle = RotatorAngle;
        PrevHealth = Health;
        PrevMaxHealth = MaxHealth;
        PrevXpIntoLevel = XpIntoLevel;
        PrevXpForNext = XpForNext;
        foreach (var g in Guards)
            g.PrevAngle = g.Angle;
        foreach (var turret in Turrets)
            turret.PrevAngle = turret.Angle;
    }
}
