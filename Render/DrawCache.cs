using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DesktopDiep;

internal sealed class DrawCache
{
    private readonly Dictionary<Color, SolidColorBrush> _brushes = [];
    private readonly Dictionary<(Color, int), Pen> _pens = [];
    private readonly Typeface _typeface = new(new FontFamily("Consolas, Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
    private readonly Visual _dpiOwner;

    public DrawCache(Visual dpiOwner) => _dpiOwner = dpiOwner;

    public SolidColorBrush Brush(Color c)
    {
        if (_brushes.TryGetValue(c, out var b))
            return b;
        b = new SolidColorBrush(c);
        b.Freeze();
        _brushes[c] = b;
        return b;
    }

    public Pen Pen(Color c, double thickness)
    {
        var key = (c, (int)(thickness * 10));
        if (_pens.TryGetValue(key, out var p))
            return p;
        p = new Pen(Brush(c), thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        p.Freeze();
        _pens[key] = p;
        return p;
    }

    public FormattedText Text(string text, double size, Color color)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            _typeface,
            size,
            Brush(color),
            VisualTreeHelper.GetDpi(_dpiOwner).PixelsPerDip);
    }

    public static Geometry RegularPolygon(int sides, double radius, double? vertexOffset = null)
    {
        var g = new StreamGeometry();
        using var ctx = g.Open();
        var offset = vertexOffset ?? (sides == 4 ? Math.PI / 4 : -Math.PI / 2);
        for (var i = 0; i < sides; i++)
        {
            var a = offset + i * (Math.PI * 2 / sides);
            var p = new Point(Math.Cos(a) * radius, Math.Sin(a) * radius);
            if (i == 0) ctx.BeginFigure(p, true, true);
            else ctx.LineTo(p, true, false);
        }
        g.Freeze();
        return g;
    }

    public static Geometry Star(int points, double radius)
    {
        var g = new StreamGeometry();
        using var ctx = g.Open();
        var inner = radius * 0.45;
        for (var i = 0; i < points * 2; i++)
        {
            var a = -Math.PI / 2 + i * (Math.PI / points);
            var r = (i & 1) == 0 ? radius : inner;
            var p = new Point(Math.Cos(a) * r, Math.Sin(a) * r);
            if (i == 0) ctx.BeginFigure(p, true, true);
            else ctx.LineTo(p, true, false);
        }
        g.Freeze();
        return g;
    }

    public static Geometry Polygon(ShapeKind kind, double radius)
    {
        var sides = kind switch
        {
            ShapeKind.Crasher => 3,
            ShapeKind.Triangle => 3,
            ShapeKind.Pentagon or ShapeKind.AlphaPentagon => 5,
            _ => 4
        };
        return RegularPolygon(sides, radius, kind == ShapeKind.Crasher ? 0 : null);
    }
}
