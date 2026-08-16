namespace DesktopDiep;

public sealed class DebugState
{
    public bool Enabled;
    public bool Paused;
    public bool ShowHitboxes = true;
    public bool ShowVelocity = true;
    public double Fps;
    public double FrameMs;
    public double Alpha;
    public int Frame;
    public int Tick;
    public int HashCells;
    public int HashPairs;
    public const int TicksPerSecond = 25;
    public string? Notice;
    public double NoticeLife;

    public void Flash(string text, double seconds = 2)
    {
        Notice = text;
        NoticeLife = seconds;
    }

    public void TickNotice(double dt)
    {
        if (NoticeLife > 0)
            NoticeLife -= dt;
    }
}
