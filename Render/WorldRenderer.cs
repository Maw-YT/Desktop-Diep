using System.Windows;
using System.Windows.Media;

namespace DesktopDiep;

internal sealed class WorldRenderer
{
    private readonly DrawCache _draw;
    private RenderStyle _style = RenderStyle.New;
    private GameWorld? _world;

    public WorldRenderer(DrawCache draw) => _draw = draw;

    public void Draw(DrawingContext dc, GameWorld world)
    {
        _style = RenderLooks.Normalize(world.RenderStyle);
        _world = world;
        var t = world.DrawAlpha;
        var bars = world.Alpha;
        foreach (var obj in world.Objects)
        {
            if (obj.Alive)
                DrawObject(dc, obj, t);
        }
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
        var deg = tank.DrawAngle(t) * 180 / Math.PI;
        var strokeW = DiepView.Stroke(tank.Radius);
        var shade = ShadeOf("tank", x, y, tank.Radius, tank.Team, tank.Shader, tank.Fill);
        if (shade.Opacity < 0.999)
            dc.PushOpacity(shade.Opacity);
        DrawGlow(dc, shade, tank.Radius);
        var fill = BodyFill(DiepColors.Hit(shade.Color, flash), deg);
        var stroke = HitPen(shade.Color, flash, strokeW);
        var barrelStroke = HitPen(DiepColors.Barrel, flash, strokeW);
        var borderStroke = HitPen(DiepColors.Border, flash, strokeW);
        var scale = TankStats.GunScale(tank);

        DrawTankCore(dc, tank, t, scale, deg, flash, fill, stroke, strokeW, barrelStroke, borderStroke);

        if (selected && !dying)
            dc.DrawEllipse(null, _draw.Pen(Colors.White, 1.6), new Point(0, 0), tank.Radius + DiepView.Px(6), tank.Radius + DiepView.Px(6));
        if (shade.Opacity < 0.999)
            dc.Pop();
        PopDeath(dc);

        if (dying)
            return;
        const double nameSize = 16;
        var nameY = y - tank.Radius - 28;
        var barW = Math.Max(52, tank.Radius * 1.55);
        if (tank.IsBoss || tank.IsMothership)
        {
            if (tank.DrawHealthRatio(bars) < 0.995)
                DrawBar(dc, x, y + tank.Radius + 8, barW, 8, tank.DrawHealthRatio(bars), DiepColors.Health);
            DrawNametag(dc, tank.BossAltName ?? tank.Class.Name, nameSize, Colors.White, x, nameY);
        }
        else if (!tank.IsArenaCloser)
        {
            if (tank.DrawHealthRatio(bars) < 0.995)
                DrawBar(dc, x, y + tank.Radius + 8, barW, 8, tank.DrawHealthRatio(bars), DiepColors.Health);
            DrawBar(dc, x, y + tank.Radius + 20, barW, 4, tank.DrawXpRatio(bars), DiepColors.Xp);
            DrawNametag(dc, $"{tank.Class.Name}  Lv {tank.Level}", nameSize, Colors.White, x, nameY);
        }
        else
        {
            DrawNametag(dc, "Arena Closer", nameSize, Colors.White, x, nameY);
        }
    }

