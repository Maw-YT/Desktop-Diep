namespace DesktopDiep;

public sealed class DestroyAnim
{
    public bool Enabled = true;
    public bool Active;
    public int Frame = 5;
    public int PrevFrame = 5;
    public double Scale = 1;
    public double PrevScale = 1;
    public double Opacity = 1;
    public double PrevOpacity = 1;

    public bool Finished => Active && Frame < 0;

    public bool Begin()
    {
        if (Active)
            return true;
        if (!Enabled)
            return false;
        Active = true;
        Frame = 5;
        PrevFrame = 5;
        Scale = 1;
        PrevScale = 1;
        Opacity = 1;
        PrevOpacity = 1;
        return true;
    }

    public void Reset()
    {
        Active = false;
        Frame = 5;
        PrevFrame = 5;
        Scale = 1;
        PrevScale = 1;
        Opacity = 1;
        PrevOpacity = 1;
    }

    public void Capture()
    {
        PrevFrame = Frame;
        PrevScale = Scale;
        PrevOpacity = Opacity;
    }

    public void Tick()
    {
        if (!Active)
            return;
        if (Frame == 0)
        {
            Frame = -1;
            Opacity = 0;
            return;
        }

        if (Frame == 5)
            Opacity = 1 - 1.0 / 6;
        Scale *= 1.1;
        Opacity -= 1.0 / 6;
        if (Opacity < 0)
            Opacity = 0;
        Frame -= 1;
    }

    public double DrawScale(double interp) => Interp.Lerp(PrevScale, Scale, interp);

    public double DrawOpacity(double interp) => Interp.Lerp(PrevOpacity, Opacity, interp);
}
