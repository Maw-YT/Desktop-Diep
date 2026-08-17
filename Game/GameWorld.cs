using System.Windows.Media;

namespace DesktopDiep;

public sealed class GameWorld
{
    public const int TicksPerSecond = 25;
    public const double TickDt = 1.0 / TicksPerSecond;
    public const int MaxTanks = 12;

    public readonly List<TankEntity> Tanks = [];
    public readonly List<ShapeEntity> Shapes = [];
    public readonly List<BulletEntity> Bullets = [];
    public TankEntity? Selected;
    public double Width;
    public double Height;
    public double Alpha { get; private set; } = 1;
    public bool Interpolate = true;
    public bool ShowSelectionHalo;
    public bool ShowNav;
    public bool ShowHash;
    public bool CollideWindows = true;
    public bool CollideCursor = true;
    public RenderStyle RenderStyle = RenderStyle.New;
    public bool ArenaClosing;
    public bool ExitRequested;
    public nint OverlayHwnd;
    public double DrawAlpha => Interpolate ? Alpha : 1;

    public DebugState Debug { get; } = new();
    public NotificationSystem Notifications { get; } = new();
    public CursorState Cursor { get; } = new();
    public WindowObstacles Windows { get; } = new();
    internal NavGrid Nav { get; } = new();
    internal ModHost? Mods { get; set; }
    internal Random ModRandom => _rng;

    private readonly Random _rng = new();
    private readonly ShapeSpawner _spawner;
    private readonly CollisionSystem _collisions = new();
    private double _accumulator;
    private int _nextTankId = 1;
    private bool _closersRetreating;
    private double _closerLinger = -1;
    private double _exitDelay;
    private double _bossSpawnIn;
    private string? _pendingClassName;

    public GameWorld()
    {
        _spawner = new ShapeSpawner(_rng);
    }

    public void Reset(double width, double height, double startX, double startY)
    {
        Width = width;
        Height = height;
        Shapes.Clear();
        Bullets.Clear();
        Tanks.Clear();
        _spawner.Reset();
        _accumulator = 0;
        Alpha = 1;
        ArenaClosing = false;
        ExitRequested = false;
        _closersRetreating = false;
        _closerLinger = -1;
        _exitDelay = 0;
        Notifications.Clear();
        _nextTankId = 1;
        ScheduleBossSpawn();
        Selected = SpawnTank(startX, startY);
        _spawner.Fill(Shapes, Width, Height, 18);
        Debug.Flash("Reset");
        Mods?.Emit("world_reset");
    }

    public TankEntity? SpawnTank(double? x = null, double? y = null, TankId? classId = null)
    {
        if (ArenaClosing)
        {
            Debug.Flash("Arena closing");
            return null;
        }
        if (PlayerTankCount() >= MaxTanks)
        {
            Debug.Flash("Tank limit");
            return null;
        }

        var tank = new TankEntity
        {
            Id = _nextTankId,
            Fill = DiepColors.Team(_nextTankId - 1)
        };
        _nextTankId++;
        tank.XpForNext = TankStats.XpNeeded(1);
        tank.X = x ?? (80 + _rng.NextDouble() * Math.Max(80, Width - 160));
        tank.Y = y ?? (80 + _rng.NextDouble() * Math.Max(80, Height - 160));
        TankClasses.Set(tank, classId ?? TankId.Basic);
        if (classId is null)
            TankClasses.PlanUpgrades(tank, _rng);
        else
            tank.ClassPlan.Clear();
        TankStats.Recalc(tank);
        tank.Health = tank.MaxHealth;
        tank.Snap();
        Tanks.Add(tank);
        Selected = tank;
        Debug.Flash($"{tank.Class.Name}");
        Mods?.EmitTank("tank_spawn", tank);
        return tank;
    }

    /// <summary>Low-level bullet spawn for Lua mods. Returns null if the bullet cap is hit.</summary>
    internal BulletEntity? SpawnBulletFromMod(
        double x, double y, double vx, double vy, double angle,
        double radius, double damage, double health, double life,
        int ownerId, ProjectileKind kind, Color fill, bool visible)
    {
        if (Bullets.Count > 200)
            return null;
        var shot = new BulletEntity
        {
            X = x,
            Y = y,
            Vx = vx,
            Vy = vy,
            Angle = angle,
            MovementAngle = angle,
            Radius = Math.Max(1, radius),
            Damage = damage,
            Health = Math.Max(0.01, health),
            Life = life,
            Mass = 0.22,
            Absorption = 1,
            PushFactor = Math.Max(1, damage),
            Kind = kind,
            OwnerId = ownerId,
            Fill = fill,
            Visible = visible,
            Accel = 0
        };
        shot.Snap();
        Bullets.Add(shot);
        Mods?.EmitBullet("bullet_spawn", shot);
        return shot;
    }

    public void CloseArena()
    {
        if (ArenaClosing || ExitRequested)
            return;
        if (Mods?.EmitCancel("arena_close") == true)
            return;
        ArenaClosing = true;
        _closersRetreating = false;
        _closerLinger = -1;
        _exitDelay = 0;
        Notifications.Arena("Arena closed: No players can join", 8, "arena_closed");
        Debug.Flash("Arena closing", 2.2);
        var count = Math.Clamp(3 + PlayerTankCount(), 4, 8);
        for (var i = 0; i < count; i++)
            SpawnCloser(i);
    }

    private int PlayerTankCount()
    {
        var n = 0;
        foreach (var t in Tanks)
        {
            if (!t.IsArenaCloser && !t.IsBoss)
                n++;
        }
        return n;
    }

    private int LivingPlayers()
    {
        var n = 0;
        foreach (var t in Tanks)
        {
            if (!t.IsArenaCloser && !t.IsBoss && t.Alive)
                n++;
        }
        return n;
    }

    private int LivingShapes()
    {
        var n = 0;
        foreach (var s in Shapes)
        {
            if (!s.Destroy.Active)
                n++;
        }
        return n;
    }

    private bool HasLivingBoss()
    {
        foreach (var t in Tanks)
        {
            if (t.IsBoss && (t.Alive || t.Destroy.Active))
                return true;
        }
        return false;
    }

