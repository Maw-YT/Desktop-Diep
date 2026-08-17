using System.Windows;
using System.Windows.Media;

namespace DesktopDiep;

internal sealed class WorldRenderer
{
    private readonly DrawCache _draw;
    private RenderStyle _style = RenderStyle.New;

    public WorldRenderer(DrawCache draw) => _draw = draw;

    public void Draw(DrawingContext dc, GameWorld world)
    {
        _style = RenderLooks.Normalize(world.RenderStyle);
        var t = world.DrawAlpha;
        var bars = world.Alpha;
        foreach (var s in world.Shapes)
            DrawShape(dc, s, t, bars);
        foreach (var b in world.Bullets)
        {
            if (!b.Visible && !b.Destroy.Active)
                continue;
            DrawBullet(dc, b, t);
        }
        foreach (var tank in world.Tanks)
        {
            if (tank.Alive || tank.Destroy.Active)
                DrawTank(dc, tank, t, bars, world.ShowSelectionHalo && tank == world.Selected);
        }

        world.Mods?.BeginFrameDraw();
        world.Mods?.Draw.Render(dc, _draw);
    }

    private void DrawTank(DrawingContext dc, TankEntity tank, double t, double bars, bool selected)
    {
        var x = tank.DrawX(t);
        var y = tank.DrawY(t);
        var dying = tank.Destroy.Active;
        PushDeath(dc, tank.Destroy, x, y, t);
        if (_style == RenderStyle.Shaded)
            RenderLooks.SoftShadow(dc, tank.Radius);

        var flash = tank.Flash.Draw(t);
        var color = DiepColors.Hit(tank.Fill, flash);
        var deg = tank.DrawAngle(t) * 180 / Math.PI;
        var fill = BodyFill(color, deg);
        var stroke = BodyStroke(color);
        var barrelStroke = BodyStroke(DiepColors.Barrel);
        var borderStroke = BodyStroke(DiepColors.Border);
        var scale = TankStats.GunScale(tank);

        DrawTankCore(dc, tank, t, scale, deg, fill, stroke, barrelStroke, borderStroke);

        if (selected && !dying)
            dc.DrawEllipse(null, _draw.Pen(Colors.White, 1.6), new Point(0, 0), tank.Radius + 6, tank.Radius + 6);
        PopDeath(dc);

        if (dying)
            return;
        var nameSize = Math.Clamp(tank.Radius * 0.42, 10, 28);
        if (tank.IsBoss)
        {
            DrawBar(dc, x, y + tank.Radius + 10, BarWidth(tank.Radius), 6.5, tank.DrawHealthRatio(bars), DiepColors.Health);
            DrawNametag(dc, tank.BossAltName ?? tank.Class.Name, nameSize, Colors.White, x, y - tank.Radius - nameSize * 1.35);
        }
        else if (!tank.IsArenaCloser)
        {
            DrawBar(dc, x, y + tank.Radius + 10, BarWidth(tank.Radius), 5.5, tank.DrawHealthRatio(bars), DiepColors.Health);
            DrawBar(dc, x, y + tank.Radius + 22, BarWidth(tank.Radius), 3.6, tank.DrawXpRatio(bars), DiepColors.Xp);
            DrawNametag(dc, $"{tank.Class.Name}  Lv {tank.Level}", nameSize, Colors.White, x, y - tank.Radius - nameSize * 1.35);
        }
        else
        {
            DrawNametag(dc, "Arena Closer", nameSize, Colors.White, x, y - tank.Radius - nameSize * 1.35);
        }
    }

