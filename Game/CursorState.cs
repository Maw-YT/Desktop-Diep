namespace DesktopDiep;

public sealed class CursorState
{
    public double X, Y, PrevX, PrevY, Vx, Vy;
    public double Radius = 14;
    public double PushFactor = 12;
    public double Absorption;
    public bool Down;
    public bool Hovering;
    public bool Grabbing;
    public PhysKind GrabKind;
    public int GrabIndex = -1;
    public double GrabOffX, GrabOffY;

    public bool WantsCapture => Hovering || Grabbing;

    public void Feed(double x, double y, bool down)
    {
        X = x;
        Y = y;
        Down = down;
    }

    public void BeginTick()
    {
        Vx = X - PrevX;
        Vy = Y - PrevY;
    }

    public void EndTick()
    {
        PrevX = X;
        PrevY = Y;
    }

    public void Release()
    {
        Grabbing = false;
        GrabIndex = -1;
    }
}