    public TankEntity? SpawnBoss(TankId? kind = null)
    {
        if (ArenaClosing)
        {
            Debug.Flash("Arena closing");
            return null;
        }
        if (HasLivingBoss())
        {
            Debug.Flash("Boss already out");
            return null;
        }

        var bosses = TankCatalog.BossList.ToArray();
        if (bosses.Length == 0)
        {
            Debug.Flash("No bosses");
            return null;
        }
        var id = kind ?? bosses[_rng.Next(bosses.Length)].Id;
        if (!TankCatalog.TryGet(id, out var def) || !def.IsBoss)
        {
            Debug.Flash("Unknown boss");
            return null;
        }

        var tank = new TankEntity
        {
            Id = _nextTankId++,
            IsBoss = true,
            BossAltName = def.BossAltName,
            BossXp = 3000,
            Level = 75,
            Fill = BossFill(id),
            Absorption = 0.05,
            PushFactor = 16,
            Mass = 40
        };
        tank.X = 120 + _rng.NextDouble() * Math.Max(80, Width - 240);
        tank.Y = 120 + _rng.NextDouble() * Math.Max(80, Height - 240);
        for (var s = 0; s < 8; s++)
            tank.Stats[s] = s == TankStats.Reload ? 7 : 0;
        TankClasses.Set(tank, id);
        TankStats.Recalc(tank);
        ApplyBossStats(tank);
        tank.Health = tank.MaxHealth;
        tank.Snap();
        Tanks.Add(tank);
        var label = def.BossAltName ?? def.Name;
        Notifications.Server($"The {label} has spawned!", 8, "boss_spawn");
        ScheduleBossSpawn();
        Debug.Flash(def.Name);
        Mods?.EmitTank("boss_spawn", tank);
        return tank;
    }

    internal double BossSpawnIn
    {
        get => _bossSpawnIn;
        set => _bossSpawnIn = Math.Max(0, value);
    }

    private void ScheduleBossSpawn() =>
        _bossSpawnIn = 600 + _rng.NextDouble() * 300; // 10–15 minutes

    private void TickBossSpawn(double dt)
    {
        if (HasLivingBoss())
            return;
        _bossSpawnIn -= dt;
        if (_bossSpawnIn > 0)
            return;
        if (Mods?.EmitCancel("boss_timer") == true)
        {
            ScheduleBossSpawn();
            return;
        }
        if (SpawnBoss() is null)
            ScheduleBossSpawn();
    }

    private static System.Windows.Media.Color BossFill(TankId id) => id switch
    {
        TankId.Guardian => DiepColors.Crasher,
        TankId.Summoner => DiepColors.Square,
        TankId.Defender => DiepColors.Triangle,
        TankId.FallenBooster or TankId.FallenOverlord => DiepColors.Fallen,
        _ => DiepColors.Fallen
    };

    private static void ApplyBossStats(TankEntity tank)
    {
        tank.MaxHealth = 3000;
        tank.Health = 3000;
        tank.Radius = tank.ClassId switch
        {
            TankId.Guardian => 72,
            TankId.Summoner => 78,
            TankId.Defender => 82,
            TankId.FallenBooster or TankId.FallenOverlord => 58,
            _ => 58
        };
        tank.Mass = 48;
        tank.Absorption = 0.05;
        tank.XpForNext = 1;
        tank.XpIntoLevel = 0;
    }

    private void SpawnCloser(int index)
    {
        var edge = index % 4;
        var along = 0.15 + _rng.NextDouble() * 0.7;
        double x, y;
        switch (edge)
        {
            case 0:
                x = -80;
                y = Height * along;
                break;
            case 1:
                x = Width + 80;
                y = Height * along;
                break;
            case 2:
                x = Width * along;
                y = -80;
                break;
            default:
                x = Width * along;
                y = Height + 80;
                break;
        }

        var tank = new TankEntity
        {
            Id = _nextTankId++,
            Fill = DiepColors.ArenaCloser,
            IsArenaCloser = true,
            Radius = 52,
            Mass = 40,
            Absorption = 0,
            PushFactor = 20,
            Level = 45
        };
        tank.X = x;
        tank.Y = y;
        tank.Angle = Math.Atan2(Height * 0.5 - y, Width * 0.5 - x);
        TankClasses.Set(tank, TankId.Basic);
        for (var s = 0; s < 8; s++)
            tank.Stats[s] = 7;
        TankStats.Recalc(tank);
        tank.Radius = 52;
        tank.Health = tank.MaxHealth = 1e9;
        tank.Snap();
        Tanks.Add(tank);
    }

    public ShapeEntity SpawnShape(ShapeKind? kind = null, double? x = null, double? y = null, bool notify = true)
    {
        var shape = _spawner.Spawn(Shapes, Width, Height, kind);
        if (x is double sx)
        {
            shape.X = sx;
            shape.OrbitCx = sx;
        }
        if (y is double sy)
        {
            shape.Y = sy;
            shape.OrbitCy = sy;
        }
        shape.Snap();
        Shapes.Add(shape);
        if (notify)
            Debug.Flash(kind is null ? "Shape" : kind.Value.ToString());
        Mods?.EmitShape("shape_spawn", shape);
        return shape;
    }

    internal void ForEachHashCell(Action<int, int, int> visit) => _collisions.ForEachHashCell(visit);

    public void SelectTank(int index)
    {
        if (index < 0 || index >= Tanks.Count)
            return;
        Selected = Tanks[index];
        Debug.Flash(Selected.Class.Name);
    }

    public void SelectById(int id)
    {
        var tank = FindTank(id);
        if (tank is null)
            return;
        Selected = tank;
        Debug.Flash(tank.Class.Name);
    }

    public void SetSelectedStat(int stat, int value)
    {
        if (Selected is null || Selected.IsBoss || Selected.IsArenaCloser)
            return;
        if (TankStats.SetLevel(Selected, stat, value))
            Debug.Flash($"{TankStats.Names[stat]} {Selected.Stats[stat]}");
    }

    public void SetSelectedClass(TankId id)
    {
        if (Selected is null || Selected.IsBoss || Selected.IsArenaCloser)
            return;
        TankClasses.Set(Selected, id);
        TankClasses.PlanUpgrades(Selected, _rng);
        TankStats.Recalc(Selected);
        Debug.Flash(Selected.Class.Name);
    }

    public void RemoveSelected()
    {
        if (Selected is null)
            return;
        if (TryRemoveTank(Selected))
            Debug.Flash("Removed");
    }