    private void DrawTankCore(
        DrawingContext dc, TankEntity tank, double t, double scale, double deg,
        Brush fill, Pen stroke, Pen barrelStroke, Pen borderStroke)
    {
        foreach (var guard in tank.Guards)
        {
            var gDeg = guard.DrawAngle(t) * 180 / Math.PI;
            var geo = DrawCache.RegularPolygon(guard.Sides, tank.Radius * guard.SizeRatio);
            dc.PushTransform(new RotateTransform(gDeg));
            if (_style == RenderStyle.Shaded)
            {
                RenderLooks.SoftPartShadow(dc, gDeg, tank.Radius * guard.SizeRatio,
                    brush => dc.DrawGeometry(brush, null, geo));
            }
            dc.DrawGeometry(BodyFill(DiepColors.Border, gDeg), borderStroke, geo);
            dc.Pop();
        }

        // Auto 3 / Auto 5 sit under the body like diep.io.
        if (!tank.IsBoss)
            DrawTurrets(dc, tank, t, scale, barrelStroke, orbitOnly: true);

        dc.PushTransform(new RotateTransform(deg));
        DrawPrePost(dc, tank.Class.PreAddon, tank, scale, deg, barrelStroke);
        foreach (var barrel in tank.Barrels)
            DrawBarrel(dc, barrel, scale, t, deg, barrelStroke, TankStats.BarrelDistance(tank, barrel.Def));
        DrawPrePost(dc, tank.Class.PostAddon, tank, scale, deg, barrelStroke);
        DrawBody(dc, tank, fill, stroke);
        dc.Pop();

        // Center auto-turrets + boss mounts draw on top.
        DrawTurrets(dc, tank, t, scale, barrelStroke, orbitOnly: tank.IsBoss ? null : false);
    }

    private void DrawNametag(DrawingContext dc, string text, double size, Color fill, double centerX, double y)
    {
        var label = _draw.Text(text, size, fill);
        var outline = _draw.Text(text, size, Colors.Black);
        var x = centerX - label.Width / 2;
        var o = Math.Max(1.0, size * 0.1);
        dc.DrawText(outline, new Point(x - o, y));
        dc.DrawText(outline, new Point(x + o, y));
        dc.DrawText(outline, new Point(x, y - o));
        dc.DrawText(outline, new Point(x, y + o));
        dc.DrawText(outline, new Point(x - o, y - o));
        dc.DrawText(outline, new Point(x + o, y - o));
        dc.DrawText(outline, new Point(x - o, y + o));
        dc.DrawText(outline, new Point(x + o, y + o));
        dc.DrawText(label, new Point(x, y));
    }

    private static void DrawBody(DrawingContext dc, TankEntity tank, Brush fill, Pen stroke)
    {
        var sides = tank.Class.Sides;
        if (sides <= 1)
            dc.DrawEllipse(fill, stroke, new Point(0, 0), tank.Radius, tank.Radius);
        else
        {
            // Triangles point along facing (crasher-style), matching Guardian/Defender.
            double? tip = sides == 3 ? 0 : null;
            dc.DrawGeometry(fill, stroke, DrawCache.RegularPolygon(sides, tank.Radius, tip));
        }
    }

    private void DrawBarrel(DrawingContext dc, BarrelState barrel, double scale, double t, double parentDeg, Pen stroke, double? distance = null)
    {
        var def = barrel.Def;
        var color = def.Addon == "purplebarrel" ? DiepColors.TeamPurple : DiepColors.Barrel;
        if (def.Addon == "purplebarrel")
            stroke = BodyStroke(DiepColors.TeamPurple);

        var length = Math.Max(0.5, barrel.DrawLength(t, scale));
        var width = Math.Max(0.5, def.Width * scale);
        var dist = (distance ?? def.Distance) * scale;
        var localDeg = parentDeg + def.Angle * 180 / Math.PI;
        var fill = BodyFill(color, localDeg);

        dc.PushTransform(new RotateTransform(def.Angle * 180 / Math.PI));
        dc.PushTransform(new TranslateTransform(dist, def.Offset * scale));

        if (_style == RenderStyle.Shaded)
        {
            RenderLooks.SoftPartShadow(dc, localDeg, width * 0.65, brush =>
                DrawBarrelShape(dc, brush, null, def, length, width));
        }

        DrawBarrelShape(dc, fill, stroke, def, length, width);

        if (def.Addon == "trapLauncher")
        {
            var launch = width * (20.0 / 42);
            dc.PushTransform(new TranslateTransform(length, 0));
            if (_style == RenderStyle.Shaded)
            {
                RenderLooks.SoftPartShadow(dc, localDeg, width * 0.5, brush =>
                    DrawTrapezoid(dc, brush, null, launch, width, width * 1.35));
            }
            DrawTrapezoid(dc, fill, stroke, launch, width, width * 1.35);
            dc.Pop();
        }

        dc.Pop();
        dc.Pop();
    }

