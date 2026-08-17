namespace DesktopDiep;

public enum TankId
{
    Basic = 0,
    Twin = 1,
    Triplet = 2,
    TripleShot = 3,
    QuadTank = 4,
    OctoTank = 5,
    Sniper = 6,
    MachineGun = 7,
    FlankGuard = 8,
    TriAngle = 9,
    Destroyer = 10,
    Overseer = 11,
    Overlord = 12,
    TwinFlank = 13,
    PentaShot = 14,
    Assassin = 15,
    Necromancer = 17,
    TripleTwin = 18,
    Hunter = 19,
    Gunner = 20,
    Stalker = 21,
    Ranger = 22,
    Booster = 23,
    Fighter = 24,
    Hybrid = 25,
    Manager = 26,
    Predator = 28,
    Sprayer = 29,
    Trapper = 31,
    GunnerTrapper = 32,
    Overtrapper = 33,
    MegaTrapper = 34,
    TriTrapper = 35,
    Smasher = 36,
    Landmine = 38,
    AutoGunner = 39,
    Auto5 = 40,
    Auto3 = 41,
    SpreadShot = 42,
    Streamliner = 43,
    AutoTrapper = 44,
    Battleship = 48,
    Annihilator = 49,
    AutoSmasher = 50,
    Spike = 51,
    Factory = 52,
    Skimmer = 54,
    Rocketeer = 55,
    Guardian = 100,
    Summoner = 101,
    Defender = 102,
    FallenBooster = 103,
    FallenOverlord = 104,
}

public enum ProjectileKind
{
    Bullet,
    Trap,
    Drone,
    Swarm,
    Necrodrone,
    Minion,
    Skimmer,
    Rocket,
    Flame,
    Croc,
    Wall
}

public sealed class BulletDef
{
    public ProjectileKind Type;
    public double SizeRatio, Health, Damage, Speed, ScatterRate, LifeLength, Absorption;
    public int Sides;
    public bool NeutralColor;
}

public sealed class BarrelDef
{
    public double Angle, Offset, Size, Width, Delay, Reload, Recoil, TrapezoidDirection, Distance;
    public bool IsTrapezoid, ForceFire, CanControlDrones;
    public int DroneCount;
    public string? Addon;
    public BulletDef Bullet = null!;
}

public sealed class TankDef
{
    public TankId Id;
    public string Name = "";
    public int LevelRequirement;
    public TankId[] Upgrades = [];
    public string? PreAddon;
    public string? PostAddon;
    public int Sides;
    public double Speed;
    public bool IsBoss;
    public string? BossAltName;
    public BarrelDef[] Barrels = [];
}