    internal bool TryRemoveTank(TankEntity tank, bool protectLastPlayer = true)
    {
        if (protectLastPlayer && !tank.IsBoss && !tank.IsArenaCloser &&
            Tanks.Count(t => !t.IsArenaCloser && !t.IsBoss) <= 1)
            return false;
        var i = Tanks.IndexOf(tank);
        if (i < 0)
            return false;
        if (IsGrabbed(PhysKind.Tank, i))
            Cursor.Release();
        ClearOwnedBullets(tank.Id);
        Tanks.RemoveAt(i);
        if (Selected == tank)
            Selected = Tanks.Find(t => !t.IsArenaCloser);
        return true;
    }

    internal void ResetKeepSize()
    {
        var sx = Selected?.X ?? Width * 0.5;
        var sy = Selected?.Y ?? Height * 0.5;
        Reset(Math.Max(1, Width), Math.Max(1, Height), sx, sy);
    }

    internal void ClearShapes()
    {
        if (Cursor.GrabKind == PhysKind.Shape)
            Cursor.Release();
        Shapes.Clear();
    }

    internal void ClearBullets()
    {
        if (Cursor.GrabKind == PhysKind.Bullet)
            Cursor.Release();
        Bullets.Clear();
    }

    internal object? EntityFromPhys(PhysKind kind, int index) => kind switch
    {
        PhysKind.Tank when (uint)index < (uint)Tanks.Count => Tanks[index],
        PhysKind.Shape when (uint)index < (uint)Shapes.Count => Shapes[index],
        PhysKind.Bullet when (uint)index < (uint)Bullets.Count => Bullets[index],
        _ => null
    };

    internal void ModForceShoot(TankEntity tank)
    {
        if (!tank.Alive || tank.Destroy.Active)
            return;
        tank.Brain.WantsShot = true;
        for (var i = 0; i < tank.Barrels.Length; i++)
        {
            var barrel = tank.Barrels[i];
            barrel.Pos = barrel.ReloadTime * (1 + barrel.Def.Delay);
            TickShootCycle(tank, i, barrel, true, tank.Angle, tank.X, tank.Y, recoil: true);
        }
    }

    internal void ModSetLevel(TankEntity tank, int level)
    {
        tank.Level = Math.Clamp(level, 1, 45);
        tank.XpForNext = TankStats.XpNeeded(tank.Level);
        tank.XpIntoLevel = Math.Min(tank.XpIntoLevel, Math.Max(0, tank.XpForNext - 1));
        TankStats.Recalc(tank);
        tank.Health = tank.MaxHealth;
    }

    public void Resize(double width, double height)
    {
        Width = width;
        Height = height;
        foreach (var tank in Tanks)
        {
            Math2.Bounce(ref tank.X, ref tank.Y, ref tank.Vx, ref tank.Vy, tank.Radius, Width, Height);
            tank.Snap();
        }
        foreach (var s in Shapes)
        {
            Math2.Bounce(ref s.X, ref s.Y, ref s.Vx, ref s.Vy, s.Radius, Width, Height);
            s.Snap();
        }
    }

    public void Advance(double renderDt)
    {
        Debug.TickNotice(renderDt);
        Notifications.Tick(renderDt);
        if (Debug.Paused)
        {
            Alpha = 1;
            Debug.Alpha = 1;
            return;
        }

        _accumulator += renderDt;
        if (_accumulator > TickDt * 5)
            _accumulator = TickDt * 5;

        while (_accumulator >= TickDt)
        {
            Capture();
            FixedTick(TickDt);
            _accumulator -= TickDt;
            Debug.Tick++;
        }

        Alpha = _accumulator / TickDt;
        Debug.Alpha = DrawAlpha;
    }

    private void Capture()
    {
        foreach (var tank in Tanks)
            tank.Capture();
        foreach (var s in Shapes)
            s.Capture();
        foreach (var b in Bullets)
            b.Capture();
    }

    private void FixedTick(double dt)
    {
        Mods?.Tick(dt);
        Cursor.BeginTick();
        TickPointer();
        if (CollideWindows)
        {
            Windows.Refresh(OverlayHwnd);
            Nav.Rebuild(Width, Height, Windows.Boxes, 24);
        }
        else
            Nav.Rebuild(Width, Height, [], 0);
        if (!ArenaClosing)
        {
            _spawner.Maintain(Shapes, Width, Height, dt);
            TickBossSpawn(dt);
        }

        foreach (var s in Shapes)
        {
            s.Flash.Tick();
            if (s.Destroy.Active)
                s.Destroy.Tick();
        }
        foreach (var b in Bullets)
        {
            if (b.Destroy.Active)
                b.Destroy.Tick();
        }

        for (var i = 0; i < Tanks.Count; i++)
            TickTank(Tanks[i], i, dt);
        SweepDeadBosses();

        for (var i = 0; i < Shapes.Count; i++)
        {
            if (!Shapes[i].Destroy.Active && !IsGrabbed(PhysKind.Shape, i))
            {
                if (Shapes[i].Kind == ShapeKind.Crasher)
                    ShapeMotion.TickCrasher(Shapes[i], Tanks);
                else
                    ShapeMotion.TickIdle(Shapes[i]);
            }
        }

        var n = Bullets.Count;
        for (var i = 0; i < n; i++)
        {
            var b = Bullets[i];
            if (b.Destroy.Active)
                continue;
            b.Life -= 1;
            Projectile.Tick(b, FindTank(b.OwnerId), Shapes, Tanks);
            TickProjectileGuns(b);
        }

        ApplyGrab();
        ResolveCollisions(dt);
        ApplyPendingClass();
        ApplyAllPhysics();
        if (CollideWindows)
            ApplyWindowCollisions();
        Cursor.EndTick();
        PruneDead();
        TickArenaClose(dt);
        Debug.HashCells = _collisions.CellCount;
        Debug.HashPairs = _collisions.PairCount;
        Mods?.PostTick(dt);
    }

    private void TickArenaClose(double dt)
    {
        if (!ArenaClosing || ExitRequested)
            return;
        if (!_closersRetreating)
        {
            if (LivingPlayers() > 0 || LivingShapes() > 0)
            {
                _closerLinger = -1;
                return;
            }
            if (_closerLinger < 0)
            {
                _closerLinger = 2.4;
                Notifications.Server("Arena cleared", 3, "arena_cleared");
            }
            _closerLinger -= dt;
            if (_closerLinger > 0)
                return;

            _closersRetreating = true;
            foreach (var tank in Tanks)
            {
                if (!tank.IsArenaCloser || !tank.Alive)
                    continue;
                tank.Alive = false;
                tank.Health = 0;
                ClearOwnedBullets(tank.Id);
                tank.Destroy.Begin();
                tank.Snap();
            }
            _exitDelay = 0.55;
            return;
        }

        _exitDelay -= dt;
        var anyCloser = false;
        for (var i = Tanks.Count - 1; i >= 0; i--)
        {
            var tank = Tanks[i];
            if (!tank.IsArenaCloser)
                continue;
            anyCloser = true;
            if (tank.Destroy.Active)
                tank.Destroy.Tick();
            if (!tank.Alive && (!tank.Destroy.Active || tank.Destroy.Finished))
            {
                if (Selected == tank)
                    Selected = null;
                Tanks.RemoveAt(i);
            }
        }
        if (_exitDelay <= 0 && !anyCloser)
            ExitRequested = true;
    }

