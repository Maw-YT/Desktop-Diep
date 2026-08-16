using System.Windows;
using System.Windows.Media;

namespace DesktopDiep;

internal sealed class WorldRenderer
{
    private readonly DrawCache _draw;

    public WorldRenderer(DrawCache draw) => _draw = draw;

    public void Draw(DrawingContext dc, GameWorld world)
    {
        var t = world.DrawAlpha;
        foreach (var s in world.Shapes)
            DrawShape(dc, s, t);
        foreach (var b in world.Bullets)
            DrawBullet(dc, b, t);
        foreach (var tank in world.Tanks)
        {
            if (tank.Alive || tank.Destroy.Active)
                DrawTank(dc, tank, t, world.ShowSelectionHalo && tank == world.Selected);
        }
    }

    private void DrawTank(DrawingContext dc, TankEntity tank, double t, bool selected)
    {
        var x = tank.DrawX(t);
        var y = tank.DrawY(t);
        var dying = tank.Destroy.Active;
        PushDeath(dc, tank.Destroy, x, y, t);
        var flash = tank.Flash.Draw(t);
        var color = DiepColors.Hit(tank.Fill, flash);
        var fill = _draw.Brush(color);
        var stroke = _draw.Pen(DiepColors.Stroke(color), 3.2);
        var barrelFill = _draw.Brush(DiepColors.Barrel);
        var barrelStroke = _draw.Pen(DiepColors.Stroke(DiepColors.Barrel), 3.2);
        var borderFill = _draw.Brush(DiepColors.Border);
        var borderStroke = _draw.Pen(DiepColors.Stroke(DiepColors.Border), 3.2);
        var deg = tank.DrawAngle(t) * 180 / Math.PI;
        var scale = tank.Radius / 50.0;

        foreach (var guard in tank.Guards)
        {
            dc.PushTransform(new RotateTransform(guard.DrawAngle(t) * 180 / Math.PI));
            dc.DrawGeometry(borderFill, borderStroke, DrawCache.RegularPolygon(guard.Sides, tank.Radius * guard.SizeRatio));
            dc.Pop();
        }

        dc.PushTransform(new RotateTransform(deg));
        DrawPrePost(dc, tank.Class.PreAddon, tank, scale, barrelFill, barrelStroke);
        foreach (var barrel in tank.Barrels)
            DrawBarrel(dc, barrel, scale, t, barrelFill, barrelStroke);
        DrawPrePost(dc, tank.Class.PostAddon, tank, scale, barrelFill, barrelStroke);
        DrawBody(dc, tank, fill, stroke);
        dc.Pop();

        DrawTurrets(dc, tank, t, scale, barrelFill, barrelStroke);
        if (selected && !dying)
            dc.DrawEllipse(null, _draw.Pen(Colors.White, 1.6), new Point(0, 0), tank.Radius + 6, tank.Radius + 6);
        PopDeath(dc);

        if (dying)
            return;
        DrawBar(dc, x, y + tank.Radius + 10, 42, 4.5, tank.Health / tank.MaxHealth, DiepColors.Health);
        DrawBar(dc, x, y + tank.Radius + 16, 42, 3, tank.XpIntoLevel / (double)Math.Max(1, tank.XpForNext), DiepColors.Xp);
        var label = _draw.Text($"{tank.Class.Name}  Lv {tank.Level}", 11, Colors.White);
        dc.DrawText(label, new Point(x - label.Width / 2, y - tank.Radius - 18));
    }

    private static void DrawBody(DrawingContext dc, TankEntity tank, Brush fill, Pen stroke)
    {
        var sides = tank.Class.Sides;
        if (sides <= 1)
            dc.DrawEllipse(fill, stroke, new Point(0, 0), tank.Radius, tank.Radius);
        else
            dc.DrawGeometry(fill, stroke, DrawCache.RegularPolygon(sides, tank.Radius));
    }

    private void DrawBarrel(DrawingContext dc, BarrelState barrel, double scale, double t, Brush fill, Pen stroke)
    {
        var def = barrel.Def;
        if (def.Addon == "purplebarrel")
        {
            fill = _draw.Brush(DiepColors.TeamPurple);
            stroke = _draw.Pen(DiepColors.Stroke(DiepColors.TeamPurple), 3.2);
        }
        var length = Math.Max(0.5, barrel.DrawLength(t, scale));
        var width = Math.Max(0.5, def.Width * scale);
        dc.PushTransform(new RotateTransform(def.Angle * 180 / Math.PI));
        dc.PushTransform(new TranslateTransform(def.Distance * scale, def.Offset * scale));
        if (def.IsTrapezoid)
        {
            var invert = Math.Abs(Math2.NormalizeAngle(def.TrapezoidDirection)) > Math.PI / 2;
            var near = invert ? width * 1.45 : width;
            var far = invert ? width : width * 1.45;
            DrawTrapezoid(dc, fill, stroke, length, near, far);
        }
        else
        {
            dc.DrawRoundedRectangle(fill, stroke, new Rect(0, -width / 2, length, width), 2.2, 2.2);
        }
        if (def.Addon == "trapLauncher")
        {
            var launch = width * (20.0 / 42);
            dc.PushTransform(new TranslateTransform(length, 0));
            DrawTrapezoid(dc, fill, stroke, launch, width, width * 1.35);
            dc.Pop();
        }
        dc.Pop();
        dc.Pop();
    }

