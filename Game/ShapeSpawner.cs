using System.Windows.Media;

namespace DesktopDiep;

internal sealed class ShapeSpawner
{
    private readonly Random _rng;
    private double _timer;

    public ShapeSpawner(Random rng) => _rng = rng;

    public void Reset() => _timer = 0;

    public ShapeEntity? Maintain(List<ShapeEntity> shapes, double width, double height, double dt)
    {
        _timer -= dt;
        const int want = 16;
        if (shapes.Count >= want)
            return null;
        if (_timer > 0 && shapes.Count > want / 2)
            return null;
        _timer = 0.9;
        var shape = Create(shapes, width, height, anywhere: false);
        shapes.Add(shape);
        return shape;
    }

    public void Fill(List<ShapeEntity> shapes, double width, double height, int count)
    {
        for (var i = 0; i < count; i++)
            shapes.Add(Create(shapes, width, height, anywhere: true));
    }

    public ShapeEntity Spawn(List<ShapeEntity> shapes, double width, double height, ShapeKind? kind = null) =>
        Create(shapes, width, height, anywhere: true, kind);

    private ShapeEntity Create(List<ShapeEntity> shapes, double width, double height, bool anywhere, ShapeKind? forced = null)
    {
        ShapeKind kind;
        if (forced is { } pick)
            kind = pick;
        else
        {
            var roll = _rng.NextDouble();
            if (roll < 0.0018 && CountKind(shapes, ShapeKind.AlphaPentagon) < 1)
                kind = ShapeKind.AlphaPentagon;
            else if (roll < 0.04 && CountKind(shapes, ShapeKind.Crasher) < 4)
                kind = ShapeKind.Crasher;
            else if (roll < 0.72)
                kind = ShapeKind.Square;
            else if (roll < 0.93)
                kind = ShapeKind.Triangle;
            else
                kind = ShapeKind.Pentagon;
        }

        Stats(kind, out var fill, out var radius, out var hp, out var xp, out var mass,
            out var absorb, out var push, out var ram, out var speed);

        double x, y;
        if (anywhere || width < 100)
        {
            x = 40 + _rng.NextDouble() * Math.Max(40, width - 80);
            y = 40 + _rng.NextDouble() * Math.Max(40, height - 80);
        }
        else
        {
            var edge = _rng.Next(4);
            x = edge is 0 or 1 ? (edge == 0 ? 30 : width - 30) : 40 + _rng.NextDouble() * (width - 80);
            y = edge is 2 or 3 ? (edge == 2 ? 30 : height - 30) : 40 + _rng.NextDouble() * (height - 80);
        }

        var orbitAngle = _rng.NextDouble() * Math.PI * 2;
        var orbitSpeed = ShapeMotion.BaseOrbit * (_rng.Next(2) == 0 ? 1 : -1);
        var s = new ShapeEntity
        {
            X = x,
            Y = y,
            Radius = radius,
            Mass = mass,
            Health = hp,
            MaxHealth = hp,
            Absorption = absorb,
            PushFactor = push,
            RamDamage = ram,
            Xp = xp,
            Kind = kind,
            Fill = fill,
            Angle = orbitAngle,
            Spin = (_rng.NextDouble() < 0.5 ? -1 : 1) * 0.01,
            OrbitAngle = orbitAngle,
            OrbitSpeed = orbitSpeed,
            ShapeVelocity = speed,
            OrbitRadius = 1,
            OrbitCx = x,
            OrbitCy = y
        };
        s.Snap();
        return s;
    }

    private void Stats(ShapeKind kind, out Color fill, out double radius, out double hp, out int xp,
        out double mass, out double absorb, out double push, out double ram, out double speed)
    {
        absorb = 1;
        push = 8;
        ram = 0;
        speed = ShapeMotion.BaseVelocity;
        switch (kind)
        {
            case ShapeKind.AlphaPentagon:
                fill = DiepColors.AlphaPentagon;
                radius = 78;
                hp = 3600;
                xp = 3000;
                mass = 52;
                absorb = 0.05;
                push = 16;
                ram = 8;
                speed = ShapeMotion.BaseVelocity * 0.55;
                break;
            case ShapeKind.Crasher:
                var large = _rng.NextDouble() < 0.22;
                fill = DiepColors.Crasher;
                radius = large ? 22 : 14;
                hp = large ? 42 : 15;
                xp = large ? 25 : 15;
                mass = large ? 6.2 : 2.8;
                absorb = large ? 0.1 : 2;
                push = large ? 12 : 8;
                ram = 2;
                speed = large ? 6.5 : 6.2;
                break;
            case ShapeKind.Triangle:
                fill = DiepColors.Triangle;
                radius = 18;
                hp = 42;
                xp = 25;
                mass = 4.0;
                break;
            case ShapeKind.Pentagon:
                fill = DiepColors.Pentagon;
                radius = 26;
                hp = 140;
                xp = 130;
                mass = 8.5;
                break;
            default:
                fill = DiepColors.Square;
                radius = 16;
                hp = 15;
                xp = 10;
                mass = 2.6;
                break;
        }
    }

    private static int CountKind(List<ShapeEntity> shapes, ShapeKind kind)
    {
        var n = 0;
        foreach (var s in shapes)
        {
            if (!s.Destroy.Active && s.Kind == kind)
                n++;
        }
        return n;
    }
}