    private void SweepDeadBosses()
    {
        for (var i = Tanks.Count - 1; i >= 0; i--)
        {
            var tank = Tanks[i];
            if (!tank.IsBoss || tank.Alive)
                continue;
            if (tank.Destroy.Active && !tank.Destroy.Finished)
                continue;
            if (Selected == tank)
                Selected = Tanks.Find(t => t.Alive && !t.IsArenaCloser && t != tank);
            ClearOwnedBullets(tank.Id);
            Tanks.RemoveAt(i);
        }
    }

    private void TickTank(TankEntity tank, int index, double dt)
    {
        tank.Flash.Tick();
        if (!tank.Alive)
        {
            if (tank.IsArenaCloser)
                return;
            if (tank.IsBoss)
            {
                if (tank.Destroy.Active)
                    tank.Destroy.Tick();
                return;
            }
            if (tank.Destroy.Active)
            {
                tank.Destroy.Tick();
                if (tank.Destroy.Finished)
                {
                    tank.Destroy.Reset();
                    tank.Respawn = ArenaClosing ? 1e9 : 2.2;
                }
            }
            else
            {
                if (ArenaClosing)
                    return;
                tank.Respawn -= dt;
                if (tank.Respawn <= 0)
                    RespawnTank(tank);
            }
            return;
        }

        if (IsGrabbed(PhysKind.Tank, index))
            return;

        if (tank.IsArenaCloser)
        {
            TickCloser(tank);
            return;
        }

        if (tank.IsBoss)
        {
            TickBoss(tank, dt);
            return;
        }

        if (!tank.AiEnabled)
        {
            TickBarrels(tank);
            Regen(tank, dt);
            return;
        }

        tank.Brain.SpendPoints(tank);
        tank.Brain.Think(tank, this, out var ax, out var ay);
        if (Mods is not null)
        {
            var script = Mods.Mods.FirstOrDefault(m => m.Enabled)?.Script;
            if (script is not null)
            {
                var e = new MoonSharp.Interpreter.Table(script)
                {
                    ["cancel"] = false,
                    ["aim_x"] = tank.Brain.AimX,
                    ["aim_y"] = tank.Brain.AimY,
                    ["wants_shot"] = tank.Brain.WantsShot,
                    ["move_x"] = ax,
                    ["move_y"] = ay
                };
                Mods.Events.EmitCancelable(script, "think", e, EntityProxies.Tank(script, tank, this));
                if (e.Get("aim_x").Type == MoonSharp.Interpreter.DataType.Number)
                    tank.Brain.AimX = e.Get("aim_x").Number;
                if (e.Get("aim_y").Type == MoonSharp.Interpreter.DataType.Number)
                    tank.Brain.AimY = e.Get("aim_y").Number;
                if (e.Get("wants_shot").Type == MoonSharp.Interpreter.DataType.Boolean)
                    tank.Brain.WantsShot = e.Get("wants_shot").Boolean;
                if (e.Get("move_x").Type == MoonSharp.Interpreter.DataType.Number)
                    ax = e.Get("move_x").Number;
                if (e.Get("move_y").Type == MoonSharp.Interpreter.DataType.Number)
                    ay = e.Get("move_y").Number;
            }
        }
        DiepPhysics.MaintainVelocity(ref tank.Vx, ref tank.Vy, Math.Atan2(ay, ax),
            (ax == 0 && ay == 0) ? 0 : TankStats.MoveSpeed(tank));
        TickBarrels(tank);
        Regen(tank, dt);
    }

    private void TickBoss(TankEntity tank, double dt)
    {
        BossBrain.Think(tank, this, out var ax, out var ay);
        DiepPhysics.MaintainVelocity(ref tank.Vx, ref tank.Vy, Math.Atan2(ay, ax),
            (ax == 0 && ay == 0) ? 0 : TankStats.MoveSpeed(tank));
        TickBarrels(tank);
        tank.Health = Math.Min(tank.MaxHealth, tank.Health + tank.MaxHealth / 25000.0);
    }

    private void TickCloser(TankEntity tank)
    {
        double tx = 0, ty = 0, tvx = 0, tvy = 0;
        var best = double.MaxValue;
        var found = false;

        foreach (var other in Tanks)
        {
            if (other.IsArenaCloser || !other.Alive || other.Destroy.Active)
                continue;
            var dx = other.X - tank.X;
            var dy = other.Y - tank.Y;
            var d = dx * dx + dy * dy;
            if (d >= best)
                continue;
            best = d;
            tx = other.X;
            ty = other.Y;
            tvx = other.Vx;
            tvy = other.Vy;
            found = true;
        }

        if (!found)
        {
            best = double.MaxValue;
            foreach (var s in Shapes)
            {
                if (s.Destroy.Active)
                    continue;
                var dx = s.X - tank.X;
                var dy = s.Y - tank.Y;
                var d = dx * dx + dy * dy;
                if (d >= best)
                    continue;
                best = d;
                tx = s.X;
                ty = s.Y;
                tvx = s.Vx;
                tvy = s.Vy;
                found = true;
            }
        }

        if (!found)
        {
            tank.Brain.WantsShot = false;
            DiepPhysics.MaintainVelocity(ref tank.Vx, ref tank.Vy, tank.Angle, TankStats.MoveSpeed(tank) * 0.35);
            TickBarrels(tank);
            return;
        }

        var dist = Math.Sqrt(best);
        var speed = (20 + tank.Stats[TankStats.BulletSpeed] * 3) * (tank.Radius / 50.0) + 30 * (tank.Radius / 50.0);
        var t = dist / Math.Max(4, speed);
        for (var i = 0; i < 3; i++)
        {
            var dx = tx + tvx * t - tank.X - tank.Vx * 0.2 * t;
            var dy = ty + tvy * t - tank.Y - tank.Vy * 0.2 * t;
            t = Math.Sqrt(dx * dx + dy * dy) / Math.Max(4, speed);
        }
        tank.Brain.AimX = tx + tvx * t;
        tank.Brain.AimY = ty + tvy * t;
        tank.Angle = Math.Atan2(tank.Brain.AimY - tank.Y, tank.Brain.AimX - tank.X);
        tank.Brain.WantsShot = true;
        DiepPhysics.MaintainVelocity(ref tank.Vx, ref tank.Vy, tank.Angle, TankStats.MoveSpeed(tank) * 1.35);
        TickBarrels(tank);
    }

