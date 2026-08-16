using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using Imaging = System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DesktopDiep;

/// <summary>
/// Code-drawn app icon: white Windows 11 mark filled with diep tanks and polygons.
/// Writes <c>Assets/app.png</c>, then builds <c>Assets/app.ico</c> from that PNG.
/// </summary>
internal static class AppIcon
{
    private static readonly Drawing.Color Tank = Drawing.Color.FromArgb(0, 178, 225);
    private static readonly Drawing.Color Barrel = Drawing.Color.FromArgb(0x99, 0x99, 0x99);
    private static readonly Drawing.Color Square = Drawing.Color.FromArgb(0xFF, 0xE8, 0x69);
    private static readonly Drawing.Color Triangle = Drawing.Color.FromArgb(0xFC, 0x76, 0x77);
    private static readonly Drawing.Color Pentagon = Drawing.Color.FromArgb(0x76, 0x8D, 0xFC);
    private static readonly Drawing.Color Outline = Drawing.Color.FromArgb(0x55, 0x55, 0x55);
    private static readonly Drawing.Color TeamPurple = Drawing.Color.FromArgb(0xBF, 0x7F, 0xF5);
    private static readonly Drawing.Color TeamGreen = Drawing.Color.FromArgb(0x00, 0xE1, 0x6E);
    private static readonly Drawing.Color TeamRed = Drawing.Color.FromArgb(0xF1, 0x4E, 0x54);

    private static readonly int[] IcoSizes = [16, 24, 32, 48, 64, 128, 256];

    public static Drawing.Icon Create(int size = 32) => FromBitmap(Draw(size));

    public static Drawing.Icon LoadFromFile(string icoPath, int size = 32) =>
        new(icoPath, size, size);

    public static Drawing.Bitmap Draw(int size)
    {
        var bmp = new Drawing.Bitmap(size, size, Imaging.PixelFormat.Format32bppArgb);
        using var g = Drawing.Graphics.FromImage(bmp);
        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality;
        g.CompositingQuality = Drawing2D.CompositingQuality.HighQuality;
        g.Clear(Drawing.Color.Transparent);

        var panes = Win11Panes(size);
        using var white = new Drawing.SolidBrush(Drawing.Color.White);

        foreach (var pane in panes)
            g.FillPath(white, pane);

        var rng = new Random(11);
        for (var i = 0; i < panes.Length; i++)
        {
            using var clip = new Drawing.Region(panes[i]);
            g.SetClip(clip, Drawing2D.CombineMode.Replace);
            FillPaneWithDiep(g, panes[i].GetBounds(), rng, i);
            g.ResetClip();
        }

        foreach (var pane in panes)
            pane.Dispose();

        return bmp;
    }

    /// <summary>Draw PNG master, then build ICO from that PNG.</summary>
    public static void WriteAssets(string directory, int masterSize = 256)
    {
        Directory.CreateDirectory(directory);
        var pngPath = Path.Combine(directory, "app.png");
        var icoPath = Path.Combine(directory, "app.ico");

        using (var master = Draw(masterSize))
            master.Save(pngPath, Imaging.ImageFormat.Png);

        WriteIcoFromPng(pngPath, icoPath, IcoSizes);
    }

    public static void WriteIcoFromPng(string pngPath, string icoPath, IReadOnlyList<int> sizes)
    {
        using var master = new Drawing.Bitmap(pngPath);
        var images = new List<byte[]>(sizes.Count);
        foreach (var size in sizes)
        {
            using var scaled = new Drawing.Bitmap(size, size, Imaging.PixelFormat.Format32bppArgb);
            using (var g = Drawing.Graphics.FromImage(scaled))
            {
                g.Clear(Drawing.Color.Transparent);
                g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(master, new Drawing.Rectangle(0, 0, size, size));
            }

            using var ms = new MemoryStream();
            scaled.Save(ms, Imaging.ImageFormat.Png);
            images.Add(ms.ToArray());
        }

        using var fs = File.Create(icoPath);
        using var bw = new BinaryWriter(fs);
        bw.Write((ushort)0);
        bw.Write((ushort)1);
        bw.Write((ushort)images.Count);

        var offset = 6 + 16 * images.Count;
        for (var i = 0; i < images.Count; i++)
        {
            var size = sizes[i];
            var data = images[i];
            bw.Write((byte)(size >= 256 ? 0 : size));
            bw.Write((byte)(size >= 256 ? 0 : size));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((ushort)1);
            bw.Write((ushort)32);
            bw.Write(data.Length);
            bw.Write(offset);
            offset += data.Length;
        }

        foreach (var data in images)
            bw.Write(data);
    }

