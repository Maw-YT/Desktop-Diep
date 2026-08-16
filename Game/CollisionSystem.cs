namespace DesktopDiep;

public enum PhysKind : byte
{
    Tank,
    Shape,
    Bullet,
    Cursor
}

internal struct PhysBody
{
    public PhysKind Kind;
    public int Index;
    public double X, Y, Radius, Push, Absorb;
}

internal sealed class CollisionSystem
{
    private readonly SpatialHash _hash = new();
    private readonly List<PhysBody> _bodies = [];

    public int CellCount => _hash.CellCount;
    public int PairCount => _hash.PairCount;

    public void Resolve(GameWorld world, double dt)
    {
        _bodies.Clear();
        _hash.Clear();

        for (var i = 0; i < world.Tanks.Count; i++)
        {
            var tank = world.Tanks[i];
            if (!tank.Alive || tank.Destroy.Active)
                continue;
            Add(PhysKind.Tank, i, tank.X, tank.Y, tank.Radius, tank.PushFactor,
                world.IsGrabbed(PhysKind.Tank, i) ? 0 : tank.Absorption);
        }

        for (var i = 0; i < world.Shapes.Count; i++)
        {
            var s = world.Shapes[i];
            if (s.Destroy.Active)
                continue;
            Add(PhysKind.Shape, i, s.X, s.Y, s.Radius, s.PushFactor, world.IsGrabbed(PhysKind.Shape, i) ? 0 : s.Absorption);
        }

        for (var i = 0; i < world.Bullets.Count; i++)
        {
            var b = world.Bullets[i];
            if (b.Destroy.Active || b.Health <= 0)
                continue;
            Add(PhysKind.Bullet, i, b.X, b.Y, b.Radius, b.PushFactor, world.IsGrabbed(PhysKind.Bullet, i) ? 0 : b.Absorption);
        }

        var c = world.Cursor;
        Add(PhysKind.Cursor, -2, c.X, c.Y, c.Radius, c.PushFactor, c.Absorption);

        _hash.ForEachPair((ia, ib) => HandlePair(world, dt, ia, ib));
    }

    private void Add(PhysKind kind, int index, double x, double y, double radius, double push, double absorb)
    {
        var id = _bodies.Count;
        _bodies.Add(new PhysBody
        {
            Kind = kind,
            Index = index,
            X = x,
            Y = y,
            Radius = radius,
            Push = push,
            Absorb = absorb
        });
        _hash.Insert(id, x, y, radius);
    }

    private void HandlePair(GameWorld world, double dt, int ia, int ib)
    {
        var a = _bodies[ia];
        var b = _bodies[ib];
        if (FriendlyFire(world, a, b))
            return;
        if (world.IsGrabbed(a.Kind, a.Index) && b.Kind == PhysKind.Cursor)
            return;
        if (world.IsGrabbed(b.Kind, b.Index) && a.Kind == PhysKind.Cursor)
            return;

        if (!Collision.Circles(a.X, a.Y, a.Radius, b.X, b.Y, b.Radius))
        {
            if (!SweptHit(world, a, b))
                return;
        }

        ApplyKnockback(world, a, b);

        if (a.Kind == PhysKind.Shape && b.Kind == PhysKind.Shape)
        {
            ShapeMotion.NudgeOrbit(world.Shapes[a.Index], 0.12);
            ShapeMotion.NudgeOrbit(world.Shapes[b.Index], 0.12);
            return;
        }

        if (TryTankTank(world, dt, a, b))
            return;
        if (TryTankShape(world, dt, a, b) || TryTankShape(world, dt, b, a))
            return;
        if (TryBulletTank(world, a, b) || TryBulletTank(world, b, a))
            return;
        if (TryBulletBullet(world, a, b))
            return;
        TryBulletShape(world, a, b);
        TryBulletShape(world, b, a);
    }

    private static bool FriendlyFire(GameWorld world, PhysBody a, PhysBody b)
    {
        var ownerA = Owner(world, a);
        var ownerB = Owner(world, b);
        if (ownerA < 0 || ownerB < 0 || ownerA != ownerB)
            return false;
        if (a.Kind == PhysKind.Bullet && b.Kind == PhysKind.Bullet)
        {
            var ba = world.Bullets[a.Index];
            var bb = world.Bullets[b.Index];
            if (Projectile.IsDroneLike(ba.Kind) && Projectile.IsDroneLike(bb.Kind))
                return false;
        }
        return true;
    }

    private static int Owner(GameWorld world, PhysBody body) => body.Kind switch
    {
        PhysKind.Tank => world.Tanks[body.Index].Id,
        PhysKind.Bullet => world.Bullets[body.Index].OwnerId,
        _ => -1
    };

    private static bool SweptHit(GameWorld world, PhysBody a, PhysBody b)
    {
        if (a.Kind != PhysKind.Bullet && b.Kind != PhysKind.Bullet)
            return false;
        var bulletBody = a.Kind == PhysKind.Bullet ? a : b;
        var other = a.Kind == PhysKind.Bullet ? b : a;
        if (other.Kind is not (PhysKind.Shape or PhysKind.Tank))
            return false;
        var bullet = world.Bullets[bulletBody.Index];
        return Math2.SweepCircle(bullet.PrevX, bullet.PrevY, bullet.X, bullet.Y, other.X, other.Y, a.Radius + b.Radius, out _);
    }

