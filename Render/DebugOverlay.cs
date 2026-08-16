using System.Text;
using System.Windows;
using System.Windows.Media;

namespace DesktopDiep;

internal sealed class DebugOverlay
{
    private readonly DrawCache _draw;

    public DebugOverlay(DrawCache draw) => _draw = draw;

    public void Draw(DrawingContext dc, GameWorld world, Size size)
    {
        var debug = world.Debug;
        if (!debug.Enabled)
            return;

        if (debug.ShowHitboxes)
            DrawHitboxes(dc, world);
        if (debug.ShowVelocity)
            DrawVelocities(dc, world);
        DrawAi(dc, world);
        DrawPanel(dc, world, size);

        if (debug.NoticeLife > 0 && debug.Notice is not null)
        {
            var alpha = (byte)Math.Clamp(debug.NoticeLife / 0.35 * 255, 0, 230);
            var banner = _draw.Text(debug.Notice, 18, Color.FromArgb(alpha, 124, 252, 0));
            dc.DrawText(banner, new Point((size.Width - banner.Width) / 2, 16));
        }
    }

    private void DrawHitboxes(DrawingContext dc, GameWorld world)
    {
        var t = world.DrawAlpha;
        var pen = _draw.Pen(Color.FromArgb(180, 124, 252, 0), 1);
        foreach (var tank in world.Tanks)
        {
            if (tank.Alive || tank.Destroy.Active)
                dc.DrawEllipse(null, pen, new Point(tank.DrawX(t), tank.DrawY(t)), tank.Radius, tank.Radius);
        }
        var orbitPen = _draw.Pen(Color.FromArgb(50, 180, 255, 255), 1);
        foreach (var s in world.Shapes)
        {
            dc.DrawEllipse(null, pen, new Point(s.DrawX(t), s.DrawY(t)), s.Radius, s.Radius);
            if (s.OrbitRadius > 8)
                dc.DrawEllipse(null, orbitPen, new Point(s.OrbitCx, s.OrbitCy), s.OrbitRadius, s.OrbitRadius);
        }
        foreach (var b in world.Bullets)
            dc.DrawEllipse(null, _draw.Pen(Colors.White, 1), new Point(b.DrawX(t), b.DrawY(t)), b.Radius, b.Radius);
        var cur = world.Cursor;
        dc.DrawEllipse(null, _draw.Pen(Color.FromArgb(200, 255, 255, 255), 1.5), new Point(cur.X, cur.Y), cur.Radius, cur.Radius);
    }

    private void DrawVelocities(DrawingContext dc, GameWorld world)
    {
        var t = world.DrawAlpha;
        var pen = _draw.Pen(Color.FromArgb(200, 255, 180, 40), 1.5);
        foreach (var tank in world.Tanks)
            DrawRay(dc, tank.DrawX(t), tank.DrawY(t), tank.Vx, tank.Vy, 0.12, pen);
        foreach (var s in world.Shapes)
            DrawRay(dc, s.DrawX(t), s.DrawY(t), s.Vx, s.Vy, 0.2, pen);
    }

    private void DrawAi(DrawingContext dc, GameWorld world)
    {
        var t = world.DrawAlpha;
        foreach (var tank in world.Tanks)
        {
            if (!tank.Alive || !tank.Brain.HasTarget)
                continue;
            var pen = _draw.Pen(tank.Brain.Fleeing
                ? Color.FromArgb(200, 255, 80, 80)
                : Color.FromArgb(180, tank.Fill.R, tank.Fill.G, tank.Fill.B), 1.5);
            dc.DrawLine(pen,
                new Point(tank.DrawX(t), tank.DrawY(t)),
                new Point(tank.Brain.AimX, tank.Brain.AimY));
        }
    }

    private static void DrawRay(DrawingContext dc, double x, double y, double vx, double vy, double scale, Pen pen)
    {
        dc.DrawLine(pen, new Point(x, y), new Point(x + vx * scale, y + vy * scale));
    }

    private void DrawPanel(DrawingContext dc, GameWorld world, Size size)
    {
        var t = world.Selected;
        var d = world.Debug;
        var sb = new StringBuilder();
        sb.AppendLine("DEBUG  pet");
        sb.AppendLine($"fps {d.Fps:0.0}   {d.FrameMs:0.00} ms   interp {(world.Interpolate ? $"on {d.Alpha:0.00}" : "off")}");
        sb.AppendLine($"phys {GameWorld.TicksPerSecond} tps   tick #{d.Tick}   {(d.Paused ? "PAUSED" : "running")}");
        sb.AppendLine($"tanks {world.Tanks.Count}   shapes {world.Shapes.Count}   bullets {world.Bullets.Count}   hash {d.HashCells}/{d.HashPairs}");
        if (t is not null)
        {
            sb.AppendLine($"sel {t.Class.Name} ({t.X:0},{t.Y:0})  v ({t.Vx:0},{t.Vy:0})");
            sb.AppendLine($"hp {t.Health:0.0}/{t.MaxHealth:0}  lv {t.Level}  xp {t.XpIntoLevel}/{t.XpForNext}  score {t.Score}");
            sb.AppendLine($"combat {t.CombatTimer:0.00}  alive {t.Alive}  ai {(t.Brain.Fleeing ? "flee" : "hunt")}  shot {t.Brain.WantsShot}");
            sb.Append("stats");
            for (var i = 0; i < 8; i++)
                sb.Append($"  {i + 1}:{t.Stats[i]}");
            sb.AppendLine();
        }
        sb.AppendLine($"cursor ({world.Cursor.X:0},{world.Cursor.Y:0})  {(world.Cursor.Grabbing ? "GRAB" : world.Cursor.Hovering ? "hover" : "free")}");
        sb.AppendLine();
        sb.AppendLine("left-click tray to spawn   right-click tray for tanks/stats");
        sb.AppendLine("Ctrl+Shift+D debug   P pause   R reset");

        var text = _draw.Text(sb.ToString().TrimEnd(), 12, Color.FromArgb(230, 220, 255, 220));
        var pad = 10.0;
        var rect = new Rect(12, size.Height - text.Height - 24, text.Width + pad * 2, text.Height + pad * 2);
        dc.DrawRoundedRectangle(_draw.Brush(Color.FromArgb(170, 10, 18, 12)), _draw.Pen(DiepColors.Debug, 1), rect, 6, 6);
        dc.DrawText(text, new Point(rect.X + pad, rect.Y + pad));
    }
}