    private static Drawing2D.GraphicsPath[] Win11Panes(int size)
    {
        float pad = size * 0.08f;
        float gap = size * 0.055f;
        float inner = size - pad * 2;
        float pane = (inner - gap) * 0.5f;
        float radius = pane * 0.18f;

        Drawing2D.GraphicsPath Rounded(float x, float y)
        {
            var path = new Drawing2D.GraphicsPath();
            AddRoundedRectangle(path, new Drawing.RectangleF(x, y, pane, pane), radius);
            return path;
        }

        return
        [
            Rounded(pad, pad),
            Rounded(pad + pane + gap, pad),
            Rounded(pad, pad + pane + gap),
            Rounded(pad + pane + gap, pad + pane + gap),
        ];
    }

    private static void FillPaneWithDiep(Drawing.Graphics g, Drawing.RectangleF bounds, Random rng, int paneIndex)
    {
        var cx = bounds.X + bounds.Width * 0.5f;
        var cy = bounds.Y + bounds.Height * 0.5f;
        var scale = Math.Min(bounds.Width, bounds.Height);

        switch (paneIndex)
        {
            case 0:
                DrawTank(g, cx - scale * 0.12f, cy + scale * 0.05f, scale * 0.22f, 0.4f, Tank);
                DrawPolygon(g, RegularPolygon(cx + scale * 0.22f, cy - scale * 0.18f, scale * 0.14f, 4, 0.2f), Square);
                DrawPolygon(g, RegularPolygon(cx - scale * 0.28f, cy - scale * 0.22f, scale * 0.11f, 3, -0.3f), Triangle);
                break;
            case 1:
                DrawTank(g, cx + scale * 0.05f, cy - scale * 0.05f, scale * 0.2f, -0.6f, TeamPurple);
                DrawPolygon(g, RegularPolygon(cx - scale * 0.22f, cy + scale * 0.2f, scale * 0.13f, 5, 0.1f), Pentagon);
                DrawPolygon(g, RegularPolygon(cx + scale * 0.28f, cy + scale * 0.22f, scale * 0.1f, 4, 0.5f), Square);
                DrawTank(g, cx - scale * 0.25f, cy - scale * 0.25f, scale * 0.12f, 1.1f, TeamGreen);
                break;
            case 2:
                DrawPolygon(g, RegularPolygon(cx, cy - scale * 0.05f, scale * 0.2f, 5, 0), Pentagon);
                DrawTank(g, cx - scale * 0.25f, cy + scale * 0.22f, scale * 0.16f, 0.2f, TeamRed);
                DrawPolygon(g, RegularPolygon(cx + scale * 0.26f, cy + scale * 0.18f, scale * 0.12f, 3, 0.8f), Triangle);
                DrawPolygon(g, RegularPolygon(cx + scale * 0.2f, cy - scale * 0.28f, scale * 0.09f, 4, -0.4f), Square);
                break;
            default:
                DrawTank(g, cx, cy, scale * 0.24f, -0.2f, Tank);
                DrawPolygon(g, RegularPolygon(cx - scale * 0.28f, cy - scale * 0.2f, scale * 0.1f, 3, 0.5f), Triangle);
                DrawPolygon(g, RegularPolygon(cx + scale * 0.28f, cy - scale * 0.18f, scale * 0.11f, 4, -0.2f), Square);
                DrawPolygon(g, RegularPolygon(cx + scale * 0.18f, cy + scale * 0.28f, scale * 0.1f, 5, 0.3f), Pentagon);
                DrawTank(g, cx - scale * 0.22f, cy + scale * 0.26f, scale * 0.11f, 2.4f, TeamPurple);
                break;
        }

        for (var n = 0; n < 4; n++)
        {
            var x = bounds.X + bounds.Width * (0.12f + (float)rng.NextDouble() * 0.76f);
            var y = bounds.Y + bounds.Height * (0.12f + (float)rng.NextDouble() * 0.76f);
            var r = scale * (0.045f + (float)rng.NextDouble() * 0.07f);
            var rot = (float)(rng.NextDouble() * Math.PI * 2);
            switch (rng.Next(4))
            {
                case 0:
                    DrawPolygon(g, RegularPolygon(x, y, r, 4, rot), Square);
                    break;
                case 1:
                    DrawPolygon(g, RegularPolygon(x, y, r, 3, rot), Triangle);
                    break;
                case 2:
                    DrawPolygon(g, RegularPolygon(x, y, r, 5, rot), Pentagon);
                    break;
                default:
                    DrawTank(g, x, y, r * 1.35f, rot, rng.Next(2) == 0 ? Tank : TeamGreen);
                    break;
            }
        }
    }

