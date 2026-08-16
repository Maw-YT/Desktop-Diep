namespace DesktopDiep;

public sealed class PetBrain
{
    private static readonly int[] RamUpgradeOrder =
    [
        TankStats.Body,
        TankStats.MaxHealth,
        TankStats.Move,
        TankStats.Regen,
        TankStats.Reload,
        TankStats.BulletDamage,
        TankStats.BulletSpeed,
        TankStats.Pen
    ];

    private static readonly int[] UpgradeOrder =
    [
        TankStats.Reload,
        TankStats.BulletDamage,
        TankStats.BulletSpeed,
        TankStats.Pen,
        TankStats.Move,
        TankStats.Regen,
        TankStats.MaxHealth,
        TankStats.Body
    ];

    private readonly Random _rng = Random.Shared;
    private int _lockId = -1;
    private int _lockLife;
    private ShapeEntity? _lockShape;
    private int _burst;
    private int _pause;
    private int _hesitate;
    private int _spendWait;
    private int _orbitDir;
    private int _repathIn;
    private int _stuck;
    private int _unstick;
    private int _roamLife;
    private double _aim;
    private double _wobble;
    private double _roamX;
    private double _roamY;
    private bool _inited;
    private bool _fleeLatch;
    private bool _hasRoam;

    public ShapeEntity? ShapeTarget { get; private set; }
    public TankEntity? Enemy { get; private set; }
    public bool Fleeing { get; private set; }
    public bool WantsShot { get; set; }
    public double AimX { get; set; }
    public double AimY { get; set; }
    public int BossCorner = -1;
    public bool HasTarget => Enemy is not null || ShapeTarget is not null;
    public readonly List<(double X, double Y)> Path = [];

    public void SetBossAim(double x, double y, TankEntity? foe)
    {
        AimX = x;
        AimY = y;
        Enemy = foe;
        ShapeTarget = null;
    }

    public void ClearBossAim()
    {
        Enemy = null;
        ShapeTarget = null;
        WantsShot = false;
    }

