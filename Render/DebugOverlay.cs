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
        if (world.ShowNav)
            DrawNav(dc, world);
        if (world.ShowHash)
            DrawHash(dc, world);
        if (!debug.Enabled)
            return;

        if (debug.NoticeLife > 0 && debug.Notice is not null)
        {
            var alpha = (byte)Math.Clamp(debug.NoticeLife / 0.35 * 255, 0, 230);
            var banner = _draw.Text(debug.Notice, 18, Color.FromArgb(alpha, 124, 252, 0));
            dc.DrawText(banner, new Point((size.Width - banner.Width) / 2, 16));
        }

        if (debug.ShowHitboxes)
            DrawHitboxes(dc, world);
        if (debug.ShowVelocity)
            DrawVelocities(dc, world);
        DrawAi(dc, world);
        DrawPanel(dc, world, size);
    }

    private void DrawNav(DrawingContext dc, GameWorld world)
    {
        var nav = world.Nav;
        var blocked = _draw.Brush(Color.FromArgb(70, 255, 64, 96));
        var cell = NavGrid.Cell;
        for (var y = 0; y < nav.Rows; y++)
        {
            for (var x = 0; x < nav.Cols; x++)
            {
                if (!nav.CellBlocked(x, y))
                    continue;
                dc.DrawRectangle(blocked, null, new Rect(x * cell, y * cell, cell, cell));
            }
        }

        var boxPen = _draw.Pen(Color.FromArgb(160, 80, 220, 255), 1.5);
        if (world.CollideWindows)
        {
            foreach (var box in world.Windows.Boxes)
                dc.DrawRectangle(null, boxPen, new Rect(box.Left, box.Top, box.Right - box.Left, box.Bottom - box.Top));
        }

        var t = world.DrawAlpha;
        foreach (var tank in world.Tanks)
        {
            if (!tank.Alive)
                continue;
            var path = tank.Brain.Path;
            if (path.Count < 2)
                continue;
            var color = Color.FromArgb(230, tank.Fill.R, tank.Fill.G, tank.Fill.B);
            var pen = _draw.Pen(color, 2.4);
            var dot = _draw.Brush(color);
            var node = _draw.Brush(Color.FromArgb(180, tank.Fill.R, tank.Fill.G, tank.Fill.B));
            for (var i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                dc.DrawLine(pen, new Point(a.X, a.Y), new Point(b.X, b.Y));
                dc.DrawEllipse(node, null, new Point(b.X, b.Y), 3.5, 3.5);
            }
            dc.DrawEllipse(dot, null, new Point(tank.DrawX(t), tank.DrawY(t)), 4, 4);
            var goal = path[^1];
            dc.DrawEllipse(null, pen, new Point(goal.X, goal.Y), 6, 6);
        }
    }

    private void DrawHash(DrawingContext dc, GameWorld world)
    {
        var cell = SpatialHash.CellSize;
        var pen = _draw.Pen(Color.FromArgb(90, 255, 220, 80), 1);
        world.ForEachHashCell((cx, cy, count) =>
        {
            var alpha = (byte)Math.Clamp(40 + count * 28, 40, 160);
            var fill = _draw.Brush(Color.FromArgb(alpha, 255, 200, 40));
            dc.DrawRectangle(fill, pen, new Rect(cx * cell, cy * cell, cell, cell));
        });
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
            if (world.CollideWindows && !world.Windows.CanSee(tank.DrawX(t), tank.DrawY(t), tank.Brain.AimX, tank.Brain.AimY))
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
        if (world.ShowNav)
            sb.AppendLine($"nav {world.Nav.Cols}x{world.Nav.Rows}  blocked {world.Nav.BlockedCount}  cell {NavGrid.Cell}");
        if (world.ShowHash)
            sb.AppendLine($"hash cells {d.HashCells}  pairs {d.HashPairs}  cell {SpatialHash.CellSize}");
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