    private void DrawTankCore(
        DrawingContext dc, TankEntity tank, double t, double scale, double deg, double flash,
        Brush fill, Pen stroke, double strokeW, Pen barrelStroke, Pen borderStroke)
    {
        foreach (var guard in tank.Guards)
        {
            var gDeg = guard.DrawAngle(t) * 180 / Math.PI;
            var guardR = Inset(_style, tank.Radius * guard.SizeRatio, strokeW);
            var geo = DrawCache.RegularPolygon(guard.Sides, guardR, null, Chamfer(guard.Sides, guardR));
            dc.PushTransform(new RotateTransform(gDeg));
            if (_style == RenderStyle.Shaded)
            {
                RenderLooks.SoftPartShadow(dc, gDeg, tank.Radius * guard.SizeRatio,
                    brush => dc.DrawGeometry(brush, null, geo));
            }
            dc.DrawGeometry(BodyFill(DiepColors.Hit(DiepColors.Border, flash), gDeg), borderStroke, geo);
            dc.Pop();
        }

        // Auto 3 / Auto 5 sit under the body like diep.io.
        if (!tank.IsBoss)
            DrawTurrets(dc, tank, t, scale, flash, barrelStroke, orbitOnly: true);

        dc.PushTransform(new RotateTransform(deg));
        DrawPrePost(dc, tank.Class.PreAddon, tank, scale, deg, flash, barrelStroke);
        foreach (var barrel in tank.Barrels)
            DrawBarrel(dc, barrel, scale, t, deg, barrelStroke, flash, barrel.Def.Distance);
        DrawPrePost(dc, tank.Class.PostAddon, tank, scale, deg, flash, barrelStroke);
        DrawBody(dc, tank, fill, stroke, strokeW);
        dc.Pop();

        // Center auto-turrets + boss mounts draw on top.
        DrawTurrets(dc, tank, t, scale, flash, barrelStroke, orbitOnly: tank.IsBoss ? null : false);
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

    private void DrawBody(DrawingContext dc, TankEntity tank, Brush fill, Pen stroke, double strokeW)
    {
        var sides = tank.Class.Sides;
        if (sides <= 1)
        {
            var r = InsetCircle(tank.Radius, strokeW);
            dc.DrawEllipse(fill, stroke, new Point(0, 0), r, r);
        }
        else
        {
            var rad = PolyRadius(tank.Radius, strokeW);
            dc.DrawGeometry(fill, stroke, DrawCache.RegularPolygon(sides, rad, sides == 3 ? 0 : null, Chamfer(sides, rad)));
        }
    }

    private void DrawBarrel(DrawingContext dc, BarrelState barrel, double scale, double t, double parentDeg, Pen stroke, double flash = 0, double? distance = null)
    {
        var def = barrel.Def;
        var color = def.Addon == "purplebarrel" ? DiepColors.TeamPurple : DiepColors.Barrel;
        var length = Math.Max(0.5, barrel.DrawLength(t, scale));
        var width = Math.Max(0.5, def.Width * scale);
        var thick = def.Addon == "purplebarrel" ? DiepView.Stroke(width) : stroke.Thickness;
        stroke = HitPen(color, flash, thick);
        var dist = (distance ?? def.Distance) * scale;
        var localDeg = parentDeg + def.Angle * 180 / Math.PI;
        var fill = BodyFill(DiepColors.Hit(color, flash), localDeg);

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
        var tip = 1.0;
        if (def.IsTrapezoid)
        {
            tip = 1 + 0.5 * Math.Cos(def.TrapezoidDirection);
            if (tip < 0.2)
                tip = 0.2;
        }
        var chamfer = _style == RenderStyle.Old ? 0 : Math.Min(length, width * 0.5) * 0.16;
        DrawTrapezoid(dc, fill, stroke, length, width, width * tip, chamfer);
    }

    private static void DrawTrapezoid(DrawingContext dc, Brush fill, Pen? stroke, double length, double nearW, double farW, double chamfer = 0)
    {
        if (length <= 0 || nearW <= 0 || farW <= 0)
            return;
        Point[] pts =
        [
            new(0, -nearW / 2),
            new(length, -farW / 2),
            new(length, farW / 2),
            new(0, nearW / 2)
        ];
        dc.DrawGeometry(fill, stroke, DrawCache.Chamfered(pts, chamfer));
    }

    private void DrawPrePost(DrawingContext dc, string? addon, TankEntity tank, double scale, double parentDeg, double flash, Pen barrelStroke)
    {
        switch (addon)
        {
            case "pronounced":
            {
                var size = tank.Radius;
                var width = 42 * scale;
                var offset = 40 * scale;
                dc.PushTransform(new TranslateTransform(offset - size / 2, 0));
                DrawAddonTrap(dc, size, width * 1.35, width, parentDeg, flash, barrelStroke);
                dc.Pop();
                break;
            }
            case "dompronounced":
            {
                var size = 22 * scale;
                var width = 35 * scale;
                dc.PushTransform(new TranslateTransform(tank.Radius - size / 2, 0));
                DrawAddonTrap(dc, size, width * 1.35, width, parentDeg, flash, barrelStroke);
                dc.Pop();
                break;
            }
            case "launcher":
            {
                var size = 65.5 * Math.Sqrt(2) * scale;
                var width = 33.6 * scale;
                DrawAddonTrap(dc, size, width * 1.45, width, parentDeg, flash, barrelStroke);
                break;
            }
        }
    }

    private void DrawAddonTrap(DrawingContext dc, double length, double nearW, double farW, double parentDeg, double flash, Pen stroke)
    {
        var fill = BodyFill(DiepColors.Hit(DiepColors.Barrel, flash), parentDeg);
        var pen = HitPen(DiepColors.Barrel, flash, stroke.Thickness);
        if (_style == RenderStyle.Shaded)
        {
            RenderLooks.SoftPartShadow(dc, parentDeg, Math.Max(nearW, farW) * 0.55, brush =>
                DrawTrapezoid(dc, brush, null, length, nearW, farW));
        }
        DrawTrapezoid(dc, fill, pen, length, nearW, farW);
    }

    /// <param name="orbitOnly">
    /// true = only orbiting mounts (Auto 3/5), false = only centered mounts,
    /// null = all (bosses).
    /// </param>
    private void DrawTurrets(DrawingContext dc, TankEntity tank, double t, double scale, double flash, Pen barrelStroke, bool? orbitOnly)
    {
        var body = tank.DrawAngle(t);
        var rot = tank.IsBoss ? 0 : tank.DrawRotator(t);
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
            DrawBarrel(dc, turret.Barrel, scale, t, tDeg, barrelStroke, flash);
            var baseR = InsetCircle(25 * scale, DiepView.Stroke(25 * scale));
            if (_style == RenderStyle.Shaded)
            {
                RenderLooks.SoftPartShadow(dc, tDeg, baseR, brush =>
                    dc.DrawEllipse(brush, null, new Point(0, 0), baseR, baseR));
            }
            dc.DrawEllipse(BodyFill(DiepColors.Hit(DiepColors.Barrel, flash), tDeg), barrelStroke, new Point(0, 0), baseR, baseR);
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
        var flash = b.Flash.Draw(t);
        var strokeW = DiepView.Stroke(b.Radius, bullet: true);
        var shade = ShadeOf("bullet", x, y, b.Radius, b.Team, b.Shader, b.Fill);
        if (shade.Opacity < 0.999)
            dc.PushOpacity(shade.Opacity);
        DrawGlow(dc, shade, b.Radius);
        var barrelStroke = HitPen(DiepColors.Barrel, flash, strokeW);
        DrawBulletCore(dc, b, t, deg, flash,
            BodyFill(DiepColors.Hit(shade.Color, flash), deg), HitPen(shade.Color, flash, strokeW), barrelStroke, strokeW);
        if (shade.Opacity < 0.999)
            dc.Pop();

        if (opacity < 0.999)
            dc.Pop();
        PopDeath(dc);
    }

    private void DrawBulletCore(DrawingContext dc, BulletEntity b, double t, double deg, double flash, Brush fill, Pen stroke, Pen barrelStroke, double strokeW)
    {
        var scale = b.Radius / 50.0;
        dc.PushTransform(new RotateTransform(deg));
        foreach (var gun in b.Guns)
            DrawBarrel(dc, gun, scale, t, deg, barrelStroke, flash);
        if (b.IsStar)
            dc.DrawGeometry(fill, stroke, DrawCache.Star(3, PolyRadius(b.Radius, strokeW)));
        else if (b.Sides <= 1)
        {
            var r = InsetCircle(b.Radius, strokeW);
            dc.DrawEllipse(fill, stroke, new Point(0, 0), r, r);
        }
        else
        {
            var rad = PolyRadius(b.Radius, strokeW);
            var tip = b.Sides == 3 ? 0 : (double?)null;
            dc.DrawGeometry(fill, stroke, DrawCache.RegularPolygon(b.Sides, rad, tip, Chamfer(b.Sides, rad)));
        }
        dc.Pop();
    }

    private void DrawShape(DrawingContext dc, ShapeEntity s, double t, double bars)
    {
        var x = s.DrawX(t);
        var y = s.DrawY(t);
        var flash = s.Flash.Draw(t);
        var shade = ShadeOf("shape", x, y, s.Radius, s.Team, s.Shader, s.Fill);
        var fillColor = DiepColors.Hit(shade.Color, flash);
        var strokeW = DiepView.Stroke(s.Radius);
        var sides = s.Sides > 2
            ? s.Sides
            : s.Kind is ShapeKind.Pentagon or ShapeKind.AlphaPentagon ? 5 : s.Kind == ShapeKind.Square ? 4 : 3;
        var rad = PolyRadius(s.Radius, strokeW);
        var geo = DrawCache.RegularPolygon(sides, rad, s.Kind == ShapeKind.Crasher ? 0 : null, Chamfer(sides, rad));
        var deg = s.DrawAngle(t) * 180 / Math.PI;
        PushDeath(dc, s.Destroy, x, y, t);
        if (shade.Opacity < 0.999)
            dc.PushOpacity(shade.Opacity);
        if (_style == RenderStyle.Shaded)
            RenderLooks.SoftShadow(dc, s.Radius);
        DrawGlow(dc, shade, s.Radius);
        dc.PushTransform(new RotateTransform(deg));
        dc.DrawGeometry(BodyFill(fillColor, deg), HitPen(shade.Color, flash, strokeW), geo);
        dc.Pop();
        if (shade.Opacity < 0.999)
            dc.Pop();
        PopDeath(dc);
        if (!s.Destroy.Active && s.Health < s.MaxHealth * 0.995)
            DrawBar(dc, x, y + s.Radius + 8, Math.Max(52, s.Radius * 1.55), 8, s.DrawHealthRatio(bars), DiepColors.Health);
    }

    private Brush BodyFill(Color color, double localRotationDeg = 0) =>
        RenderLooks.Fill(_draw, color, _style, localRotationDeg);

    private Pen StrokePen(Color fill, double thickness) =>
        _draw.Pen(RenderLooks.Outline(fill, _style), thickness);

    private Pen HitPen(Color fill, double flash, double thickness) =>
        StrokePen(DiepColors.Hit(RenderLooks.Outline(fill, _style), flash), thickness);

    private double PolyRadius(double physics, double stroke)
    {
        var rad = physics * Math.Sqrt(2);
        return _style == RenderStyle.Old ? rad : Math.Max(rad - stroke * 0.5, rad * 0.72);
    }

    private static double InsetCircle(double radius, double stroke) =>
        Math.Max(radius - stroke * 0.5, radius * 0.72);

    private static double Inset(RenderStyle style, double radius, double stroke) =>
        style == RenderStyle.Old ? radius : Math.Max(radius - stroke * 0.5, radius * 0.72);

    private double Chamfer(int sides, double radius) =>
        _style == RenderStyle.Old ? 0 : radius * (sides == 3 ? 0.045 : sides <= 4 ? 0.11 : 0.08);

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

    private void DrawObject(DrawingContext dc, ArenaObject obj, double t)
    {
        var x = obj.DrawX(t);
        var y = obj.DrawY(t);
        var shade = ShadeOf(obj.Kind, x, y, obj.Circle ? obj.Radius : Math.Max(obj.W, obj.H) * 0.5, obj.Team, obj.Shader, obj.Fill);
        var color = shade.Color;
        color.A = (byte)(255 * Math.Clamp(obj.Opacity * shade.Opacity, 0, 1));
        var brush = _draw.Brush(color);
        dc.PushTransform(new TranslateTransform(x, y));
        DrawGlow(dc, shade, obj.Circle ? obj.Radius : Math.Min(obj.W, obj.H) * 0.5);
        if (obj.Circle)
        {
            var sides = obj.Sides > 2 ? obj.Sides : 0;
            if (sides > 2)
            {
                dc.PushTransform(new RotateTransform(obj.Angle * 180 / Math.PI));
                dc.DrawGeometry(brush, null, DrawCache.RegularPolygon(sides, obj.Radius, null, 0));
                dc.Pop();
            }
            else
                dc.DrawEllipse(brush, null, new Point(0, 0), obj.Radius, obj.Radius);
        }
        else
        {
            dc.DrawRectangle(brush, null, new Rect(-obj.W * 0.5, -obj.H * 0.5, obj.W, obj.H));
        }
        dc.Pop();
        if (obj.MaxHealth > 0 && obj.Health < obj.MaxHealth * 0.995)
            DrawBar(dc, x, y + (obj.Circle ? obj.Radius : obj.H * 0.5) + 8, 70, 8, obj.Health / obj.MaxHealth, DiepColors.Health);
        if (obj.Label.Length > 0 && obj.Kind is "base" or "dominator")
        {
            var text = obj.Kind == "dominator" && obj.Team != 0
                ? $"{Teams.Name(obj.Team)} {obj.Label}"
                : obj.Label;
            DrawNametag(dc, text, 14, Colors.White, x, y);
        }
    }

    private ShadeResult ShadeOf(string kind, double x, double y, double radius, int team, string? shader, Color color)
    {
        var id = string.IsNullOrEmpty(shader) ? _world?.ScreenShader : shader;
        var time = _world is null ? 0 : _world.Debug.Tick * GameWorld.TickDt;
        return ModContent.Shade(id, kind, x, y, time, radius, team, color);
    }

    private void DrawGlow(DrawingContext dc, ShadeResult shade, double radius)
    {
        if (shade.Glow <= 0)
            return;
        var glow = Color.FromArgb(72, shade.Color.R, shade.Color.G, shade.Color.B);
        dc.DrawEllipse(_draw.Brush(glow), null, new Point(0, 0), radius + shade.Glow, radius + shade.Glow);
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