    private void DrawBarrelShape(DrawingContext dc, Brush fill, Pen? stroke, BarrelDef def, double length, double width)
    {
        if (def.IsTrapezoid)
        {
            var invert = Math.Abs(Math2.NormalizeAngle(def.TrapezoidDirection)) > Math.PI / 2;
            var near = invert ? width * 1.45 : width;
            var far = invert ? width : width * 1.45;
            DrawTrapezoid(dc, fill, stroke, length, near, far);
        }
        else if (_style == RenderStyle.Old)
        {
            dc.DrawRectangle(fill, stroke, new Rect(0, -width / 2, length, width));
        }
        else
        {
            dc.DrawRoundedRectangle(fill, stroke, new Rect(0, -width / 2, length, width), 2.2, 2.2);
        }
    }

    private static void DrawTrapezoid(DrawingContext dc, Brush fill, Pen? stroke, double length, double nearW, double farW)
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

    private void DrawPrePost(DrawingContext dc, string? addon, TankEntity tank, double scale, double parentDeg, Pen barrelStroke)
    {
        switch (addon)
        {
            case "pronounced":
            {
                var size = tank.Radius;
                var width = 42 * scale;
                var offset = 40 * scale;
                dc.PushTransform(new TranslateTransform(offset - size / 2, 0));
                DrawAddonTrap(dc, size, width * 1.35, width, parentDeg, barrelStroke);
                dc.Pop();
                break;
            }
            case "dompronounced":
            {
                var size = 22 * scale;
                var width = 35 * scale;
                dc.PushTransform(new TranslateTransform(tank.Radius - size / 2, 0));
                DrawAddonTrap(dc, size, width * 1.35, width, parentDeg, barrelStroke);
                dc.Pop();
                break;
            }
            case "launcher":
            {
                var size = 65.5 * Math.Sqrt(2) * scale;
                var width = 33.6 * scale;
                DrawAddonTrap(dc, size, width * 1.45, width, parentDeg, barrelStroke);
                break;
            }
        }
    }

    private void DrawAddonTrap(DrawingContext dc, double length, double nearW, double farW, double parentDeg, Pen stroke)
    {
        var fill = BodyFill(DiepColors.Barrel, parentDeg);
        if (_style == RenderStyle.Shaded)
        {
            RenderLooks.SoftPartShadow(dc, parentDeg, Math.Max(nearW, farW) * 0.55, brush =>
                DrawTrapezoid(dc, brush, null, length, nearW, farW));
        }
        DrawTrapezoid(dc, fill, stroke, length, nearW, farW);
    }