internal static class TankCatalog
{
    public static readonly TankDef[] All =
    [
        new() { Id = TankId.Basic, Name = "Tank", LevelRequirement = 0, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.Twin, TankId.Sniper, TankId.MachineGun, TankId.FlankGuard, TankId.Smasher], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Twin, Name = "Twin", LevelRequirement = 15, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.TripleShot, TankId.QuadTank, TankId.TwinFlank], Barrels =
        [
            new() { Angle = 0, Offset = -26, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 0.75, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.9, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 26, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 0.75, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.9, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Triplet, Name = "Triplet", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = -26, Size = 80, Width = 42, Delay = 0.5, Reload = 1, Recoil = 0.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.7, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 26, Size = 80, Width = 42, Delay = 0.5, Reload = 1, Recoil = 0.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.7, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 0.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.7, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.TripleShot, Name = "Triple Shot", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.Triplet, TankId.PentaShot, TankId.SpreadShot], Barrels =
        [
            new() { Angle = -0.785398163397448, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.7, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0.785398163397448, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.7, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.7, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.QuadTank, Name = "Quad Tank", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.OctoTank, TankId.Auto5], Barrels =
        [
            new() { Angle = 3.14159265358979, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.75, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -1.5707963267949, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.75, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 1.5707963267949, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.75, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.75, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.OctoTank, Name = "Octo Tank", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = -0.785398163397448, Offset = 0, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0.785398163397448, Offset = 0, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -2.35619449019234, Offset = 0, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 2.35619449019234, Offset = 0, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 3.14159265358979, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -1.5707963267949, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 1.5707963267949, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.65, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Sniper, Name = "Sniper", LevelRequirement = 15, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.Assassin, TankId.Overseer, TankId.Hunter, TankId.Trapper], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 110, Width = 42, Delay = 0, Reload = 1.5, Recoil = 3, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1.5, ScatterRate = 0.3, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.MachineGun, Name = "Machine Gun", LevelRequirement = 15, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.Destroyer, TankId.Gunner, TankId.Sprayer], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 0.5, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.7, Speed = 1, ScatterRate = 3, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.FlankGuard, Name = "Flank Guard", LevelRequirement = 15, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.TriAngle, TankId.QuadTank, TankId.TwinFlank, TankId.Auto3], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 3.14159265358979, Offset = 0, Size = 80, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.TriAngle, Name = "Tri-Angle", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.Booster, TankId.Fighter], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 3.66519142918809, Offset = 0, Size = 80, Width = 42, Delay = 0.5, Reload = 1, Recoil = 2.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.2, Speed = 1, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
            new() { Angle = 2.61799387799149, Offset = 0, Size = 80, Width = 42, Delay = 0.5, Reload = 1, Recoil = 2.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.2, Speed = 1, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
        ] },
        new() { Id = TankId.Destroyer, Name = "Destroyer", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.Hybrid, TankId.Annihilator, TankId.Skimmer, TankId.Rocketeer], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 95, Width = 71.4, Delay = 0, Reload = 4, Recoil = 15, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 2, Damage = 3, Speed = 0.7, ScatterRate = 1, LifeLength = 1, Absorption = 0.1 } },
        ] },
        new() { Id = TankId.Overseer, Name = "Overseer", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.Overlord, TankId.Necromancer, TankId.Manager, TankId.Overtrapper, TankId.Battleship, TankId.Factory], Barrels =
        [
            new() { Angle = -1.5707963267949, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 4, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 2, Damage = 0.7, Speed = 0.8, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
            new() { Angle = 1.5707963267949, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 4, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 2, Damage = 0.7, Speed = 0.8, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Overlord, Name = "Overlord", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = -1.5707963267949, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 2, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 2, Damage = 0.7, Speed = 0.8, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
            new() { Angle = 1.5707963267949, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 2, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 2, Damage = 0.7, Speed = 0.8, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 2, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 2, Damage = 0.7, Speed = 0.8, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
            new() { Angle = 3.14159265358979, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 2, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 2, Damage = 0.7, Speed = 0.8, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
        ] },
        new() { Id = TankId.TwinFlank, Name = "Twin Flank", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.TripleTwin, TankId.Battleship], Barrels =
        [
            new() { Angle = 0, Offset = -26, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 26, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 3.14159265358979, Offset = -26, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 3.14159265358979, Offset = 26, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.PentaShot, Name = "Penta Shot", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = -0.785398163397448, Offset = 0, Size = 80, Width = 42, Delay = 0.66, Reload = 1, Recoil = 0.7, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.55, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0.785398163397448, Offset = 0, Size = 80, Width = 42, Delay = 0.66, Reload = 1, Recoil = 0.7, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.55, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -0.392699081698724, Offset = 0, Size = 95, Width = 42, Delay = 0.33, Reload = 1, Recoil = 0.7, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.55, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0.392699081698724, Offset = 0, Size = 95, Width = 42, Delay = 0.33, Reload = 1, Recoil = 0.7, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.55, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 110, Width = 42, Delay = 0, Reload = 1, Recoil = 0.7, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.55, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Assassin, Name = "Assassin", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.Ranger, TankId.Stalker], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 120, Width = 42, Delay = 0, Reload = 2, Recoil = 3, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1.5, ScatterRate = 0.3, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Necromancer, Name = "Necromancer", LevelRequirement = 45, Speed = 1, Sides = 4, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = -1.5707963267949, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 11, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Necrodrone, SizeRatio = 1, Health = 2, Damage = 0.42, Speed = 0.72, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
            new() { Angle = 1.5707963267949, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 11, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Necrodrone, SizeRatio = 1, Health = 2, Damage = 0.42, Speed = 0.72, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
        ] },
        new() { Id = TankId.TripleTwin, Name = "Triple Twin", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = -26, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 26, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 2.0943951023932, Offset = -26, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 2.0943951023932, Offset = 26, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -2.0943951023932, Offset = -26, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -2.0943951023932, Offset = 26, Size = 95, Width = 42, Delay = 0.5, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Hunter, Name = "Hunter", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.Predator, TankId.Streamliner], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 110, Width = 42, Delay = 0, Reload = 2.5, Recoil = 0.3, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.75, Speed = 1.4, ScatterRate = 0.3, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 95, Width = 56.7, Delay = 0.2, Reload = 2.5, Recoil = 0.3, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.75, Speed = 1.4, ScatterRate = 0.3, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Gunner, Name = "Gunner", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.AutoGunner, TankId.GunnerTrapper, TankId.Streamliner], Barrels =
        [
            new() { Angle = 0, Offset = -32, Size = 65, Width = 25.2, Delay = 0.5, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.45, Damage = 0.5, Speed = 1.1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 32, Size = 65, Width = 25.2, Delay = 0.75, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.45, Damage = 0.5, Speed = 1.1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = -17, Size = 85, Width = 25.2, Delay = 0, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.45, Damage = 0.5, Speed = 1.1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 17, Size = 85, Width = 25.2, Delay = 0.25, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.45, Damage = 0.5, Speed = 1.1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Stalker, Name = "Stalker", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 120, Width = 42, Delay = 0, Reload = 2, Recoil = 3, IsTrapezoid = true, TrapezoidDirection = 3.14159265358979, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1.5, ScatterRate = 0.3, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Ranger, Name = "Ranger", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = "pronounced", Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 120, Width = 42, Delay = 0, Reload = 2, Recoil = 3, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1.5, ScatterRate = 0.3, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Booster, Name = "Booster", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 3.92699081698724, Offset = 0, Size = 70, Width = 42, Delay = 0.66, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.2, Speed = 1, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
            new() { Angle = 2.35619449019234, Offset = 0, Size = 70, Width = 42, Delay = 0.66, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.2, Speed = 1, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
            new() { Angle = 3.66519142918809, Offset = 0, Size = 80, Width = 42, Delay = 0.33, Reload = 1, Recoil = 2.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.2, Speed = 1, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
            new() { Angle = 2.61799387799149, Offset = 0, Size = 80, Width = 42, Delay = 0.33, Reload = 1, Recoil = 2.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.2, Speed = 1, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
        ] },
        new() { Id = TankId.Fighter, Name = "Fighter", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 1.5707963267949, Offset = 0, Size = 80, Width = 42, Delay = 0, Reload = 1.5, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.8, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -1.5707963267949, Offset = 0, Size = 80, Width = 42, Delay = 0, Reload = 1.5, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.8, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 3.66519142918809, Offset = 0, Size = 80, Width = 42, Delay = 0.5, Reload = 1, Recoil = 2.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.2, Speed = 1, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
            new() { Angle = 2.61799387799149, Offset = 0, Size = 80, Width = 42, Delay = 0.5, Reload = 1, Recoil = 2.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.2, Speed = 1, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
        ] },
        new() { Id = TankId.Hybrid, Name = "Hybrid", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 95, Width = 71.4, Delay = 0, Reload = 4, Recoil = 15, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 2, Damage = 3, Speed = 0.7, ScatterRate = 1, LifeLength = 1, Absorption = 0.1 } },
            new() { Angle = 3.14159265358979, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 2, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 1.4, Damage = 0.7, Speed = 1, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Manager, Name = "Manager", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 3, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 8, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 2, Damage = 0.7, Speed = 0.8, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Predator, Name = "Predator", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 110, Width = 42, Delay = 0, Reload = 3, Recoil = 0.3, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.75, Speed = 1.4, ScatterRate = 0.3, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 95, Width = 56.7, Delay = 0.2, Reload = 3, Recoil = 0.3, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.75, Speed = 1.4, ScatterRate = 0.3, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 80, Width = 71.4, Delay = 0.4, Reload = 3, Recoil = 0.3, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.75, Speed = 1.4, ScatterRate = 0.3, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Sprayer, Name = "Sprayer", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 110, Width = 42, Delay = 0.5, Reload = 1, Recoil = 0, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.1, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 0.5, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.7, Speed = 1, ScatterRate = 3, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Trapper, Name = "Trapper", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [TankId.TriTrapper, TankId.GunnerTrapper, TankId.Overtrapper, TankId.MegaTrapper, TankId.AutoTrapper], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 60, Width = 42, Delay = 0, Reload = 1.5, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = "trapLauncher", DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Trap, SizeRatio = 0.8, Health = 2, Damage = 1, Speed = 2, ScatterRate = 1, LifeLength = 8, Absorption = 1 } },
        ] },
        new() { Id = TankId.GunnerTrapper, Name = "Gunner Trapper", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = -16, Size = 75, Width = 21, Delay = 0.66, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 16, Size = 75, Width = 21, Delay = 0.33, Reload = 1, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.5, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 3.14159265358979, Offset = 0, Size = 60, Width = 54.6, Delay = 0, Reload = 3, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = "trapLauncher", DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Trap, SizeRatio = 0.8, Health = 2, Damage = 1, Speed = 2, ScatterRate = 1, LifeLength = 8, Absorption = 1 } },
        ] },
        new() { Id = TankId.Overtrapper, Name = "Overtrapper", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 60, Width = 42, Delay = 0, Reload = 1.5, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = "trapLauncher", DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Trap, SizeRatio = 0.8, Health = 2, Damage = 1, Speed = 2, ScatterRate = 1, LifeLength = 8, Absorption = 1 } },
            new() { Angle = 2.0943951023932, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 1, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 1.4, Damage = 0.7, Speed = 1, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
            new() { Angle = 4.18879020478639, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 6, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 1, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Drone, SizeRatio = 1, Health = 1.4, Damage = 0.7, Speed = 1, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
        ] },
        new() { Id = TankId.MegaTrapper, Name = "Mega Trapper", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 60, Width = 54.6, Delay = 0, Reload = 3.3, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = "trapLauncher", DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Trap, SizeRatio = 1.28, Health = 3.2, Damage = 1.6, Speed = 2, ScatterRate = 1, LifeLength = 8, Absorption = 1 } },
        ] },
        new() { Id = TankId.TriTrapper, Name = "Tri-Trapper", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 60, Width = 42, Delay = 0, Reload = 1.5, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = "trapLauncher", DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Trap, SizeRatio = 0.8, Health = 2, Damage = 1, Speed = 2, ScatterRate = 1, LifeLength = 3.2, Absorption = 1 } },
            new() { Angle = 2.0943951023932, Offset = 0, Size = 60, Width = 42, Delay = 0, Reload = 1.5, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = "trapLauncher", DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Trap, SizeRatio = 0.8, Health = 2, Damage = 1, Speed = 2, ScatterRate = 1, LifeLength = 3.2, Absorption = 1 } },
            new() { Angle = 4.18879020478639, Offset = 0, Size = 60, Width = 42, Delay = 0, Reload = 1.5, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = "trapLauncher", DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Trap, SizeRatio = 0.8, Health = 2, Damage = 1, Speed = 2, ScatterRate = 1, LifeLength = 3.2, Absorption = 1 } },
        ] },
        new() { Id = TankId.Smasher, Name = "Smasher", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = "smasher", Upgrades = [TankId.Landmine, TankId.AutoSmasher, TankId.Spike], Barrels =
        [
        ] },
        new() { Id = TankId.Landmine, Name = "Landmine", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = "landmine", Upgrades = [], Barrels =
        [
        ] },
        new() { Id = TankId.AutoGunner, Name = "Auto Gunner", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = "autoturret", Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = -32, Size = 65, Width = 25.2, Delay = 0.5, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.45, Damage = 0.5, Speed = 1.1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 32, Size = 65, Width = 25.2, Delay = 0.75, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.45, Damage = 0.5, Speed = 1.1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = -17, Size = 85, Width = 25.2, Delay = 0, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.45, Damage = 0.5, Speed = 1.1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 17, Size = 85, Width = 25.2, Delay = 0.25, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 0.45, Damage = 0.5, Speed = 1.1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Auto5, Name = "Auto 5", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = "auto5", Upgrades = [], Barrels =
        [
        ] },
        new() { Id = TankId.Auto3, Name = "Auto 3", LevelRequirement = 30, Speed = 1, Sides = 1, PreAddon = null, PostAddon = "auto3", Upgrades = [TankId.Auto5, TankId.AutoGunner], Barrels =
        [
        ] },
        new() { Id = TankId.SpreadShot, Name = "Spread Shot", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 1.30899693899575, Offset = 0, Size = 65, Width = 29.4, Delay = 0.833325, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -1.30899693899575, Offset = 0, Size = 65, Width = 29.4, Delay = 0.833325, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 1.0471975511966, Offset = 0, Size = 71, Width = 29.4, Delay = 0.666675, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -1.0471975511966, Offset = 0, Size = 71, Width = 29.4, Delay = 0.666675, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0.785398163397448, Offset = 0, Size = 77, Width = 29.4, Delay = 0.5, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -0.785398163397448, Offset = 0, Size = 77, Width = 29.4, Delay = 0.5, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0.523598775598299, Offset = 0, Size = 83, Width = 29.4, Delay = 0.333325, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -0.523598775598299, Offset = 0, Size = 83, Width = 29.4, Delay = 0.333325, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0.261799387799149, Offset = 0, Size = 89, Width = 29.4, Delay = 0.166675, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = -0.261799387799149, Offset = 0, Size = 89, Width = 29.4, Delay = 0.166675, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 0.6, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 2, Recoil = 0.1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Streamliner, Name = "Streamliner", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 110, Width = 42, Delay = 0, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.2, Speed = 1.1, ScatterRate = 0.3, LifeLength = 0.8, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 100, Width = 42, Delay = 0.2, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.2, Speed = 1.1, ScatterRate = 0.3, LifeLength = 0.8, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 90, Width = 42, Delay = 0.4, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.2, Speed = 1.1, ScatterRate = 0.3, LifeLength = 0.8, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 80, Width = 42, Delay = 0.6, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.2, Speed = 1.1, ScatterRate = 0.3, LifeLength = 0.8, Absorption = 1 } },
            new() { Angle = 0, Offset = 0, Size = 70, Width = 42, Delay = 0.8, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 0.7, Health = 1, Damage = 0.2, Speed = 1.1, ScatterRate = 0.3, LifeLength = 0.8, Absorption = 1 } },
        ] },
        new() { Id = TankId.AutoTrapper, Name = "Auto Trapper", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = "autoturret", Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 60, Width = 42, Delay = 0, Reload = 1.5, Recoil = 1, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = "trapLauncher", DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Trap, SizeRatio = 0.8, Health = 2, Damage = 1, Speed = 2, ScatterRate = 1, LifeLength = 8, Absorption = 1 } },
        ] },
        new() { Id = TankId.Battleship, Name = "Battleship", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 1.5707963267949, Offset = -20, Size = 75, Width = 29.4, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 3.14159265358979, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Swarm, SizeRatio = 0.7, Health = 1, Damage = 0.15, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 4.71238898038469, Offset = -20, Size = 75, Width = 29.4, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 3.14159265358979, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Swarm, SizeRatio = 0.7, Health = 1, Damage = 0.15, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 1.5707963267949, Offset = 20, Size = 75, Width = 29.4, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 3.14159265358979, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Swarm, SizeRatio = 0.7, Health = 1, Damage = 0.15, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
            new() { Angle = 4.71238898038469, Offset = 20, Size = 75, Width = 29.4, Delay = 0, Reload = 1, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 3.14159265358979, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Swarm, SizeRatio = 0.7, Health = 1, Damage = 0.15, Speed = 1, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Annihilator, Name = "Annihilator", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 95, Width = 96.6, Delay = 0, Reload = 4, Recoil = 17, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 2, Damage = 3, Speed = 0.7, ScatterRate = 1, LifeLength = 1, Absorption = 0.05 } },
        ] },
        new() { Id = TankId.AutoSmasher, Name = "Auto Smasher", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = "autosmasher", Upgrades = [], Barrels =
        [
        ] },
        new() { Id = TankId.Spike, Name = "Spike", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = null, PostAddon = "spike", Upgrades = [], Barrels =
        [
        ] },
        new() { Id = TankId.Factory, Name = "Factory", LevelRequirement = 45, Speed = 1, Sides = 4, PreAddon = null, PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 3, Recoil = 1, IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 6, CanControlDrones = true, ForceFire = false, Bullet = new() { Type = ProjectileKind.Minion, SizeRatio = 1, Health = 4, Damage = 0.7, Speed = 0.56, ScatterRate = 1, LifeLength = -1, Absorption = 1 } },
        ] },
        new() { Id = TankId.Skimmer, Name = "Skimmer", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = "launcher", PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 80, Width = 71.4, Delay = 0, Reload = 4, Recoil = 3, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Skimmer, SizeRatio = 1, Health = 3, Damage = 1, Speed = 0.5, ScatterRate = 1, LifeLength = 1.3, Absorption = 0.1 } },
        ] },
        new() { Id = TankId.Rocketeer, Name = "Rocketeer", LevelRequirement = 45, Speed = 1, Sides = 1, PreAddon = "launcher", PostAddon = null, Upgrades = [], Barrels =
        [
            new() { Angle = 0, Offset = 0, Size = 80, Width = 52.5, Delay = 0, Reload = 4, Recoil = 3, IsTrapezoid = true, TrapezoidDirection = 3.14159265358979, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Rocket, SizeRatio = 1, Health = 5, Damage = 1, Speed = 0.3, ScatterRate = 1, LifeLength = 1, Absorption = 0.1 } },
        ] },
    ];

    public static readonly TankDef[] Bosses =
    [
        new()
        {
            Id = TankId.Guardian, Name = "Guardian", BossAltName = "Guardian of the Pentagons", IsBoss = true,
            LevelRequirement = 75, Speed = 0.55, Sides = 3, PreAddon = null, PostAddon = null, Upgrades = [],
            Barrels =
            [
                // Petite rear trapezoid like the wiki Guardian (not diepcustom's oversized 100×71.4 stub).
                new()
                {
                    Angle = Math.PI, Offset = 0, Size = 52, Width = 48, Delay = 0, Reload = 0.36, Recoil = 1,
                    IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 24,
                    CanControlDrones = true, ForceFire = false,
                    Bullet = new()
                    {
                        Type = ProjectileKind.Drone, SizeRatio = 0.85, Health = 12.5, Damage = 0.56,
                        Speed = 1.7, ScatterRate = 1, LifeLength = 1.5, Absorption = 1, Sides = 3
                    }
                },
            ]
        },
        new()
        {
            Id = TankId.Summoner, Name = "Summoner", IsBoss = true,
            LevelRequirement = 75, Speed = 0.5, Sides = 4, PreAddon = null, PostAddon = null, Upgrades = [],
            Barrels =
            [
                SummonerGun(0),
                SummonerGun(Math.PI * 0.5),
                SummonerGun(Math.PI),
                SummonerGun(Math.PI * 1.5),
            ]
        },
        new()
        {
            Id = TankId.Defender, Name = "Defender", IsBoss = true,
            LevelRequirement = 75, Speed = 0.22, Sides = 3, PreAddon = null, PostAddon = "defender", Upgrades = [],
            Barrels =
            [
                DefenderTrap(Math.PI * 2 * (0 / 3.0 + 1.0 / 6.0)),
                DefenderTrap(Math.PI * 2 * (1 / 3.0 + 1.0 / 6.0)),
                DefenderTrap(Math.PI * 2 * (2 / 3.0 + 1.0 / 6.0)),
            ]
        },
        new()
        {
            Id = TankId.FallenBooster, Name = "Fallen Booster", IsBoss = true,
            LevelRequirement = 75, Speed = 1.05, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [],
            Barrels =
            [
                new() { Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 6.25, Damage = 0.8, Speed = 1.7, ScatterRate = 1, LifeLength = 1, Absorption = 1 } },
                new() { Angle = 3.92699081698724, Offset = 0, Size = 70, Width = 42, Delay = 0.66, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 6.25, Damage = 0.16, Speed = 1.7, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
                new() { Angle = 2.35619449019234, Offset = 0, Size = 70, Width = 42, Delay = 0.66, Reload = 1, Recoil = 0.2, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 6.25, Damage = 0.16, Speed = 1.7, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
                new() { Angle = 3.66519142918809, Offset = 0, Size = 80, Width = 42, Delay = 0.33, Reload = 1, Recoil = 2.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 6.25, Damage = 0.16, Speed = 1.7, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
                new() { Angle = 2.61799387799149, Offset = 0, Size = 80, Width = 42, Delay = 0.33, Reload = 1, Recoil = 2.5, IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0, CanControlDrones = false, ForceFire = false, Bullet = new() { Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 6.25, Damage = 0.16, Speed = 1.7, ScatterRate = 1, LifeLength = 0.5, Absorption = 1 } },
            ]
        },
        new()
        {
            Id = TankId.FallenOverlord, Name = "Fallen Overlord", IsBoss = true,
            LevelRequirement = 75, Speed = 0.5, Sides = 1, PreAddon = null, PostAddon = null, Upgrades = [],
            Barrels =
            [
                FallenOverlordGun(-Math.PI * 0.5),
                FallenOverlordGun(Math.PI * 0.5),
                FallenOverlordGun(0),
                FallenOverlordGun(Math.PI),
            ]
        },
    ];

    private static BarrelDef SummonerGun(double angle) => new()
    {
        Angle = angle, Offset = 0, Size = 58, Width = 38, Delay = 0, Reload = 0.36, Recoil = 1,
        IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 7,
        CanControlDrones = true, ForceFire = false,
        Bullet = new()
        {
            Type = ProjectileKind.Drone, SizeRatio = 0.9, Health = 12.5, Damage = 0.56,
            Speed = 1.7, ScatterRate = 1, LifeLength = -1, Absorption = 1, Sides = 4
        }
    };

    private static BarrelDef DefenderTrap(double angle) => new()
    {
        Angle = angle, Offset = 0, Size = 55, Width = 40, Delay = 0, Reload = 5, Recoil = 2,
        IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = "trapLauncher", DroneCount = 0,
        CanControlDrones = false, ForceFire = true,
        Bullet = new()
        {
            Type = ProjectileKind.Trap, SizeRatio = 0.8, Health = 12.5, Damage = 4, Speed = 5,
            ScatterRate = 1, LifeLength = 8, Absorption = 1, NeutralColor = true
        }
    };

    private static BarrelDef FallenOverlordGun(double angle) => new()
    {
        Angle = angle, Offset = 0, Size = 70, Width = 42, Delay = 0, Reload = 0.36, Recoil = 1,
        IsTrapezoid = true, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 7,
        CanControlDrones = true, ForceFire = false,
        Bullet = new()
        {
            Type = ProjectileKind.Drone, SizeRatio = 0.5, Health = 12.5, Damage = 0.56, Speed = 1.7,
            ScatterRate = 1, LifeLength = -1, Absorption = 1, Sides = 3
        }
    };

    private static readonly Dictionary<TankId, TankDef> Map =
        All.Concat(Bosses).ToDictionary(t => t.Id);

    private static readonly Dictionary<string, TankId> ModKeys = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<TankDef> ModDefs = [];
    private static int _nextModId = 1000;

    public static IEnumerable<TankDef> Playable => All.Concat(ModDefs.Where(d => !d.IsBoss));
    public static IEnumerable<TankDef> BossList => Bosses.Concat(ModDefs.Where(d => d.IsBoss));

    public static bool TryGet(TankId id, out TankDef def) => Map.TryGetValue(id, out def!);

    public static TankDef Get(TankId id) => Map.TryGetValue(id, out var def) ? def : Map[TankId.Basic];

    public static bool TryParseId(string text, out TankId id)
    {
        if (Enum.TryParse(text, true, out id) && Map.ContainsKey(id))
            return true;
        if (ModKeys.TryGetValue(text, out id))
            return true;
        if (int.TryParse(text, out var n) && Map.ContainsKey((TankId)n))
        {
            id = (TankId)n;
            return true;
        }
        id = TankId.Basic;
        return false;
    }

    public static void ClearModRegistrations()
    {
        foreach (var id in ModKeys.Values.ToList())
            Map.Remove(id);
        ModKeys.Clear();
        ModDefs.Clear();
        _nextModId = 1000;
    }

    public static bool UnregisterMod(string key)
    {
        if (!ModKeys.TryGetValue(key, out var id))
            return false;
        ModKeys.Remove(key);
        Map.Remove(id);
        ModDefs.RemoveAll(d => d.Id == id);
        return true;
    }

    public static bool TryRegisterFromLua(MoonSharp.Interpreter.Table table, out string key)
    {
        key = table.Get("id").Type == MoonSharp.Interpreter.DataType.String
            ? table.Get("id").String
            : table.Get("name").Type == MoonSharp.Interpreter.DataType.String
                ? table.Get("name").String.Replace(' ', '_')
                : $"mod_{_nextModId}";
        if (string.IsNullOrWhiteSpace(key))
            return false;
        if (ModKeys.ContainsKey(key) || Enum.TryParse<TankId>(key, true, out _))
        {
            // Allow replace of prior mod registration with same key.
            UnregisterMod(key);
        }

        var id = (TankId)_nextModId++;
        var def = new TankDef
        {
            Id = id,
            Name = table.Get("name").Type == MoonSharp.Interpreter.DataType.String
                ? table.Get("name").String
                : key,
            LevelRequirement = table.Get("level").Type == MoonSharp.Interpreter.DataType.Number
                ? (int)table.Get("level").Number
                : 0,
            Sides = table.Get("sides").Type == MoonSharp.Interpreter.DataType.Number
                ? Math.Max(1, (int)table.Get("sides").Number)
                : 1,
            Speed = table.Get("speed").Type == MoonSharp.Interpreter.DataType.Number
                ? table.Get("speed").Number
                : 1,
            IsBoss = table.Get("is_boss").Type == MoonSharp.Interpreter.DataType.Boolean
                && table.Get("is_boss").Boolean,
            BossAltName = table.Get("boss_name").Type == MoonSharp.Interpreter.DataType.String
                ? table.Get("boss_name").String
                : null,
            PreAddon = table.Get("pre_addon").Type == MoonSharp.Interpreter.DataType.String
                ? NullIfEmpty(table.Get("pre_addon").String)
                : null,
            PostAddon = table.Get("post_addon").Type == MoonSharp.Interpreter.DataType.String
                ? NullIfEmpty(table.Get("post_addon").String)
                : null,
            Upgrades = ParseUpgrades(table.Get("upgrades")),
            Barrels = ParseBarrels(table.Get("barrels"))
        };
        if (def.Barrels.Length == 0)
        {
            def.Barrels =
            [
                new()
                {
                    Angle = 0, Offset = 0, Size = 95, Width = 42, Delay = 0, Reload = 1, Recoil = 1,
                    IsTrapezoid = false, TrapezoidDirection = 0, Distance = 0, Addon = null, DroneCount = 0,
                    CanControlDrones = false, ForceFire = false,
                    Bullet = new()
                    {
                        Type = ProjectileKind.Bullet, SizeRatio = 1, Health = 1, Damage = 1, Speed = 1,
                        ScatterRate = 1, LifeLength = 1, Absorption = 1
                    }
                }
            ];
        }

        Map[id] = def;
        ModKeys[key] = id;
        ModDefs.Add(def);
        return true;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static TankId[] ParseUpgrades(MoonSharp.Interpreter.DynValue v)
    {
        if (v.Type != MoonSharp.Interpreter.DataType.Table)
            return [];
        var list = new List<TankId>();
        foreach (var pair in v.Table.Pairs)
        {
            if (pair.Value.Type == MoonSharp.Interpreter.DataType.String && TryParseId(pair.Value.String, out var id))
                list.Add(id);
        }
        return list.ToArray();
    }

    private static BarrelDef[] ParseBarrels(MoonSharp.Interpreter.DynValue v)
    {
        if (v.Type != MoonSharp.Interpreter.DataType.Table)
            return [];
        var list = new List<BarrelDef>();
        foreach (var pair in v.Table.Values)
        {
            if (pair.Type != MoonSharp.Interpreter.DataType.Table)
                continue;
            var b = pair.Table;
            var bulletTable = b.Get("bullet");
            var bullet = new BulletDef
            {
                Type = ParseProjectile(bulletTable.Type == MoonSharp.Interpreter.DataType.Table
                    ? bulletTable.Table.Get("type")
                    : MoonSharp.Interpreter.DynValue.Nil),
                SizeRatio = Num(bulletTable, "size", 1),
                Health = Num(bulletTable, "health", 1),
                Damage = Num(bulletTable, "damage", 1),
                Speed = Num(bulletTable, "speed", 1),
                ScatterRate = Num(bulletTable, "scatter", 1),
                LifeLength = Num(bulletTable, "life", 1),
                Absorption = Num(bulletTable, "absorption", 1),
                Sides = (int)Num(bulletTable, "sides", 0),
                NeutralColor = bulletTable.Type == MoonSharp.Interpreter.DataType.Table
                    && bulletTable.Table.Get("neutral").Type == MoonSharp.Interpreter.DataType.Boolean
                    && bulletTable.Table.Get("neutral").Boolean
            };
            list.Add(new BarrelDef
            {
                Angle = Num(b, "angle", 0),
                Offset = Num(b, "offset", 0),
                Size = Num(b, "size", 95),
                Width = Num(b, "width", 42),
                Delay = Num(b, "delay", 0),
                Reload = Num(b, "reload", 1),
                Recoil = Num(b, "recoil", 1),
                TrapezoidDirection = Num(b, "trap_dir", 0),
                Distance = Num(b, "distance", 0),
                IsTrapezoid = b.Get("trapezoid").Type == MoonSharp.Interpreter.DataType.Boolean && b.Get("trapezoid").Boolean,
                ForceFire = b.Get("force_fire").Type == MoonSharp.Interpreter.DataType.Boolean && b.Get("force_fire").Boolean,
                CanControlDrones = b.Get("control_drones").Type == MoonSharp.Interpreter.DataType.Boolean && b.Get("control_drones").Boolean,
                DroneCount = (int)Num(b, "drones", 0),
                Addon = b.Get("addon").Type == MoonSharp.Interpreter.DataType.String ? NullIfEmpty(b.Get("addon").String) : null,
                Bullet = bullet
            });
        }
        return list.ToArray();
    }

    private static double Num(MoonSharp.Interpreter.DynValue tableOrNil, string key, double fallback)
    {
        if (tableOrNil.Type != MoonSharp.Interpreter.DataType.Table)
            return fallback;
        return Num(tableOrNil.Table, key, fallback);
    }

    private static double Num(MoonSharp.Interpreter.Table table, string key, double fallback)
    {
        var v = table.Get(key);
        return v.Type == MoonSharp.Interpreter.DataType.Number ? v.Number : fallback;
    }

    private static ProjectileKind ParseProjectile(MoonSharp.Interpreter.DynValue v)
    {
        if (v.Type == MoonSharp.Interpreter.DataType.String &&
            Enum.TryParse<ProjectileKind>(v.String, true, out var kind))
            return kind;
        return ProjectileKind.Bullet;
    }
}

