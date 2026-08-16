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
    public double DrawAlpha => Interpolate ? Alpha : 1;

    public DebugState Debug { get; } = new();
    public CursorState Cursor { get; } = new();

    private readonly Random _rng = new();
    private readonly ShapeSpawner _spawner;
    private readonly CollisionSystem _collisions = new();
    private double _accumulator;
    private int _nextTankId = 1;
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
        _nextTankId = 1;
        Selected = SpawnTank(startX, startY);
        _spawner.Fill(Shapes, Width, Height, 18);
        Debug.Flash("Reset");
    }

    public TankEntity? SpawnTank(double? x = null, double? y = null)
    {
        if (Tanks.Count >= MaxTanks)
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
        TankClasses.Set(tank, TankId.Basic);
        TankStats.Recalc(tank);
        tank.Health = tank.MaxHealth;
        tank.Snap();
        Tanks.Add(tank);
        Selected = tank;
        Debug.Flash($"{tank.Class.Name}");
        return tank;
    }

    public void SelectTank(int index)
    {
        if (index < 0 || index >= Tanks.Count)
            return;
        Selected = Tanks[index];
        Debug.Flash(Selected.Class.Name);
    }

    public void SetSelectedStat(int stat, int value)
    {
        if (Selected is null)
            return;
        if (TankStats.SetLevel(Selected, stat, value))
            Debug.Flash($"{TankStats.Names[stat]} {Selected.Stats[stat]}");
    }

    public void SetSelectedClass(TankId id)
    {
        if (Selected is null)
            return;
        TankClasses.Set(Selected, id);
        TankStats.Recalc(Selected);
        Debug.Flash(Selected.Class.Name);
    }

    public void RemoveSelected()
    {
        if (Selected is null || Tanks.Count <= 1)
            return;
        var i = Tanks.IndexOf(Selected);
        if (i < 0)
            return;
        if (IsGrabbed(PhysKind.Tank, i))
            Cursor.Release();
        ClearOwnedBullets(Selected.Id);
        Tanks.RemoveAt(i);
        Selected = Tanks[Math.Clamp(i, 0, Tanks.Count - 1)];
        Debug.Flash("Removed");
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
        Cursor.BeginTick();
        TickPointer();
        _spawner.Maintain(Shapes, Width, Height, dt);

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
        Cursor.EndTick();
        PruneDead();
        Debug.HashCells = _collisions.CellCount;
        Debug.HashPairs = _collisions.PairCount;
    }

    private void TickTank(TankEntity tank, int index, double dt)
    {
        tank.Flash.Tick();
        if (!tank.Alive)
        {
            if (tank.Destroy.Active)
            {
                tank.Destroy.Tick();
                if (tank.Destroy.Finished)
                {
                    tank.Destroy.Reset();
                    tank.Respawn = 2.2;
                }
            }
            else
            {
                tank.Respawn -= dt;
                if (tank.Respawn <= 0)
                    RespawnTank(tank);
            }
            return;
        }

        if (IsGrabbed(PhysKind.Tank, index))
            return;

        tank.Brain.SpendPoints(tank);
        tank.Brain.Think(tank, Tanks, Shapes, out var ax, out var ay);
        DiepPhysics.MaintainVelocity(ref tank.Vx, ref tank.Vy, Math.Atan2(ay, ax),
            (ax == 0 && ay == 0) ? 0 : TankStats.MoveSpeed(tank));
        TickBarrels(tank);
        Regen(tank, dt);
    }

    private void TickBarrels(TankEntity tank)
    {
        tank.RotatorAngle += 0.1;
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
            else if (turret.Orbit > 0)
            {
                turret.Angle = turret.MountAngle + tank.RotatorAngle;
            }
            else
            {
                turret.Angle += 0.1;
            }

            TickShootCycle(tank, 100 + i, turret.Barrel, wants, turret.Angle, x, y, recoil: false);
        }
    }

    private static void TurretPose(TankEntity tank, AutoTurretState turret, out double x, out double y)
    {
        var a = turret.MountAngle + tank.RotatorAngle;
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
        var scale = (from?.Radius ?? tank.Radius) / 50.0;
        var scatter = (Math.PI / 180.0) * bullet.ScatterRate * (_rng.NextDouble() - 0.5) * 10;
        var angle = facing + def.Angle + scatter;
        var size = def.Size * scale;
        var offset = def.Offset * scale;
        var dist = def.Distance * scale;
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
            Fill = tank.Fill
        };
        Projectile.Configure(shot, tank, def, scale);
        shot.MovementAngle = angle;
        shot.Snap();
        Bullets.Add(shot);

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
            if (!tank.Alive || tank.Destroy.Active) continue;
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
        if (!tank.Alive || amount <= 0)
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
            tank.Respawn = 2.2;
        if (killer is not null && killer.Id != tank.Id)
            AddScore(killer, Math.Max(20, tank.Level * 8));
        Debug.Flash("Tank died");
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
            Accel = (20 + tank.Stats[TankStats.BulletSpeed] * 3) * barrel.Def.Bullet.Speed * (tank.Radius / 50.0),
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

    private void AddScore(TankEntity tank, int xp)
    {
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
            if (TankClasses.TryUpgrade(tank, _rng))
                _pendingClassName = tank.Class.Name;
            else
                Debug.Flash($"Level {tank.Level}");
        }
    }
}
