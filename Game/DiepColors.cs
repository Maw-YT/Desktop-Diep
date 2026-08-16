using System.Windows.Media;

namespace DesktopDiep;

public sealed class DamageFlash
{
    public double Amount;
    public double Prev;

    public void Hit() => Amount = 1;

    public void Capture() => Prev = Amount;

    public void Tick() => Amount = Math.Max(0, Amount - 0.5);

    public void Reset()
    {
        Amount = 0;
        Prev = 0;
    }

    public double Draw(double interp) => Interp.Lerp(Prev, Amount, interp);
}

internal static class DiepColors
{
    public static readonly Color Tank = Color.FromRgb(0x00, 0xB2, 0xE1);
    public static readonly Color Barrel = Color.FromRgb(0x99, 0x99, 0x99);
    public static readonly Color Border = Color.FromRgb(0x55, 0x55, 0x55);
    public static readonly Color TeamPurple = Color.FromRgb(0xBF, 0x7F, 0xF5);
    public static readonly Color Square = Color.FromRgb(0xFF, 0xE8, 0x69);
    public static readonly Color Triangle = Color.FromRgb(0xFC, 0x76, 0x77);
    public static readonly Color Pentagon = Color.FromRgb(0x76, 0x8D, 0xFC);
    public static readonly Color Crasher = Color.FromRgb(0xF1, 0x77, 0xDD);
    public static readonly Color HealthBack = Color.FromRgb(0x55, 0x55, 0x55);
    public static readonly Color Health = Color.FromRgb(0x85, 0xE3, 0x7D);
    public static readonly Color Xp = Color.FromRgb(0xF0, 0xD9, 0x4A);
    public static readonly Color Debug = Color.FromRgb(0x7C, 0xFC, 0x00);
    public static readonly Color NecroSquare = Color.FromRgb(0xFC, 0xC3, 0x76);
    public static readonly Color Damage = Color.FromRgb(0xF1, 0x4E, 0x54);

    public static readonly Color[] Teams =
    [
        Color.FromRgb(0x00, 0xB2, 0xE1),
        Color.FromRgb(0xF1, 0x4E, 0x54),
        Color.FromRgb(0x00, 0xE1, 0x6E),
        Color.FromRgb(0xBF, 0x7F, 0xF5),
        Color.FromRgb(0xFF, 0xA5, 0x00),
        Color.FromRgb(0xF1, 0x77, 0xDD),
        Color.FromRgb(0xFC, 0xC3, 0x76),
        Color.FromRgb(0x8A, 0xFF, 0x69),
        Color.FromRgb(0x00, 0xC4, 0xE0),
        Color.FromRgb(0xE8, 0xA0, 0xFF),
        Color.FromRgb(0xFF, 0x6B, 0x35),
        Color.FromRgb(0x76, 0x8D, 0xFC),
        Color.FromRgb(0xC0, 0xC0, 0xC0),
        Color.FromRgb(0xFF, 0xE8, 0x69),
        Color.FromRgb(0x43, 0xFF, 0x91)
    ];

    public static Color Team(int id) => Teams[Math.Abs(id) % Teams.Length];

    public static Color Stroke(Color fill) =>
        Color.FromRgb((byte)(fill.R * 0.72), (byte)(fill.G * 0.72), (byte)(fill.B * 0.72));

    public static Color Mix(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromRgb(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t));
    }

    public static Color Hit(Color fill, double flash) =>
        flash <= 0.001 ? fill : Mix(fill, Damage, flash);
}
