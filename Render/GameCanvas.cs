using System.Windows;
using System.Windows.Media;

namespace DesktopDiep;

public sealed class GameCanvas : FrameworkElement
{
    private readonly GameWorld _world = new();
    private readonly DrawCache _draw;
    private readonly WorldRenderer _worldRenderer;
    private readonly DebugOverlay _debugOverlay;
    private TimeSpan _lastTime = TimeSpan.Zero;
    private double _fpsEma;
    private bool _started;

    public GameWorld World => _world;

    public GameCanvas()
    {
        _draw = new DrawCache(this);
        _worldRenderer = new WorldRenderer(_draw);
        _debugOverlay = new DebugOverlay(_draw);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsHitTestVisible = true;
    }

    public void ResetWorld()
    {
        var origin = StartPoint();
        _world.Reset(Math.Max(1, ActualWidth), Math.Max(1, ActualHeight), origin.X, origin.Y);
        InvalidateVisual();
    }

    public void SpawnTank() => _world.SpawnTank();

    public void RemoveSelected() => _world.RemoveSelected();

    public void SelectTank(int index) => _world.SelectTank(index);

    public void SetStat(int stat, int value) => _world.SetSelectedStat(stat, value);

    public void SetClass(TankId id) => _world.SetSelectedClass(id);

    public void ToggleDebug()
    {
        _world.Debug.Enabled = !_world.Debug.Enabled;
        _world.Debug.Flash(_world.Debug.Enabled ? "Debug on" : "Debug off", 1.4);
    }

    public void TogglePause()
    {
        _world.Debug.Paused = !_world.Debug.Paused;
        _world.Debug.Enabled = true;
        _world.Debug.Flash(_world.Debug.Paused ? "Paused" : "Resumed", 1.4);
    }

    public void ToggleInterpolate()
    {
        _world.Interpolate = !_world.Interpolate;
        _world.Debug.Flash(_world.Interpolate ? "Interp on" : "Interp off", 1.4);
    }

    public void ToggleSelectionHalo()
    {
        _world.ShowSelectionHalo = !_world.ShowSelectionHalo;
        _world.Debug.Flash(_world.ShowSelectionHalo ? "Halo on" : "Halo off", 1.4);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_started) return;
        _started = true;
        ResetWorld();
        CompositionTarget.Rendering += OnRendering;
        SizeChanged += (_, _) => _world.Resize(ActualWidth, ActualHeight);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        _started = false;
    }

    private static Point StartPoint()
    {
        var work = SystemParameters.WorkArea;
        var left = SystemParameters.VirtualScreenLeft;
        var top = SystemParameters.VirtualScreenTop;
        return new Point(
            work.Left - left + work.Width * 0.5,
            work.Top - top + work.Height * 0.5);
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs re)
            return;
        if (_lastTime == TimeSpan.Zero)
        {
            _lastTime = re.RenderingTime;
            return;
        }

        var dt = Math.Clamp((re.RenderingTime - _lastTime).TotalSeconds, 0, 0.05);
        _lastTime = re.RenderingTime;
        _world.Debug.FrameMs = dt * 1000;
        var inst = dt > 0 ? 1.0 / dt : 0;
        _fpsEma = _fpsEma <= 0 ? inst : _fpsEma * 0.9 + inst * 0.1;
        _world.Debug.Fps = _fpsEma;
        _world.Debug.Frame++;
        SamplePointer();
        _world.Advance(dt);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
        _worldRenderer.Draw(dc, _world);
        _debugOverlay.Draw(dc, _world, new Size(ActualWidth, ActualHeight));
    }

    private void SamplePointer()
    {
        Win32.GetCursorPos(out var pt);
        var screen = new Point(pt.X, pt.Y);
        Point local;
        try
        {
            local = PointFromScreen(screen);
        }
        catch
        {
            local = new Point(_world.Cursor.X, _world.Cursor.Y);
        }
        var down = (Win32.GetAsyncKeyState(Win32.VkLButton) & 0x8000) != 0;
        _world.SetPointer(local.X, local.Y, down);
    }
}
