namespace DesktopDiep;

public sealed class BarrelState
{
    public BarrelDef Def = null!;
    public double Pos;
    public double ReloadTime = 1;
    public int ShotAge = 1000;
    public int PrevShotAge = 1000;

    public void Bind(BarrelDef def, double tankReload)
    {
        Def = def;
        ReloadTime = Math.Max(1, tankReload * def.Reload);
        Pos = ReloadTime;
        ShotAge = 1000;
        PrevShotAge = 1000;
    }

    public void Capture() => PrevShotAge = ShotAge;

    public double DrawLength(double interp, double scale)
    {
        var reload = Math.Max(1, ReloadTime);
        var a = Math.Min(1, PrevShotAge / reload);
        var b = Math.Min(1, ShotAge / reload);
        return Def.Size * scale * (0.8 + 0.2 * Interp.Lerp(a, b, interp));
    }
}