    private static void DrawTank(Drawing.Graphics g, float cx, float cy, float radius, float angle, Drawing.Color body)
    {
        var state = g.Save();
        g.TranslateTransform(cx, cy);
        g.RotateTransform(angle * 180f / (float)Math.PI);

        var stroke = Math.Max(1f, radius * 0.14f);
        using var bodyBrush = new Drawing.SolidBrush(body);
        using var barrelBrush = new Drawing.SolidBrush(Barrel);
        using var pen = new Drawing.Pen(Outline, stroke) { LineJoin = Drawing2D.LineJoin.Round };

        var barrel = new Drawing.RectangleF(radius * 0.15f, -radius * 0.28f, radius * 1.15f, radius * 0.56f);
        g.FillRectangle(barrelBrush, barrel);
        g.DrawRectangle(pen, barrel.X, barrel.Y, barrel.Width, barrel.Height);
        g.FillEllipse(bodyBrush, -radius, -radius, radius * 2f, radius * 2f);
        g.DrawEllipse(pen, -radius, -radius, radius * 2f, radius * 2f);

        g.Restore(state);
    }

    private static void DrawPolygon(Drawing.Graphics g, Drawing.PointF[] pts, Drawing.Color fill)
    {
        if (pts.Length < 3) return;
        var stroke = Math.Max(1f, Distance(pts[0], pts[1]) * 0.12f);
        using var brush = new Drawing.SolidBrush(fill);
        using var pen = new Drawing.Pen(Outline, stroke) { LineJoin = Drawing2D.LineJoin.Round };
        g.FillPolygon(brush, pts);
        g.DrawPolygon(pen, pts);
    }

    private static Drawing.PointF[] RegularPolygon(float cx, float cy, float radius, int sides, float rotation)
    {
        var pts = new Drawing.PointF[sides];
        var start = sides == 3 ? rotation : rotation - (float)Math.PI / 2f;
        for (var i = 0; i < sides; i++)
        {
            var a = start + i * (float)(Math.PI * 2 / sides);
            pts[i] = new Drawing.PointF(cx + MathF.Cos(a) * radius, cy + MathF.Sin(a) * radius);
        }
        return pts;
    }

    private static float Distance(Drawing.PointF a, Drawing.PointF b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static Drawing.Icon FromBitmap(Drawing.Bitmap bmp)
    {
        var handle = bmp.GetHicon();
        try
        {
            using var temp = Drawing.Icon.FromHandle(handle);
            return (Drawing.Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
            bmp.Dispose();
        }
    }

    private static void AddRoundedRectangle(Drawing2D.GraphicsPath path, Drawing.RectangleF bounds, float radius)
    {
        radius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) * 0.5f);
        var diameter = radius * 2f;
        var arc = new Drawing.RectangleF(bounds.Location, new Drawing.SizeF(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint hIcon);
}