    public void Think(TankEntity tank, GameWorld world, out double ax, out double ay)
    {
        ax = 0;
        ay = 0;
        WantsShot = false;
        if (!_inited)
        {
            _orbitDir = _rng.Next(2) == 0 ? 1 : -1;
            _aim = tank.Angle;
            _inited = true;
        }

        var ram = TankStats.IsRam(tank);
        if (!_fleeLatch && !ram && tank.Health < tank.MaxHealth * 0.2)
            _fleeLatch = true;
        else if (_fleeLatch && tank.Health > tank.MaxHealth * 0.42)
            _fleeLatch = false;
        Fleeing = _fleeLatch;

        var prevEnemy = Enemy;
        var prevShape = ShapeTarget;
        PickTargets(tank, world);
        if (Enemy != prevEnemy || ShapeTarget != prevShape)
            _repathIn = 0;

        double tx, ty, tr, tvx, tvy;
        if (Enemy is { } foe)
        {
            tx = foe.X - tank.X;
            ty = foe.Y - tank.Y;
            tr = foe.Radius;
            tvx = foe.Vx;
            tvy = foe.Vy;
            AimX = foe.X;
            AimY = foe.Y;
        }
        else if (ShapeTarget is { } shape)
        {
            tx = shape.X - tank.X;
            ty = shape.Y - tank.Y;
            tr = shape.Radius;
            tvx = shape.Vx;
            tvy = shape.Vy;
            AimX = shape.X;
            AimY = shape.Y;
        }
        else
        {
            if (world.CollideWindows && (world.Nav.Occupied(tank.X, tank.Y) || _unstick > 0 || _stuck > 7))
                Unstick(tank, world, out ax, out ay);
            else
                Roam(tank, world, out ax, out ay);
            return;
        }

        var dist = Math.Sqrt(tx * tx + ty * ty);
        var hasLos = CanSee(world, tank.X, tank.Y, Enemy?.X ?? ShapeTarget!.X, Enemy?.Y ?? ShapeTarget!.Y);
        if (hasLos)
            Lead(tank, tvx, tvy, dist);
        tx = AimX - tank.X;
        ty = AimY - tank.Y;
        dist = Math.Sqrt(tx * tx + ty * ty);
        var look = Math.Atan2(ty, tx);
        _wobble += (_rng.NextDouble() - 0.5) * 0.012;
        _wobble *= 0.88;
        var wantAim = hasLos ? look + _wobble * 0.12 : _aim;
        var turn = Math2.NormalizeAngle(wantAim - _aim);
        // Track hard when we can see the target so orbiting doesn't leave the barrel behind.
        _aim += turn * (hasLos ? 0.55 : 0.14);
        tank.Angle = _aim;

        // Stand farther from bigger targets (radius-scaled orbit / engagement).
        var preferred = (ram ? 8 : Enemy is null ? 75 : 110) + tank.Radius + tr * 2.6;
        var range = Math.Max(Enemy is null ? 260.0 : 340.0, preferred + 90);
        var canShoot = hasLos && CanShoot(world, tank.X, tank.Y, AimX, AimY);

        // Always hold a ring around the target — never path into its center (that was
        // why pets walked into Alpha Pentagons when they lacked a clear orbit shot).
        RingGoal(tank.X, tank.Y, AimX, AimY, preferred, 0, out var gx, out var gy);
        if (Fleeing)
        {
            gx = tank.X - Math.Cos(look) * Math.Max(220, preferred);
            gy = tank.Y - Math.Sin(look) * Math.Max(220, preferred);
        }
        else if (!ram && hasLos)
        {
            RingGoal(tank.X, tank.Y, AimX, AimY, preferred, _orbitDir * Math.PI / 2, out var ox, out var oy);
            if (CanShoot(world, ox, oy, AimX, AimY))
            {
                gx = ox;
                gy = oy;
            }
            else if (dist < preferred * 0.92)
            {
                // Too close and can't use the side orbit — back straight out.
                RingGoal(tank.X, tank.Y, AimX, AimY, preferred, 0, out gx, out gy);
            }
            else if (dist <= preferred * 1.08)
            {
                gx = tank.X;
                gy = tank.Y;
            }
        }
        else if (!ram && world.CollideWindows
            && world.Nav.TryFiringPoint(tank.X, tank.Y, AimX, AimY, preferred, range, world.Windows.Boxes, out var fx, out var fy))
        {
            gx = fx;
            gy = fy;
        }

        gx = Math.Clamp(gx, 80, Math.Max(80, world.Width - 80));
        gy = Math.Clamp(gy, 80, Math.Max(80, world.Height - 80));
        if (world.CollideWindows)
            world.Nav.SnapGoal(ref gx, ref gy);

        if (_unstick > 0 || _stuck > 7)
        {
            _hesitate = 0;
            Unstick(tank, world, out ax, out ay);
        }
        else if (_hesitate > 0)
            _hesitate--;
        else if (_rng.NextDouble() < 0.008)
        {
            _hesitate = 2 + _rng.Next(5);
            if (_rng.NextDouble() < 0.3)
                _orbitDir = -_orbitDir;
        }
        else
            Follow(tank, world, gx, gy, out ax, out ay);

        if (!hasLos)
        {
            look = Math.Atan2(gy - tank.Y, gx - tank.X);
            _aim += Math2.NormalizeAngle(look - _aim) * 0.16;
            tank.Angle = _aim;
        }

        var aligned = Math.Abs(Math2.NormalizeAngle(look - _aim)) < 0.16;
        TickBurst(aligned && canShoot && dist <= range && !Fleeing);
        WantsShot = _burst > 0 && aligned && canShoot;
        if (ram)
            WantsShot = true;
    }

