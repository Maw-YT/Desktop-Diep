using System.Windows.Media;

namespace DesktopDiep;

internal sealed class ShapeSpawner
{
    private readonly Random _rng;
    private double _timer;

    public ShapeSpawner(Random rng) => _rng = rng;

    public void Reset() => _timer = 0;

    public void Maintain(List<ShapeEntity> shapes, double width, double height, double dt)
    {
        _timer -= dt;
        const int want = 16;
        if (shapes.Count >= want)
            return;
        if (_timer > 0 && shapes.Count > want / 2)
            return;
        _timer = 0.9;
        shapes.Add(Create(shapes, width, height, anywhere: false));
    }

    public void Fill(List<ShapeEntity> shapes, double width, double height, int count)
    {
        for (var i = 0; i < count; i++)
            shapes.Add(Create(shapes, width, height, anywhere: true));
    }

    private ShapeEntity Create(List<ShapeEntity> shapes, double width, double height, bool anywhere)
    {
        var roll = _rng.NextDouble();
        ShapeKind kind;
        Color fill;
        double radius, hp, mass, absorb = 1, push = 8, ram = 0, speed = ShapeMotion.BaseVelocity;
        int xp;
        if (roll < 0.04 && CountKind(shapes, ShapeKind.Crasher) < 4)
        {
            var large = _rng.NextDouble() < 0.22;
            kind = ShapeKind.Crasher;
            fill = DiepColors.Crasher;
            radius = large ? 22 : 14;
            hp = large ? 30 : 10;
            xp = large ? 25 : 15;
            mass = large ? 5.5 : 2.4;
            absorb = large ? 0.1 : 2;
            push = large ? 12 : 8;
            ram = 2;
            speed = large ? 6.5 : 6.2;
        }
        else if (roll < 0.72)
        {
            kind = ShapeKind.Square;
            fill = DiepColors.Square;
            radius = 16;
            hp = 10;
            xp = 10;
            mass = 2.2;
        }
        else if (roll < 0.93)
        {
            kind = ShapeKind.Triangle;
            fill = DiepColors.Triangle;
            radius = 18;
            hp = 30;
            xp = 25;
            mass = 3.4;
        }
        else
        {
            kind = ShapeKind.Pentagon;
            fill = DiepColors.Pentagon;
            radius = 26;
            hp = 100;
            xp = 130;
            mass = 7.2;
        }

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
