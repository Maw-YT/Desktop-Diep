using System.Windows;
using System.Windows.Media;

namespace DesktopDiep;

/// <summary>Per-frame immediate-mode draw list for Lua mods (cleared every render).</summary>
internal sealed class ModDraw
{
    private const int MaxCommands = 2500;
    private readonly List<Cmd> _cmds = new(256);

    public int Count => _cmds.Count;

    public void Clear() => _cmds.Clear();

    public void Line(double x1, double y1, double x2, double y2, Color color, double width)
    {
        if (_cmds.Count >= MaxCommands) return;
        _cmds.Add(new Cmd { Kind = CmdKind.Line, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Width = width, Color = color });
    }

    public void Circle(double x, double y, double radius, Color color, bool filled)
    {
        if (_cmds.Count >= MaxCommands) return;
        _cmds.Add(new Cmd { Kind = CmdKind.Circle, X1 = x, Y1 = y, Radius = radius, Filled = filled, Color = color });
    }

    public void Glow(double x, double y, double radius, Color color)
    {
        if (_cmds.Count >= MaxCommands) return;
        _cmds.Add(new Cmd { Kind = CmdKind.Glow, X1 = x, Y1 = y, Radius = radius, Color = color });
    }

    /// <summary>Thick rounded segment with soft outer glow layers — generic beam look, not a projectile type.</summary>
    public void Beam(double x1, double y1, double x2, double y2, double width, Color color)
    {
        if (_cmds.Count >= MaxCommands) return;
        _cmds.Add(new Cmd { Kind = CmdKind.Beam, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Width = Math.Max(1, width), Color = color });
    }

    public void Rect(double x, double y, double w, double h, Color color, bool filled, double stroke)
    {
        if (_cmds.Count >= MaxCommands) return;
        _cmds.Add(new Cmd { Kind = CmdKind.Rect, X1 = x, Y1 = y, X2 = w, Y2 = h, Filled = filled, Width = stroke, Color = color });
    }

    public void Triangle(double x1, double y1, double x2, double y2, double x3, double y3, Color color, bool filled)
    {
        if (_cmds.Count >= MaxCommands) return;
        _cmds.Add(new Cmd
        {
            Kind = CmdKind.Poly,
            Points = [new Point(x1, y1), new Point(x2, y2), new Point(x3, y3)],
            Filled = filled,
            Color = color,
            Width = 2
        });
    }

    public void Poly(Point[] points, Color color, bool filled, double stroke)
    {
        if (_cmds.Count >= MaxCommands || points.Length < 2) return;
        _cmds.Add(new Cmd { Kind = CmdKind.Poly, Points = points, Filled = filled, Color = color, Width = stroke });
    }

    public void Ring(double x, double y, double inner, double outer, Color color)
    {
        if (_cmds.Count >= MaxCommands) return;
        _cmds.Add(new Cmd { Kind = CmdKind.Ring, X1 = x, Y1 = y, Radius = inner, Width = outer, Color = color });
    }

    public void Text(double x, double y, string text, double size, Color color)
    {
        if (_cmds.Count >= MaxCommands || string.IsNullOrEmpty(text)) return;
        _cmds.Add(new Cmd { Kind = CmdKind.Text, X1 = x, Y1 = y, Radius = Math.Max(8, size), Color = color, Text = text });
    }