    private void TickBarrels(TankEntity tank)
    {
        if (!tank.IsBoss)
            tank.RotatorAngle += 0.022;
        foreach (var g in tank.Guards)
            g.Tick();
        for (var i = 0; i < tank.Barrels.Length; i++)
            TickShootCycle(tank, i, tank.Barrels[i], tank.Brain.WantsShot, tank.Angle, tank.X, tank.Y, recoil: true);
        TickTurrets(tank);
    }

    private void TickTurrets(TankEntity tank)
    {
        for (var i = 0; i < tank.Turrets.Length; i++)
        {
            var turret = tank.Turrets[i];
            TurretPose(tank, turret, out var x, out var y);
            var wants = false;
            if (tank.Brain.HasTarget)
            {
                turret.Angle = Math.Atan2(tank.Brain.AimY - y, tank.Brain.AimX - x);
                wants = true;
            }
            else if (tank.IsBoss)
            {
                // Fixed on the body; face outward, no idle spin.
                turret.Angle = tank.Angle + turret.MountAngle;
            }
            else if (turret.Orbit > 0)
            {
                turret.Angle = turret.MountAngle + tank.RotatorAngle;
            }
            else
            {
                turret.Angle += 0.022;
            }

            TickShootCycle(tank, 100 + i, turret.Barrel, wants, turret.Angle, x, y, recoil: false);
        }
    }

    private static void TurretPose(TankEntity tank, AutoTurretState turret, out double x, out double y)
    {
        var a = tank.IsBoss
            ? tank.Angle + turret.MountAngle
            : turret.MountAngle + tank.RotatorAngle;
        var r = tank.Radius * turret.Orbit;
        x = tank.X + Math.Cos(a) * r;
        y = tank.Y + Math.Sin(a) * r;
    }

    private void TickShootCycle(TankEntity tank, int index, BarrelState barrel, bool wantsShot, double facing, double ox, double oy, bool recoil, BulletEntity? from = null)
    {
        var def = barrel.Def;
        var reloadTime = barrel.ReloadTime;
        var always = def.ForceFire || Projectile.AlwaysFire(def.Bullet.Type);
        barrel.ShotAge++;
        if (def.Bullet.Type is ProjectileKind.Necrodrone or ProjectileKind.Wall)
        {
            barrel.Pos = reloadTime;
            return;
        }

        if (barrel.Pos >= reloadTime)
        {
            if (!wantsShot && !always)
            {
                barrel.Pos = reloadTime;
                return;
            }
            if (def.DroneCount > 0 && CountDrones(tank, index, def) >= MaxDrones(tank, def))
            {
                barrel.Pos = reloadTime;
                return;
            }
        }

        if (barrel.Pos >= reloadTime * (1 + def.Delay))
        {
            ShootBarrel(tank, index, barrel, facing, ox, oy, recoil, from);
            barrel.Pos = reloadTime * def.Delay;
        }

        barrel.Pos += 1;
    }

    private int CountDrones(TankEntity tank, int barrelIndex, BarrelDef def)
    {
        var n = 0;
        var necro = def.Bullet.Type == ProjectileKind.Necrodrone;
        foreach (var b in Bullets)
        {
            if (b.Destroy.Active || b.OwnerId != tank.Id || b.Kind != def.Bullet.Type)
                continue;
            if (!necro && b.BarrelIndex != barrelIndex)
                continue;
            n++;
        }
        return n;
    }

    private static int MaxDrones(TankEntity tank, BarrelDef def)
    {
        if (def.Bullet.Type == ProjectileKind.Necrodrone)
            return 11 + tank.Stats[TankStats.Reload];
        return def.DroneCount;
    }

    private void TickProjectileGuns(BulletEntity parent)
    {
        if (parent.Guns.Length == 0 || parent.Destroy.Active)
            return;
        var owner = FindTank(parent.OwnerId);
        if (owner is null || !owner.Alive)
            return;
        var wants = parent.Kind switch
        {
            ProjectileKind.Rocket => parent.Age >= TankStats.ReloadTicks(owner),
            ProjectileKind.Minion => MinionWantsShot(parent, owner),
            _ => true
        };
        for (var i = 0; i < parent.Guns.Length; i++)
            TickShootCycle(owner, 1000 + i, parent.Guns[i], wants, parent.Angle, parent.X, parent.Y, recoil: false, parent);
    }

    private static bool MinionWantsShot(BulletEntity minion, TankEntity owner)
    {
        if (minion.RestCycle || !owner.Brain.HasTarget)
            return false;
        var dx = owner.Brain.AimX - minion.X;
        var dy = owner.Brain.AimY - minion.Y;
        return dx * dx + dy * dy <= 170 * 170;
    }