    public void SpendPoints(TankEntity tank)
    {
        if (tank.ManualStats || tank.SkillPoints <= 0)
            return;
        if (_spendWait > 0)
        {
            _spendWait--;
            return;
        }
        _spendWait = 6 + _rng.Next(10);
        foreach (var stat in TankStats.IsRam(tank) ? RamUpgradeOrder : UpgradeOrder)
        {
            if (TankStats.TryUpgrade(tank, stat))
                return;
        }
    }

    private void Follow(TankEntity tank, GameWorld world, double gx, double gy, out double ax, out double ay)
    {
        ax = gx - tank.X;
        ay = gy - tank.Y;
        if (!world.CollideWindows)
        {
            Path.Clear();
            Path.Add((tank.X, tank.Y));
            Path.Add((gx, gy));
            return;
        }

        if (_repathIn <= 0 || Path.Count < 2)
        {
            _repathIn = 3;
            var ok = world.Nav.TrySteer(tank.X, tank.Y, gx, gy, Path, out ax, out ay);
            if (!ok || (Math.Abs(ax) < 0.01 && Math.Abs(ay) < 0.01))
            {
                ax = gx - tank.X;
                ay = gy - tank.Y;
                if (Path.Count < 2)
                {
                    Path.Clear();
                    Path.Add((tank.X, tank.Y));
                    Path.Add((gx, gy));
                }
            }
            NoteProgress(tank, world, ok, ax, ay);
            return;
        }

        _repathIn--;
        Carrot(tank.X, tank.Y, out var cx, out var cy);
        ax = cx - tank.X;
        ay = cy - tank.Y;
        NoteProgress(tank, world, Path.Count >= 2, ax, ay);
    }

    private void NoteProgress(TankEntity tank, GameWorld world, bool ok, double ax, double ay)
    {
        var speed = Math.Sqrt(tank.Vx * tank.Vx + tank.Vy * tank.Vy);
        var want = Math.Abs(ax) > 0.4 || Math.Abs(ay) > 0.4;
        if (world.CollideWindows && world.Nav.Occupied(tank.X, tank.Y))
            _stuck += 2;
        else if (!ok || (want && speed < 0.45))
            _stuck++;
        else
            _stuck = Math.Max(0, _stuck - 2);
    }