    private static void ApplyKnockback(GameWorld world, PhysBody a, PhysBody b)
    {
        GetVel(world, a, out var x1, out var y1, out var vx1, out var vy1);
        GetVel(world, b, out var x2, out var y2, out var vx2, out var vy2);
        Collision.Knockback(
            ref x1, ref y1, ref vx1, ref vy1, a.Absorb, a.Push,
            ref x2, ref y2, ref vx2, ref vy2, b.Absorb, b.Push);
        SetVel(world, a, vx1, vy1);
        SetVel(world, b, vx2, vy2);
    }

    private static bool TryTankTank(GameWorld world, double dt, PhysBody a, PhysBody b)
    {
        if (a.Kind != PhysKind.Tank || b.Kind != PhysKind.Tank)
            return false;
        var t1 = world.Tanks[a.Index];
        var t2 = world.Tanks[b.Index];
        world.HurtTank(t1, TankStats.BodyDamage(t2), t2);
        world.HurtTank(t2, TankStats.BodyDamage(t1), t1);
        return true;
    }

    private static bool TryTankShape(GameWorld world, double dt, PhysBody tankBody, PhysBody shapeBody)
    {
        if (tankBody.Kind != PhysKind.Tank || shapeBody.Kind != PhysKind.Shape)
            return false;
        var tank = world.Tanks[tankBody.Index];
        var s = world.Shapes[shapeBody.Index];
        if (s.Destroy.Active)
            return true;
        ShapeMotion.NudgeOrbit(s, 0.25);
        world.HurtTank(tank, s.RamDamage > 0 ? s.RamDamage : 12 * dt, null);
        s.Hurt(TankStats.BodyDamage(tank));
        if (s.Health <= 0)
            world.KillShape(s, tank);
        return true;
    }

    private static bool TryBulletTank(GameWorld world, PhysBody bulletBody, PhysBody tankBody)
    {
        if (bulletBody.Kind != PhysKind.Bullet || tankBody.Kind != PhysKind.Tank)
            return false;
        var bullet = world.Bullets[bulletBody.Index];
        var tank = world.Tanks[tankBody.Index];
        if (bullet.Destroy.Active || !tank.Alive)
            return true;
        var killer = world.FindTank(bullet.OwnerId);
        world.HurtTank(tank, bullet.Damage, killer);
        bullet.Health -= 1;
        FinishBullet(bullet);
        return true;
    }

    private static bool TryBulletBullet(GameWorld world, PhysBody a, PhysBody b)
    {
        if (a.Kind != PhysKind.Bullet || b.Kind != PhysKind.Bullet)
            return false;
        var ba = world.Bullets[a.Index];
        var bb = world.Bullets[b.Index];
        if (ba.Destroy.Active || bb.Destroy.Active)
            return true;
        if (ba.OwnerId == bb.OwnerId && Projectile.IsDroneLike(ba.Kind) && Projectile.IsDroneLike(bb.Kind))
            return true;
        ba.Health -= 1;
        bb.Health -= 1;
        FinishBullet(ba);
        FinishBullet(bb);
        return true;
    }

    private static void TryBulletShape(GameWorld world, PhysBody bulletBody, PhysBody shapeBody)
    {
        if (bulletBody.Kind != PhysKind.Bullet || shapeBody.Kind != PhysKind.Shape)
            return;
        var bullet = world.Bullets[bulletBody.Index];
        var shape = world.Shapes[shapeBody.Index];
        if (bullet.Destroy.Active || shape.Destroy.Active)
            return;
        ShapeMotion.NudgeOrbit(shape, 0.18);
        shape.Hurt(bullet.Damage);
        bullet.Health -= 1;
        if (shape.Health <= 0)
            world.KillShape(shape, world.FindTank(bullet.OwnerId));
        FinishBullet(bullet);
    }

    private static void FinishBullet(BulletEntity bullet)
    {
        if (bullet.Health > 0)
            return;
        if (bullet.NoDestroyAnim || !bullet.Destroy.Begin())
            bullet.Health = 0;
    }

    private static void GetVel(GameWorld world, PhysBody body, out double x, out double y, out double vx, out double vy)
    {
        switch (body.Kind)
        {
            case PhysKind.Tank:
                var tank = world.Tanks[body.Index];
                x = tank.X;
                y = tank.Y;
                vx = tank.Vx;
                vy = tank.Vy;
                return;
            case PhysKind.Shape:
                var s = world.Shapes[body.Index];
                x = s.X;
                y = s.Y;
                vx = s.Vx;
                vy = s.Vy;
                return;
            case PhysKind.Cursor:
                x = world.Cursor.X;
                y = world.Cursor.Y;
                vx = world.Cursor.Vx;
                vy = world.Cursor.Vy;
                return;
            default:
                var b = world.Bullets[body.Index];
                x = b.X;
                y = b.Y;
                vx = b.Vx;
                vy = b.Vy;
                return;
        }
    }

    private static void SetVel(GameWorld world, PhysBody body, double vx, double vy)
    {
        switch (body.Kind)
        {
            case PhysKind.Tank:
                world.Tanks[body.Index].Vx = vx;
                world.Tanks[body.Index].Vy = vy;
                break;
            case PhysKind.Shape:
                world.Shapes[body.Index].Vx = vx;
                world.Shapes[body.Index].Vy = vy;
                break;
            case PhysKind.Cursor:
                world.Cursor.Vx = vx;
                world.Cursor.Vy = vy;
                break;
            default:
                world.Bullets[body.Index].Vx = vx;
                world.Bullets[body.Index].Vy = vy;
                break;
        }
    }
}