    private void ShootBarrel(TankEntity tank, int index, BarrelState barrel, double facing, double ox, double oy, bool recoilTank, BulletEntity? from = null)
    {
        if (Bullets.Count > 160)
            return;

        var def = barrel.Def;
        var bullet = def.Bullet;
        if (bullet.Type is ProjectileKind.Wall)
            return;
        if (Mods?.EmitCancel("pre_shoot", e =>
            {
                e["barrel"] = index;
                e["facing"] = facing;
            }, tank) == true)
            return;

        var scale = from is not null
            ? from.Radius / 50.0
            : index >= 100 && index < 1000
                ? TankStats.TurretGunScale(tank)
                : TankStats.GunScale(tank);
        var scatter = (Math.PI / 180.0) * bullet.ScatterRate * (_rng.NextDouble() - 0.5) * 10;
        var angle = facing + def.Angle + scatter;
        var size = def.Size * scale;
        var offset = def.Offset * scale;
        var dist = (from is not null ? def.Distance : TankStats.BarrelDistance(tank, def)) * scale;
        var c = Math.Cos(angle);
        var s = Math.Sin(angle);
        var accel = (20 + tank.Stats[TankStats.BulletSpeed] * 3) * bullet.Speed * scale;
        var speed = accel + 30 * scale - _rng.NextDouble() * bullet.ScatterRate * scale;
        if (bullet.Type == ProjectileKind.Trap)
            speed = accel / 2 + 30 * scale - _rng.NextDouble() * bullet.ScatterRate * scale;
        var life = bullet.LifeLength < 0 ? 1e9 : bullet.LifeLength * 75;

        var shot = new BulletEntity
        {
            X = ox + c * size - s * offset + c * dist,
            Y = oy + s * size + c * offset + s * dist,
            Vx = c * speed + (from is null ? tank.Vx : from.Vx) * 0.2,
            Vy = s * speed + (from is null ? tank.Vy : from.Vy) * 0.2,
            Angle = angle,
            Radius = Math.Max(3, (def.Width * scale / 2) * bullet.SizeRatio),
            Mass = 0.22,
            Damage = (7 + tank.Stats[TankStats.BulletDamage] * 3) * bullet.Damage,
            Health = (1.5 * tank.Stats[TankStats.Pen] + 2) * bullet.Health,
            Life = life,
            Accel = accel,
            Absorption = bullet.Absorption,
            PushFactor = ((7.0 / 3) + tank.Stats[TankStats.BulletDamage]) * bullet.Damage * bullet.Absorption,
            Kind = bullet.Type,
            BarrelIndex = index,
            OwnerId = tank.Id,
            Fill = bullet.NeutralColor ? DiepColors.Neutral : tank.Fill
        };
        Projectile.Configure(shot, tank, def, scale);
        shot.MovementAngle = angle;
        if (tank.IsBoss && Projectile.IsDroneLike(shot.Kind))
        {
            shot.Accel *= 0.38;
            shot.Vx *= 0.38;
            shot.Vy *= 0.38;
        }
        if (tank.IsArenaCloser)
        {
            shot.Damage *= 4;
            shot.Health *= 3;
            shot.Radius = Math.Max(shot.Radius, 22);
            shot.Life = Math.Max(shot.Life, 90);
        }
        shot.Snap();
        Bullets.Add(shot);
        Mods?.EmitBullet("bullet_spawn", shot);
        if (Mods is not null)
        {
            var script = Mods.Mods.FirstOrDefault(m => m.Enabled)?.Script;
            if (script is not null)
                Mods.Emit("post_shoot", EntityProxies.Tank(script, tank, this), EntityProxies.Bullet(script, shot, this));
        }

        barrel.ShotAge = 0;
        var kick = def.Recoil * 2 * scale;
        if (from is not null)
            DiepPhysics.AddVelocity(ref from.Vx, ref from.Vy, angle + Math.PI, kick);
        else if (recoilTank)
            DiepPhysics.AddVelocity(ref tank.Vx, ref tank.Vy, angle + Math.PI, kick);
    }

    private void ApplyPendingClass()
    {
        if (_pendingClassName is null)
            return;
        Debug.Flash(_pendingClassName);
        _pendingClassName = null;
    }

    private static void Regen(TankEntity tank, double dt)
    {
        tank.CombatTimer = Math.Max(0, tank.CombatTimer - dt);
        var rate = 1.2 + tank.Stats[TankStats.Regen] * 1.6;
        if (tank.CombatTimer <= 0)
            rate *= 3.2;
        tank.Health = Math.Min(tank.MaxHealth, tank.Health + rate * dt);
    }

    private void ResolveCollisions(double dt) => _collisions.Resolve(this, dt);

    internal bool IsGrabbed(PhysKind kind, int index) =>
        Cursor.Grabbing && Cursor.GrabKind == kind && Cursor.GrabIndex == index;

    public void SetPointer(double x, double y, bool down) => Cursor.Feed(x, y, down);

    internal TankEntity? FindTank(int id)
    {
        foreach (var tank in Tanks)
        {
            if (tank.Id == id)
                return tank;
        }
        return null;
    }

    private void TickPointer()
    {
        Cursor.Hovering = HitTest(Cursor.X, Cursor.Y, out _, out _);
        if (Cursor.Down && !Cursor.Grabbing && Cursor.Hovering)
            TryGrab();
        if (!Cursor.Down && Cursor.Grabbing)
            Cursor.Release();
    }

    private void TryGrab()
    {
        if (!HitTest(Cursor.X, Cursor.Y, out var kind, out var index))
            return;
        Cursor.Grabbing = true;
        Cursor.GrabKind = kind;
        Cursor.GrabIndex = index;
        GetBodyPos(kind, index, out var x, out var y);
        Cursor.GrabOffX = x - Cursor.X;
        Cursor.GrabOffY = y - Cursor.Y;
        if (kind == PhysKind.Tank && index >= 0 && index < Tanks.Count)
            Selected = Tanks[index];
    }

    private void ApplyGrab()
    {
        if (!Cursor.Grabbing)
            return;
        var x = Cursor.X + Cursor.GrabOffX;
        var y = Cursor.Y + Cursor.GrabOffY;
        switch (Cursor.GrabKind)
        {
            case PhysKind.Tank:
                if (Cursor.GrabIndex < 0 || Cursor.GrabIndex >= Tanks.Count || !Tanks[Cursor.GrabIndex].Alive)
                {
                    Cursor.Release();
                    return;
                }
                var tank = Tanks[Cursor.GrabIndex];
                tank.X = x;
                tank.Y = y;
                tank.Vx = Cursor.Vx;
                tank.Vy = Cursor.Vy;
                break;
            case PhysKind.Shape:
                if (Cursor.GrabIndex < 0 || Cursor.GrabIndex >= Shapes.Count) { Cursor.Release(); return; }
                var s = Shapes[Cursor.GrabIndex];
                s.X = x;
                s.Y = y;
                s.Vx = Cursor.Vx;
                s.Vy = Cursor.Vy;
                break;
            case PhysKind.Bullet:
                if (Cursor.GrabIndex < 0 || Cursor.GrabIndex >= Bullets.Count) { Cursor.Release(); return; }
                var b = Bullets[Cursor.GrabIndex];
                b.X = x;
                b.Y = y;
                b.Vx = Cursor.Vx;
                b.Vy = Cursor.Vy;
                break;
            default:
                Cursor.Release();
                break;
        }
    }

