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

    public static Geometry RegularPolygon(int sides, double radius, double? vertexOffset = null, double chamfer = 0)
    {
        if (sides < 3)
            sides = 3;
        var offset = vertexOffset ?? (sides == 3 ? 0 : sides % 2 == 0 ? Math.PI / sides : 0);
        var pts = new Point[sides];
        for (var i = 0; i < sides; i++)
        {
            var a = offset + i * (Math.PI * 2 / sides);
            pts[i] = new Point(Math.Cos(a) * radius, Math.Sin(a) * radius);
        }
        return chamfer > 0.4 ? Chamfered(pts, chamfer) : Closed(pts);
    }

    public static Geometry Chamfered(IReadOnlyList<Point> pts, double radius)
    {
        var n = pts.Count;
        if (n < 3 || radius <= 0.4)
            return Closed(pts);
        var cut = new List<Point>(n * 2);
        for (var i = 0; i < n; i++)
        {
            var prev = pts[(i - 1 + n) % n];
            var cur = pts[i];
            var next = pts[(i + 1) % n];
            var dpx = cur.X - prev.X;
            var dpy = cur.Y - prev.Y;
            var dnx = next.X - cur.X;
            var dny = next.Y - cur.Y;
            var lp = Math.Sqrt(dpx * dpx + dpy * dpy);
            var ln = Math.Sqrt(dnx * dnx + dny * dny);
            if (lp < 1e-3 || ln < 1e-3)
            {
                cut.Add(cur);
                continue;
            }
            var r = Math.Min(radius, Math.Min(lp, ln) * 0.32);
            cut.Add(new Point(cur.X - dpx / lp * r, cur.Y - dpy / lp * r));
            cut.Add(new Point(cur.X + dnx / ln * r, cur.Y + dny / ln * r));
        }
        return Closed(cut);
    }

    private static Geometry Closed(IReadOnlyList<Point> pts)
    {
        var g = new StreamGeometry { FillRule = FillRule.Nonzero };
        using var ctx = g.Open();
        ctx.BeginFigure(pts[0], true, true);
        for (var i = 1; i < pts.Count; i++)
            ctx.LineTo(pts[i], true, false);
        g.Freeze();
        return g;
    }

    public static Geometry Star(int points, double radius)
    {
        var g = new StreamGeometry();
        using var ctx = g.Open();
        var inner = radius * 0.38;
        for (var i = 0; i < points * 2; i++)
        {
            var a = i * (Math.PI / points);
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