    private void Unstick(TankEntity tank, GameWorld world, out double ax, out double ay)
    {
        if (_unstick <= 0)
            _unstick = 16;
        _unstick--;
        _stuck = 0;
        _repathIn = 0;
        if (world.CollideWindows && world.Nav.TryOpenSpace(tank.X, tank.Y, Path, out ax, out ay))
            return;

        double rx = 0, ry = 0;
        foreach (var box in world.Windows.Boxes)
        {
            var cx = Math.Clamp(tank.X, box.Left, box.Right);
            var cy = Math.Clamp(tank.Y, box.Top, box.Bottom);
            var dx = tank.X - cx;
            var dy = tank.Y - cy;
            if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01)
            {
                var left = tank.X - box.Left;
                var right = box.Right - tank.X;
                var top = tank.Y - box.Top;
                var bot = box.Bottom - tank.Y;
                var m = Math.Min(Math.Min(left, right), Math.Min(top, bot));
                if (m == left) dx = -1;
                else if (m == right) dx = 1;
                else if (m == top) dy = -1;
                else dy = 1;
            }
            var d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 1)
                continue;
            var w = (220 - Math.Min(220, d)) / d;
            rx += dx * w;
            ry += dy * w;
        }
        if (Math.Abs(rx) < 0.01 && Math.Abs(ry) < 0.01)
        {
            rx = Math.Cos(_aim + Math.PI);
            ry = Math.Sin(_aim + Math.PI);
        }
        var mag = Math.Sqrt(rx * rx + ry * ry);
        ax = rx / mag;
        ay = ry / mag;
        Path.Clear();
        Path.Add((tank.X, tank.Y));
        Path.Add((tank.X + ax * 140, tank.Y + ay * 140));
    }

    private static void RingGoal(double px, double py, double tx, double ty, double preferred, double angleOffset,
        out double gx, out double gy)
    {
        var dx = px - tx;
        var dy = py - ty;
        var d = Math.Sqrt(dx * dx + dy * dy);
        var a = (d < 1 ? 0 : Math.Atan2(dy, dx)) + angleOffset;
        gx = tx + Math.Cos(a) * preferred;
        gy = ty + Math.Sin(a) * preferred;
    }

    private void Lead(TankEntity tank, double tvx, double tvy, double dist)
    {
        var ox = AimX;
        var oy = AimY;
        var speed = ShotSpeed(tank);
        if (speed < 4)
            return;
        // Bullets inherit 20% of owner velocity — cancel that so strafing doesn't shove shots sideways.
        const double inherit = 0.2;
        var t = dist / Math.Max(4, speed);
        for (var i = 0; i < 4; i++)
        {
            var dx = ox + tvx * t - tank.X - tank.Vx * inherit * t;
            var dy = oy + tvy * t - tank.Y - tank.Vy * inherit * t;
            var next = Math.Sqrt(dx * dx + dy * dy) / speed;
            if (double.IsNaN(next) || next < 0.01)
                break;
            t = next;
        }
        AimX = ox + tvx * t - tank.Vx * inherit * t;
        AimY = oy + tvy * t - tank.Vy * inherit * t;
    }

    private static double ShotSpeed(TankEntity tank)
    {
        var mul = tank.Barrels.Length > 0 ? tank.Barrels[0].Def.Bullet.Speed : 1;
        var scale = Math.Max(0.45, tank.Radius / 50.0);
        return (20 + tank.Stats[TankStats.BulletSpeed] * 3) * mul * scale + 30 * scale;
    }

    private void Carrot(double x, double y, out double cx, out double cy)
    {
        cx = Path[^1].X;
        cy = Path[^1].Y;
        const double reach = 48;
        for (var i = 0; i < Path.Count; i++)
        {
            var p = Path[i];
            var dx = p.X - x;
            var dy = p.Y - y;
            if (dx * dx + dy * dy >= reach * reach)
            {
                cx = p.X;
                cy = p.Y;
                return;
            }
        }
    }

    private void PickTargets(TankEntity tank, GameWorld world)
    {
        if (_lockLife > 0)
            _lockLife--;
        Enemy = null;
        ShapeTarget = null;

        if (_lockLife > 0 && _lockId >= 0)
        {
            foreach (var other in world.Tanks)
            {
                if (other.Id != _lockId || !other.Alive || other.Destroy.Active)
                    continue;
                if (!CanSee(world, tank.X, tank.Y, other.X, other.Y))
                    break;
                Enemy = other;
                return;
            }
            _lockLife = 0;
            _lockId = -1;
        }

        if (_lockLife > 0 && _lockShape is { } held && !held.Destroy.Active
            && CanSee(world, tank.X, tank.Y, held.X, held.Y))
        {
            ShapeTarget = held;
            return;
        }

        _lockShape = null;
        Enemy = PickEnemy(tank, world);
        if (Enemy is not null)
        {
            _lockId = Enemy.Id;
            _lockLife = 45 + _rng.Next(40);
            _hasRoam = false;
            return;
        }

        ShapeTarget = PickShape(tank, world);
        if (ShapeTarget is not null)
        {
            _lockShape = ShapeTarget;
            _lockLife = 40 + _rng.Next(50);
            _hasRoam = false;
        }
    }

    private void Roam(TankEntity tank, GameWorld world, out double ax, out double ay)
    {
        if (_roamLife > 0)
            _roamLife--;
        var dx = _roamX - tank.X;
        var dy = _roamY - tank.Y;
        var arrived = !_hasRoam || dx * dx + dy * dy < 70 * 70;
        if (!_hasRoam || arrived || _roamLife <= 0)
            PickRoamGoal(tank, world);

        if (_hesitate > 0)
        {
            _hesitate--;
            ax = 0;
            ay = 0;
            _aim += 0.01 * _orbitDir;
            tank.Angle = _aim;
            return;
        }
        if (_rng.NextDouble() < 0.01)
            _hesitate = 4 + _rng.Next(10);

        var look = Math.Atan2(_roamY - tank.Y, _roamX - tank.X);
        _aim += Math2.NormalizeAngle(look - _aim) * 0.14;
        tank.Angle = _aim;
        Follow(tank, world, _roamX, _roamY, out ax, out ay);
    }

    private void PickRoamGoal(TankEntity tank, GameWorld world)
    {
        var pad = 100.0;
        var w = Math.Max(pad * 2, world.Width - pad * 2);
        var h = Math.Max(pad * 2, world.Height - pad * 2);
        for (var i = 0; i < 8; i++)
        {
            var gx = pad + _rng.NextDouble() * w;
            var gy = pad + _rng.NextDouble() * h;
            var dx = gx - tank.X;
            var dy = gy - tank.Y;
            if (dx * dx + dy * dy < 220 * 220)
                continue;
            if (world.CollideWindows)
                world.Nav.SnapGoal(ref gx, ref gy);
            _roamX = gx;
            _roamY = gy;
            _roamLife = 70 + _rng.Next(90);
            _hasRoam = true;
            _repathIn = 0;
            return;
        }
        _roamX = pad + _rng.NextDouble() * w;
        _roamY = pad + _rng.NextDouble() * h;
        if (world.CollideWindows)
            world.Nav.SnapGoal(ref _roamX, ref _roamY);
        _roamLife = 50 + _rng.Next(60);
        _hasRoam = true;
        _repathIn = 0;
    }

    private void TickBurst(bool canFire)
    {
        if (_pause > 0)
        {
            _pause--;
            _burst = 0;
            return;
        }
        if (_burst > 0)
        {
            _burst--;
            if (_burst == 0)
                _pause = 3 + _rng.Next(8);
            return;
        }
        if (canFire && _rng.NextDouble() < 0.32)
            _burst = 5 + _rng.Next(12);
    }

    private TankEntity? PickEnemy(TankEntity tank, GameWorld world)
    {
        TankEntity? best = null;
        var bestD = double.MaxValue;
        foreach (var other in world.Tanks)
        {
            if (other.Id == tank.Id || !other.Alive || other.Destroy.Active || other.IsArenaCloser)
                continue;
            if (!CanSee(world, tank.X, tank.Y, other.X, other.Y))
                continue;
            var dx = other.X - tank.X;
            var dy = other.Y - tank.Y;
            var d = dx * dx + dy * dy;
            if (other.IsBoss)
                d *= 0.45;
            if (d >= bestD)
                continue;
            bestD = d;
            best = other;
        }
        return best;
    }

    private ShapeEntity? PickShape(TankEntity tank, GameWorld world)
    {
        ShapeEntity? best = null;
        var bestWeight = double.MaxValue;
        foreach (var s in world.Shapes)
        {
            if (s.Destroy.Active)
                continue;
            if (!CanSee(world, tank.X, tank.Y, s.X, s.Y))
                continue;
            var dx = s.X - tank.X;
            var dy = s.Y - tank.Y;
            var threat = s.Kind switch
            {
                ShapeKind.Crasher => 0.2,
                ShapeKind.AlphaPentagon => 0.35,
                _ => 1
            };
            var weight = (dx * dx + dy * dy) / (s.Xp + 8.0) * threat;
            if (weight >= bestWeight)
                continue;
            bestWeight = weight;
            best = s;
        }
        return best;
    }

    private static bool CanSee(GameWorld world, double x0, double y0, double x1, double y1) =>
        !world.CollideWindows || world.Windows.CanSee(x0, y0, x1, y1, 2);

    private static bool CanShoot(GameWorld world, double x0, double y0, double x1, double y1) =>
        !world.CollideWindows || world.Windows.CanSee(x0, y0, x1, y1, 10);
}