    private bool HitTest(double x, double y, out PhysKind kind, out int index)
    {
        kind = PhysKind.Cursor;
        index = -1;
        var best = Cursor.Radius * Cursor.Radius;
        var found = false;
        for (var i = 0; i < Tanks.Count; i++)
        {
            var tank = Tanks[i];
            if (!tank.Alive || tank.Destroy.Active || tank.IsArenaCloser) continue;
            var d = Dist2(x, y, tank.X, tank.Y);
            var r = tank.Radius + 6;
            if (d <= r * r && d <= best) { best = d; kind = PhysKind.Tank; index = i; found = true; }
        }
        for (var i = 0; i < Shapes.Count; i++)
        {
            var s = Shapes[i];
            if (s.Destroy.Active) continue;
            var d = Dist2(x, y, s.X, s.Y);
            var r = s.Radius + 6;
            if (d <= r * r && d <= best) { best = d; kind = PhysKind.Shape; index = i; found = true; }
        }
        for (var i = 0; i < Bullets.Count; i++)
        {
            var b = Bullets[i];
            if (b.Destroy.Active) continue;
            var d = Dist2(x, y, b.X, b.Y);
            var r = b.Radius + 8;
            if (d <= r * r && d <= best) { best = d; kind = PhysKind.Bullet; index = i; found = true; }
        }
        return found;
    }

    private void GetBodyPos(PhysKind kind, int index, out double x, out double y)
    {
        switch (kind)
        {
            case PhysKind.Tank:
                x = Tanks[index].X;
                y = Tanks[index].Y;
                return;
            case PhysKind.Shape:
                x = Shapes[index].X;
                y = Shapes[index].Y;
                return;
            case PhysKind.Bullet:
                x = Bullets[index].X;
                y = Bullets[index].Y;
                return;
            default:
                x = Cursor.X;
                y = Cursor.Y;
                return;
        }
    }

    private static double Dist2(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    private void ApplyAllPhysics()
    {
        for (var i = 0; i < Tanks.Count; i++)
        {
            var tank = Tanks[i];
            if (!(tank.Alive || tank.Destroy.Active) || IsGrabbed(PhysKind.Tank, i))
                continue;
            if (tank.IsArenaCloser)
                DiepPhysics.ApplyPhysics(ref tank.X, ref tank.Y, ref tank.Vx, ref tank.Vy, tank.Destroy.Active, tank.Radius, Width, Height, clamp: false);
            else
                DiepPhysics.ApplyPhysics(ref tank.X, ref tank.Y, ref tank.Vx, ref tank.Vy, tank.Destroy.Active, tank.Radius, Width, Height);
        }
        for (var i = 0; i < Shapes.Count; i++)
        {
            var s = Shapes[i];
            if (IsGrabbed(PhysKind.Shape, i))
                continue;
            DiepPhysics.ApplyPhysics(ref s.X, ref s.Y, ref s.Vx, ref s.Vy, s.Destroy.Active, s.Radius, Width, Height);
        }
        for (var i = 0; i < Bullets.Count; i++)
        {
            var b = Bullets[i];
            if (IsGrabbed(PhysKind.Bullet, i))
                continue;
            if (Projectile.IsDroneLike(b.Kind))
            {
                DiepPhysics.ApplyDronePhysics(ref b.X, ref b.Y, ref b.Vx, ref b.Vy, b.Destroy.Active, b.Radius, Width, Height, Projectile.StaysInArena(b.Kind));
                continue;
            }
            b.X += b.Vx;
            b.Y += b.Vy;
            if (b.Kind == ProjectileKind.Trap)
            {
                var mag = Math.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy);
                if (mag > 0.01)
                    DiepPhysics.AddVelocity(ref b.Vx, ref b.Vy, Math.Atan2(b.Vy, b.Vx), mag * -0.1);
            }
            if (b.Kind == ProjectileKind.Trap)
                Math2.Bounce(ref b.X, ref b.Y, ref b.Vx, ref b.Vy, b.Radius, Width, Height);
        }
    }

    private void ApplyWindowCollisions()
    {
        var boxes = Windows.Boxes;
        if (boxes.Count == 0)
            return;

        for (var i = 0; i < Tanks.Count; i++)
        {
            var tank = Tanks[i];
            if (!(tank.Alive || tank.Destroy.Active) || tank.IsArenaCloser || IsGrabbed(PhysKind.Tank, i))
                continue;
            BounceWindows(ref tank.X, ref tank.Y, ref tank.Vx, ref tank.Vy, tank.Radius, boxes);
        }
        for (var i = 0; i < Shapes.Count; i++)
        {
            var s = Shapes[i];
            if (IsGrabbed(PhysKind.Shape, i))
                continue;
            BounceWindows(ref s.X, ref s.Y, ref s.Vx, ref s.Vy, s.Radius, boxes);
        }
        for (var i = 0; i < Bullets.Count; i++)
        {
            var b = Bullets[i];
            if (IsGrabbed(PhysKind.Bullet, i) || b.Destroy.Active)
                continue;
            if (Projectile.IsDroneLike(b.Kind) || b.Kind == ProjectileKind.Trap)
            {
                BounceWindows(ref b.X, ref b.Y, ref b.Vx, ref b.Vy, b.Radius, boxes);
                continue;
            }
            foreach (var box in boxes)
            {
                if (!Math2.CircleHitsAabb(b.X, b.Y, b.Radius, box))
                    continue;
                b.Health = 0;
                b.Life = 0;
                break;
            }
        }
    }

    private static void BounceWindows(ref double x, ref double y, ref double vx, ref double vy, double radius, IReadOnlyList<WindowBox> boxes)
    {
        foreach (var box in boxes)
            Math2.BounceCircleAabb(ref x, ref y, ref vx, ref vy, radius, box);
    }

    private void PruneDead()
    {
        for (var i = Shapes.Count - 1; i >= 0; i--)
        {
            var s = Shapes[i];
            if (s.Destroy.Finished || (s.Health <= 0 && !s.Destroy.Active))
            {
                if (IsGrabbed(PhysKind.Shape, i))
                    Cursor.Release();
                else if (Cursor.Grabbing && Cursor.GrabKind == PhysKind.Shape && i < Cursor.GrabIndex)
                    Cursor.GrabIndex--;
                Shapes.RemoveAt(i);
            }
        }

        for (var i = Bullets.Count - 1; i >= 0; i--)
        {
            var b = Bullets[i];
            if (b.Destroy.Finished)
            {
                Bullets.RemoveAt(i);
                continue;
            }
            if (b.Destroy.Active)
                continue;
            if (!Projectile.StaysInArena(b.Kind) &&
                (b.X < -40 || b.Y < -40 || b.X > Width + 40 || b.Y > Height + 40))
            {
                Bullets.RemoveAt(i);
                continue;
            }
            if ((b.Life <= 0 || b.Health <= 0 || b.Opacity <= 0) && (b.NoDestroyAnim || !b.Destroy.Begin()))
                Bullets.RemoveAt(i);
        }
    }