    public void Render(DrawingContext dc, DrawCache draw)
    {
        foreach (var c in _cmds)
        {
            switch (c.Kind)
            {
                case CmdKind.Line:
                    dc.DrawLine(draw.Pen(c.Color, c.Width), new Point(c.X1, c.Y1), new Point(c.X2, c.Y2));
                    break;
                case CmdKind.Circle:
                    if (c.Filled)
                        dc.DrawEllipse(draw.Brush(c.Color), null, new Point(c.X1, c.Y1), c.Radius, c.Radius);
                    else
                        dc.DrawEllipse(null, draw.Pen(c.Color, 2), new Point(c.X1, c.Y1), c.Radius, c.Radius);
                    break;
                case CmdKind.Glow:
                    DrawGlow(dc, draw, c.X1, c.Y1, c.Radius, c.Color);
                    break;
                case CmdKind.Beam:
                    DrawBeam(dc, draw, c.X1, c.Y1, c.X2, c.Y2, c.Width, c.Color);
                    break;
                case CmdKind.Rect:
                    var rect = new Rect(c.X1, c.Y1, Math.Max(0, c.X2), Math.Max(0, c.Y2));
                    if (c.Filled)
                        dc.DrawRectangle(draw.Brush(c.Color), null, rect);
                    else
                        dc.DrawRectangle(null, draw.Pen(c.Color, Math.Max(1, c.Width)), rect);
                    break;
                case CmdKind.Poly:
                    DrawPoly(dc, draw, c.Points, c.Color, c.Filled, c.Width);
                    break;
                case CmdKind.Ring:
                    DrawRing(dc, draw, c.X1, c.Y1, c.Radius, c.Width, c.Color);
                    break;
                case CmdKind.Text:
                    dc.DrawText(draw.Text(c.Text ?? "", c.Radius, c.Color), new Point(c.X1, c.Y1));
                    break;
            }
        }
    }

    private static void DrawPoly(DrawingContext dc, DrawCache draw, Point[]? pts, Color color, bool filled, double stroke)
    {
        if (pts is null || pts.Length < 2)
            return;
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(pts[0], true, true);
            for (var i = 1; i < pts.Length; i++)
                ctx.LineTo(pts[i], true, false);
        }
        g.Freeze();
        if (filled)
            dc.DrawGeometry(draw.Brush(color), null, g);
        else
            dc.DrawGeometry(null, draw.Pen(color, Math.Max(1, stroke)), g);
    }

    private static void DrawRing(DrawingContext dc, DrawCache draw, double x, double y, double inner, double outer, Color color)
    {
        var rOut = Math.Max(inner, outer);
        var rIn = Math.Min(inner, outer);
        var g = new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new EllipseGeometry(new Point(x, y), rOut, rOut),
            new EllipseGeometry(new Point(x, y), rIn, rIn));
        g.Freeze();
        dc.DrawGeometry(draw.Brush(color), null, g);
    }

    private static void DrawGlow(DrawingContext dc, DrawCache draw, double x, double y, double radius, Color color)
    {
        for (var i = 3; i >= 0; i--)
        {
            var t = (i + 1) / 4.0;
            var a = (byte)Math.Clamp(color.A * (0.12 + 0.22 * (1 - t)), 0, 255);
            var r = radius * (0.55 + 0.7 * t);
            dc.DrawEllipse(
                draw.Brush(Color.FromArgb(a, color.R, color.G, color.B)),
                null,
                new Point(x, y),
                r, r);
        }
    }

    private static void DrawBeam(DrawingContext dc, DrawCache draw, double x1, double y1, double x2, double y2, double width, Color color)
    {
        var p0 = new Point(x1, y1);
        var p1 = new Point(x2, y2);
        for (var i = 4; i >= 1; i--)
        {
            var t = i / 4.0;
            var a = (byte)Math.Clamp(color.A * (0.1 + 0.18 * (1 - t * 0.65)), 0, 255);
            var w = width * (1.0 + t * 2.4);
            dc.DrawLine(draw.Pen(Color.FromArgb(a, color.R, color.G, color.B), w), p0, p1);
        }
        var coreA = color.A == 0 ? (byte)255 : color.A;
        dc.DrawLine(draw.Pen(Color.FromArgb(coreA, color.R, color.G, color.B), width), p0, p1);
        var tip = Math.Max(width * 0.55, 3);
        var tipBrush = draw.Brush(Color.FromArgb((byte)Math.Min(255, coreA + 40),
            (byte)Math.Min(255, color.R + 40),
            (byte)Math.Min(255, color.G + 40),
            (byte)Math.Min(255, color.B + 40)));
        dc.DrawEllipse(tipBrush, null, p0, tip, tip);
        dc.DrawEllipse(tipBrush, null, p1, tip * 0.85, tip * 0.85);
    }

    private enum CmdKind : byte { Line, Circle, Glow, Beam, Rect, Poly, Ring, Text }

    private sealed class Cmd
    {
        public CmdKind Kind;
        public double X1, Y1, X2, Y2, Radius, Width;
        public Color Color;
        public bool Filled;
        public string? Text;
        public Point[]? Points;
    }
}