    /// <param name="orbitOnly">
    /// true = only orbiting mounts (Auto 3/5), false = only centered mounts,
    /// null = all (bosses).
    /// </param>
    private void DrawTurrets(DrawingContext dc, TankEntity tank, double t, double scale, Pen barrelStroke, bool? orbitOnly)
    {
        var body = tank.DrawAngle(t);
        var rot = tank.IsBoss ? 0 : tank.DrawRotator(t);
        // Boss turrets: a bit under full AutoTurret size (not tiny, not huge).
        var turretScale = tank.IsBoss ? 0.62 : scale;
        foreach (var turret in tank.Turrets)
        {
            var isOrbit = turret.Orbit > 0.01;
            if (orbitOnly is { } only && only != isOrbit)
                continue;

            var a = tank.IsBoss ? body + turret.MountAngle : turret.MountAngle + rot;
            var r = tank.Radius * turret.Orbit;
            var tDeg = turret.DrawAngle(t) * 180 / Math.PI;
            dc.PushTransform(new TranslateTransform(Math.Cos(a) * r, Math.Sin(a) * r));
            dc.PushTransform(new RotateTransform(tDeg));
            DrawBarrel(dc, turret.Barrel, turretScale, t, tDeg, barrelStroke);
            var baseR = 25 * turretScale;
            if (_style == RenderStyle.Shaded)
            {
                RenderLooks.SoftPartShadow(dc, tDeg, baseR, brush =>
                    dc.DrawEllipse(brush, null, new Point(0, 0), baseR, baseR));
            }
            dc.DrawEllipse(BodyFill(DiepColors.Barrel, tDeg), barrelStroke, new Point(0, 0), baseR, baseR);
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
        if (_style == RenderStyle.Shaded)
            RenderLooks.SoftShadow(dc, b.Radius);

        var deg = b.DrawAngle(t) * 180 / Math.PI;
        var barrelStroke = BodyStroke(DiepColors.Barrel, bullet: true);
        DrawBulletCore(dc, b, t, deg,
            BodyFill(b.Fill, deg), BodyStroke(b.Fill, bullet: true), barrelStroke);

        if (opacity < 0.999)
            dc.Pop();
        PopDeath(dc);
    }

    private void DrawBulletCore(DrawingContext dc, BulletEntity b, double t, double deg, Brush fill, Pen stroke, Pen barrelStroke)
    {
        var scale = b.Radius / 50.0;
        dc.PushTransform(new RotateTransform(deg));
        foreach (var gun in b.Guns)
            DrawBarrel(dc, gun, scale, t, deg, barrelStroke);
        if (b.IsStar)
            dc.DrawGeometry(fill, stroke, DrawCache.Star(3, b.Radius));
        else if (b.Sides <= 1)
            dc.DrawEllipse(fill, stroke, new Point(0, 0), b.Radius, b.Radius);
        else if (b.Sides == 3)
            // Tip along +X so DrawAngle aims the point at the target (same as crashers).
            dc.DrawGeometry(fill, stroke, DrawCache.RegularPolygon(3, b.Radius, 0));
        else
            dc.DrawGeometry(fill, stroke, DrawCache.RegularPolygon(b.Sides, b.Radius));
        dc.Pop();
    }

    private void DrawShape(DrawingContext dc, ShapeEntity s, double t, double bars)
    {
        var x = s.DrawX(t);
        var y = s.DrawY(t);
        var fillColor = DiepColors.Hit(s.Fill, s.Flash.Draw(t));
        var geo = DrawCache.Polygon(s.Kind, s.Radius);
        var deg = s.DrawAngle(t) * 180 / Math.PI;
        PushDeath(dc, s.Destroy, x, y, t);
        if (_style == RenderStyle.Shaded)
            RenderLooks.SoftShadow(dc, s.Radius);
        dc.PushTransform(new RotateTransform(deg));
        dc.DrawGeometry(BodyFill(fillColor, deg), BodyStroke(fillColor), geo);
        dc.Pop();
        PopDeath(dc);
        if (!s.Destroy.Active && s.Health < s.MaxHealth - 0.2)
            DrawBar(dc, x, y + s.Radius + 8, BarWidth(s.Radius), 5.5, s.DrawHealthRatio(bars), DiepColors.Health);
    }

    private Brush BodyFill(Color color, double localRotationDeg = 0) =>
        RenderLooks.Fill(_draw, color, _style, localRotationDeg);

    private Pen BodyStroke(Color fill, bool bullet = false) =>
        _draw.Pen(RenderLooks.Outline(fill, _style), RenderLooks.StrokeWidth(_style, bullet));

    private static double BarWidth(double radius) => Math.Clamp(radius * 2.2, 36, 120);

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
        const double outline = 1.6;
        var totalW = width + outline * 2;
        var totalH = height + outline * 2;
        var x = cx - totalW / 2;
        // Capsule ends (half-height radius), like diep health bars.
        var outerR = totalH * 0.5;
        dc.DrawRoundedRectangle(_draw.Brush(DiepColors.HealthBack), null, new Rect(x, y, totalW, totalH), outerR, outerR);
        var ix = x + outline;
        var iy = y + outline;
        var innerR = height * 0.5;
        dc.DrawRoundedRectangle(_draw.Brush(DiepColors.HealthBack), null, new Rect(ix, iy, width, height), innerR, innerR);
        if (ratio > 0.001)
        {
            var fillW = Math.Max(0.5, width * ratio);
            var fillR = Math.Min(innerR, fillW * 0.5);
            dc.DrawRoundedRectangle(_draw.Brush(fill), null, new Rect(ix, iy, fillW, height), fillR, fillR);
        }
    }
}