    private static void DrawTrapezoid(DrawingContext dc, Brush fill, Pen stroke, double length, double nearW, double farW)
    {
        if (length <= 0 || nearW <= 0 || farW <= 0)
            return;
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(0, -nearW / 2), true, true);
            ctx.LineTo(new Point(length, -farW / 2), true, false);
            ctx.LineTo(new Point(length, farW / 2), true, false);
            ctx.LineTo(new Point(0, nearW / 2), true, false);
        }
        dc.DrawGeometry(fill, stroke, g);
    }

    private static void DrawPrePost(DrawingContext dc, string? addon, TankEntity tank, double scale, Brush barrelFill, Pen barrelStroke)
    {
        switch (addon)
        {
            case "pronounced":
            {
                var size = tank.Radius;
                var width = 42 * scale;
                var offset = 40 * scale;
                dc.PushTransform(new TranslateTransform(offset - size / 2, 0));
                DrawTrapezoid(dc, barrelFill, barrelStroke, size, width * 1.35, width);
                dc.Pop();
                break;
            }
            case "dompronounced":
            {
                var size = 22 * scale;
                var width = 35 * scale;
                dc.PushTransform(new TranslateTransform(tank.Radius - size / 2, 0));
                DrawTrapezoid(dc, barrelFill, barrelStroke, size, width * 1.35, width);
                dc.Pop();
                break;
            }
            case "launcher":
            {
                var size = 65.5 * Math.Sqrt(2) * scale;
                var width = 33.6 * scale;
                DrawTrapezoid(dc, barrelFill, barrelStroke, size, width * 1.45, width);
                break;
            }
        }
    }

    private void DrawTurrets(DrawingContext dc, TankEntity tank, double t, double scale, Brush barrelFill, Pen barrelStroke)
    {
        var rot = tank.DrawRotator(t);
        foreach (var turret in tank.Turrets)
        {
            var a = turret.MountAngle + rot;
            var r = tank.Radius * turret.Orbit;
            dc.PushTransform(new TranslateTransform(Math.Cos(a) * r, Math.Sin(a) * r));
            dc.PushTransform(new RotateTransform(turret.DrawAngle(t) * 180 / Math.PI));
            DrawBarrel(dc, turret.Barrel, scale, t, barrelFill, barrelStroke);
            var baseR = 25 * scale;
            dc.DrawEllipse(barrelFill, barrelStroke, new Point(0, 0), baseR, baseR);
            dc.Pop();
            dc.Pop();
        }
    }

    private void DrawBullet(DrawingContext dc, BulletEntity b, double t)
    {
        var x = b.DrawX(t);
        var y = b.DrawY(t);
        PushDeath(dc, b.Destroy, x, y, t);
        var opacity = b.DrawOpacity(t);
        if (opacity < 0.999)
            dc.PushOpacity(opacity);
        var fill = _draw.Brush(b.Fill);
        var stroke = _draw.Pen(DiepColors.Stroke(b.Fill), 2.2);
        var barrelFill = _draw.Brush(DiepColors.Barrel);
        var barrelStroke = _draw.Pen(DiepColors.Stroke(DiepColors.Barrel), 2.2);
        var scale = b.Radius / 50.0;
        dc.PushTransform(new RotateTransform(b.DrawAngle(t) * 180 / Math.PI));
        foreach (var gun in b.Guns)
            DrawBarrel(dc, gun, scale, t, barrelFill, barrelStroke);
        if (b.IsStar)
            dc.DrawGeometry(fill, stroke, DrawCache.Star(3, b.Radius));
        else if (b.Sides <= 1)
            dc.DrawEllipse(fill, stroke, new Point(0, 0), b.Radius, b.Radius);
        else
            dc.DrawGeometry(fill, stroke, DrawCache.RegularPolygon(b.Sides, b.Radius));
        dc.Pop();
        if (opacity < 0.999)
            dc.Pop();
        PopDeath(dc);
    }

    private void DrawShape(DrawingContext dc, ShapeEntity s, double t)
    {
        var x = s.DrawX(t);
        var y = s.DrawY(t);
        var fill = DiepColors.Hit(s.Fill, s.Flash.Draw(t));
        var geo = DrawCache.Polygon(s.Kind, s.Radius);
        PushDeath(dc, s.Destroy, x, y, t);
        dc.PushTransform(new RotateTransform(s.DrawAngle(t) * 180 / Math.PI));
        dc.DrawGeometry(_draw.Brush(fill), _draw.Pen(DiepColors.Stroke(fill), 3), geo);
        dc.Pop();
        PopDeath(dc);
        if (!s.Destroy.Active && s.Health < s.MaxHealth - 0.2)
            DrawBar(dc, x, y + s.Radius + 8, s.Radius * 2, 4.5, s.Health / s.MaxHealth, DiepColors.Health);
    }

    private static void PushDeath(DrawingContext dc, DestroyAnim death, double x, double y, double interp)
    {
        var p = death.DrawOpacity(interp);
        dc.PushOpacity(p);
        dc.PushTransform(new TranslateTransform(x, y));
        var scale = death.Active ? death.DrawScale(interp) : 1;
        dc.PushTransform(new ScaleTransform(scale, scale));
    }

    private static void PopDeath(DrawingContext dc)
    {
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    private void DrawBar(DrawingContext dc, double cx, double y, double width, double height, double ratio, Color fill)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        var x = cx - width / 2;
        dc.DrawRoundedRectangle(_draw.Brush(DiepColors.HealthBack), null, new Rect(x, y, width, height), 2, 2);
        if (ratio > 0)
            dc.DrawRoundedRectangle(_draw.Brush(fill), null, new Rect(x, y, width * ratio, height), 2, 2);
    }
}
