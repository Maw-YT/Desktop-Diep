namespace DesktopDiep;

internal static class Interp
{
    public static double Lerp(double a, double b, double t) => a + (b - a) * t;

    public static double LerpAngle(double a, double b, double t) =>
        a + Math2.NormalizeAngle(b - a) * t;
}