    internal void HurtTank(TankEntity tank, double amount, TankEntity? killer)
    {
        if (!tank.Alive || amount <= 0 || tank.IsArenaCloser)
            return;
        amount = Mods?.EmitDamage("tank_hurt", amount, tank, killer) ?? amount;
        if (amount <= 0)
            return;
        tank.Health -= amount;
        tank.Flash.Hit();
        tank.CombatTimer = 3;
        if (tank.Health > 0)
            return;
        tank.Health = 0;
        tank.Alive = false;
        ClearOwnedBullets(tank.Id);
        tank.Snap();
        if (!tank.Destroy.Begin())
            tank.Respawn = tank.IsBoss ? 1e9 : 2.2;
        if (killer is not null && killer.Id != tank.Id)
            AddScore(killer, tank.IsBoss ? tank.BossXp : Math.Max(20, tank.Level * 8));
        if (tank.IsBoss)
        {
            var name = tank.BossAltName ?? tank.Class.Name;
            var killerName = killer is null ? "an unnamed tank"
                : killer.IsBoss ? (killer.BossAltName ?? killer.Class.Name) : killer.Class.Name;
            Notifications.Server($"The {name} has been defeated by {killerName}!", 10, "boss_death");
        }
        Debug.Flash(tank.IsBoss ? "Boss died" : "Tank died");
        Mods?.EmitTank("tank_death", tank);
        if (killer is not null)
            Mods?.EmitTank("kill", killer);
    }

    private void ClearOwnedBullets(int ownerId)
    {
        foreach (var b in Bullets)
        {
            if (b.OwnerId != ownerId)
                continue;
            b.Health = 0;
            b.Life = 0;
        }
    }

    private void RespawnTank(TankEntity tank)
    {
        tank.Alive = true;
        tank.Destroy.Reset();
        tank.Flash.Reset();
        TankStats.Recalc(tank);
        tank.Health = tank.MaxHealth;
        tank.X = 80 + _rng.NextDouble() * Math.Max(80, Width - 160);
        tank.Y = 80 + _rng.NextDouble() * Math.Max(80, Height - 160);
        tank.Vx = 0;
        tank.Vy = 0;
        tank.Snap();
        Mods?.EmitTank("tank_respawn", tank);
    }

    internal void KillShape(ShapeEntity s, TankEntity? killer)
    {
        if (s.Destroy.Active)
            return;
        if (killer is not null && s.Kind == ShapeKind.Square && TryNecroClaim(killer, s))
            return;
        if (killer is not null)
            AddScore(killer, s.Xp);
        if (!s.Destroy.Begin())
            s.Health = 0;
        Mods?.EmitShape("shape_death", s);
    }

    private bool TryNecroClaim(TankEntity tank, ShapeEntity shape)
    {
        if (tank.ClassId != TankId.Necromancer)
            return false;
        BarrelState? barrel = null;
        var barrelIndex = -1;
        for (var i = 0; i < tank.Barrels.Length; i++)
        {
            var def = tank.Barrels[i].Def;
            if (def.Bullet.Type != ProjectileKind.Necrodrone)
                continue;
            if (CountDrones(tank, i, def) >= MaxDrones(tank, def))
                continue;
            barrel = tank.Barrels[i];
            barrelIndex = i;
            break;
        }
        if (barrel is null)
            return false;

        AddScore(tank, shape.Xp);
        var shot = new BulletEntity
        {
            X = shape.X,
            Y = shape.Y,
            Angle = shape.Angle,
            Radius = shape.Radius,
            Mass = 0.4,
            Damage = (7 + tank.Stats[TankStats.BulletDamage] * 3) * barrel.Def.Bullet.Damage * 0.5,
            Health = (1.5 * tank.Stats[TankStats.Pen] + 2) * barrel.Def.Bullet.Health,
            Life = 1e9,
            Accel = (20 + tank.Stats[TankStats.BulletSpeed] * 3) * barrel.Def.Bullet.Speed * TankStats.GunScale(tank),
            Absorption = barrel.Def.Bullet.Absorption,
            PushFactor = 4,
            Kind = ProjectileKind.Necrodrone,
            BarrelIndex = barrelIndex,
            OwnerId = tank.Id,
            Fill = DiepColors.NecroSquare,
            Sides = 4,
            CanControl = true,
            MovementAngle = shape.Angle
        };
        shot.Snap();
        Bullets.Add(shot);
        shape.Health = 0;
        return true;
    }

    internal void ModAddScore(TankEntity tank, int xp) => AddScore(tank, xp);

    private void AddScore(TankEntity tank, int xp)
    {
        if (Mods is not null)
        {
            var script = Mods.Mods.FirstOrDefault(m => m.Enabled)?.Script;
            if (script is not null)
            {
                var e = new MoonSharp.Interpreter.Table(script)
                {
                    ["cancel"] = false,
                    ["xp"] = xp
                };
                Mods.Events.EmitCancelable(script, "xp_gain", e, EntityProxies.Tank(script, tank, this));
                if (e.Get("cancel").Type == MoonSharp.Interpreter.DataType.Boolean && e.Get("cancel").Boolean)
                    return;
                if (e.Get("xp").Type == MoonSharp.Interpreter.DataType.Number)
                    xp = (int)e.Get("xp").Number;
            }
        }
        if (xp <= 0)
            return;
        tank.Score += xp;
        tank.XpIntoLevel += xp;
        while (tank.XpIntoLevel >= tank.XpForNext && tank.Level < 45)
        {
            tank.XpIntoLevel -= tank.XpForNext;
            tank.Level++;
            tank.SkillPoints++;
            tank.XpForNext = TankStats.XpNeeded(tank.Level);
            TankStats.Recalc(tank);
            tank.Health = tank.MaxHealth;
            Mods?.EmitTank("level_up", tank);
            var prevClass = tank.ClassId;
            if (TankClasses.TryUpgrade(tank, _rng))
            {
                _pendingClassName = tank.Class.Name;
                Mods?.EmitTank("class_upgrade", tank);
            }
            else
                Debug.Flash($"Level {tank.Level}");
        }
    }
}
