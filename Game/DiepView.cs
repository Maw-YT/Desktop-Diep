namespace DesktopDiep;

/// <summary>
/// Same view as LoveDiep: a 16:9 diep field (1920×1080 / FOV) scaled with
/// max(width, height) so a non-16:9 desktop never shows more world than that.
/// Authored sizes in this project are pixels at <see cref="Authored"/> (a size-50 tank ≈ 22px).
/// </summary>
internal static class DiepView
{
    public const double Fov = 0.35;
    public const double Authored = 22.0 / 50.0;

    public static double Scale { get; private set; } = Authored;

    public static double Factor => Scale / Authored;

    public static void SetScreen(double width, double height)
    {
        var refW = 1920.0 / Fov;
        var refH = 1080.0 / Fov;
        var scale = Math.Max(Math.Max(1, width) / refW, Math.Max(1, height) / refH);
        if (double.IsNaN(scale) || scale < 0.05)
            scale = Authored;
        Scale = scale;
    }

    public static double Px(double authoredPixels) => authoredPixels * Factor;

    /// <summary>Diep physics units (a basic body is 50) into screen pixels.</summary>
    public static double Units(double diepSize) => diepSize * Scale;

    public const double SqrtHalf = 0.7071067811865476;

    /// <summary>Fallen bosses call scale(1.01^74).</summary>
    public static readonly double FallenScale = Math.Pow(1.01, 74);

    /// <summary>Screen-pixel stroke. Diep border is 7.5×0.78 world units, with a 2.5px floor.</summary>
    public static double Stroke(double radiusPx, bool bullet = false)
    {
        var cap = 7.5 * 0.78 * Scale * (bullet ? 0.85 : 1);
        var floor = bullet ? 2.0 : 2.5;
        return Math.Max(floor, Math.Min(cap, Math.Max(1, radiusPx) * 0.2));
    }
}
