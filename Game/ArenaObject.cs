using System.Windows.Media;
using MoonSharp.Interpreter;

namespace DesktopDiep;

/// <summary>Rect or circle placed by a gamemode or a Lua mod. Rects are centered on X, Y.</summary>
public sealed class ArenaObject
{
    public int Id;
    public string Kind = "object";
    public string? CustomId;
    public string Label = "";
    public int Team;
    public double X, Y, PrevX, PrevY;
    public double W, H, Radius;
    public double Vx, Vy, Angle, Spin;
    public bool Solid;
    public bool ModeOwned;
    public bool Alive = true;
    public double DamagePerTick;
    public double HealPerTick;
    public double Health;
    public double MaxHealth;
    public double Opacity = 1;
    public int Sides;
    public Color Fill = DiepColors.Neutral;
    public string? Shader;
    public int ShootCooldown;
    public Closure? TickFn;

    public bool Circle => Radius > 0;

    public double DrawX(double t) => Interp.Lerp(PrevX, X, t);
    public double DrawY(double t) => Interp.Lerp(PrevY, Y, t);

    public void Capture()
    {
        PrevX = X;
        PrevY = Y;
    }

    public void Snap()
    {
        PrevX = X;
        PrevY = Y;
    }

    public WindowBox Box()
    {
        if (Circle)
        {
            return new WindowBox(X - Radius, Y - Radius, X + Radius, Y + Radius);
        }
        return new WindowBox(X - W * 0.5, Y - H * 0.5, X + W * 0.5, Y + H * 0.5);
    }

    public bool Hits(double x, double y, double radius)
    {
        if (Circle)
        {
            var dx = x - X;
            var dy = y - Y;
            var reach = Radius + radius;
            return dx * dx + dy * dy <= reach * reach;
        }
        var box = Box();
        var cx = Math.Clamp(x, box.Left, box.Right);
        var cy = Math.Clamp(y, box.Top, box.Bottom);
        var dx2 = x - cx;
        var dy2 = y - cy;
        return dx2 * dx2 + dy2 * dy2 <= radius * radius;
    }
}
