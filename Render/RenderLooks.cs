using System.Windows;
using System.Windows.Media;

namespace DesktopDiep;

public enum RenderStyle
{
    Old = 0,
    New = 1,
    Shaded = 2
}

internal static class RenderLooks
{
    // Fixed screen-space light from upper-left.
    private const double LightX = 0.30;
    private const double LightY = 0.26;

    public static string Label(RenderStyle style) => style switch
    {
        RenderStyle.Old => "Old",
        RenderStyle.New => "New",
        RenderStyle.Shaded => "Shaded",
        _ => "New"
    };

    public static RenderStyle Normalize(RenderStyle style) => style switch
    {
        RenderStyle.Old or RenderStyle.New or RenderStyle.Shaded => style,
        _ => RenderStyle.Shaded // legacy 3D
    };

    public static Color Outline(Color fill, RenderStyle style) => style switch
    {
        RenderStyle.Old => DiepColors.Border,
        _ => DiepColors.Stroke(fill)
    };

    public static double StrokeWidth(RenderStyle style, bool bullet = false) => style switch
    {
        RenderStyle.Old => bullet ? 2.8 : 4.0,
        _ => bullet ? 2.2 : 3.2
    };

    public static Brush Flat(DrawCache draw, Color color) => draw.Brush(color);

    /// <param name="localRotationDeg">
    /// DrawingContext rotation on the geometry. Counter-rotated so the highlight
    /// stays fixed in screen space.
    /// </param>
    public static Brush ShadedFill(Color color, double localRotationDeg)
    {
        var light = DiepColors.Mix(color, Colors.White, 0.42);
        var mid = color;
        var dark = DiepColors.Mix(color, Colors.Black, 0.38);
        var brush = new RadialGradientBrush
        {
            GradientOrigin = new Point(LightX, LightY),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.92,
            RadiusY = 0.92,
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            RelativeTransform = new RotateTransform(-localRotationDeg, 0.5, 0.5)
        };
        brush.GradientStops.Add(new GradientStop(light, 0));
        brush.GradientStops.Add(new GradientStop(mid, 0.42));
        brush.GradientStops.Add(new GradientStop(dark, 1));
        brush.Freeze();
        return brush;
    }

    public static Brush Fill(DrawCache draw, Color color, RenderStyle style, double localRotationDeg = 0) =>
        style == RenderStyle.Shaded ? ShadedFill(color, localRotationDeg) : Flat(draw, color);

    /// <summary>
    /// Convert a screen-space drop offset into local space under <paramref name="localRotationDeg"/>.
    /// </summary>
    public static Point ShadowOffsetLocal(double localRotationDeg, double amount)
    {
        var ox = amount * 0.85;
        var oy = amount;
        var rad = -localRotationDeg * Math.PI / 180;
        var c = Math.Cos(rad);
        var s = Math.Sin(rad);
        return new Point(ox * c - oy * s, ox * s + oy * c);
    }

    public static Brush ShadowBrush(byte alpha)
    {
        var brush = new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.5),
            Center = new Point(0.5, 0.5),
            RadiusX = 0.5,
            RadiusY = 0.5,
            MappingMode = BrushMappingMode.RelativeToBoundingBox
        };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 0, 0, 0), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(alpha * 0.55), 0, 0, 0), 0.45));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));
        brush.Freeze();
        return brush;
    }

    public static Brush ShadowFlat(byte alpha)
    {
        var b = new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0));
        b.Freeze();
        return b;
    }

    /// <summary>Layered soft drop shadow in local entity space (center at origin).</summary>
    public static void SoftShadow(DrawingContext dc, double radius)
    {
        DrawShadowLayer(dc, radius, 0.38, 0.48, 1.55, 1.28, 48);
        DrawShadowLayer(dc, radius, 0.28, 0.36, 1.28, 1.05, 85);
        DrawShadowLayer(dc, radius, 0.18, 0.24, 1.02, 0.85, 120);
    }

    private static void DrawShadowLayer(
        DrawingContext dc, double radius,
        double oxRatio, double oyRatio, double rxRatio, double ryRatio, byte alpha)
    {
        var ox = radius * oxRatio;
        var oy = radius * oyRatio;
        dc.DrawEllipse(ShadowBrush(alpha), null, new Point(ox, oy), radius * rxRatio, radius * ryRatio);
    }

    /// <summary>
    /// Soft silhouette shadow for a part already in a rotated local space.
    /// Draws <paramref name="draw"/> at a screen-fixed offset.
    /// </summary>
    public static void SoftPartShadow(DrawingContext dc, double localRotationDeg, double size, Action<Brush> draw)
    {
        // Outer soft pass
        var outer = ShadowOffsetLocal(localRotationDeg, size * 0.42);
        dc.PushTransform(new TranslateTransform(outer.X, outer.Y));
        draw(ShadowFlat(40));
        dc.Pop();

        var mid = ShadowOffsetLocal(localRotationDeg, size * 0.28);
        dc.PushTransform(new TranslateTransform(mid.X, mid.Y));
        draw(ShadowFlat(70));
        dc.Pop();

        var core = ShadowOffsetLocal(localRotationDeg, size * 0.16);
        dc.PushTransform(new TranslateTransform(core.X, core.Y));
        draw(ShadowFlat(100));
        dc.Pop();
    }
}
