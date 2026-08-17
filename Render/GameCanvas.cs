using System.Windows;
using System.Windows.Media;

namespace DesktopDiep;

public sealed class GameCanvas : FrameworkElement
{
    private readonly GameWorld _world = new();
    private readonly DrawCache _draw;
    private readonly WorldRenderer _worldRenderer;
    private readonly DebugOverlay _debugOverlay;
    private ModHost? _mods;
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
        PersistSettings();
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
        PersistSettings();
    }

    public void ToggleSelectionHalo()
    {
        _world.ShowSelectionHalo = !_world.ShowSelectionHalo;
        _world.Debug.Flash(_world.ShowSelectionHalo ? "Halo on" : "Halo off", 1.4);
        PersistSettings();
    }

    public void ToggleNav()
    {
        _world.ShowNav = !_world.ShowNav;
        _world.Debug.Flash(_world.ShowNav ? "A* debug on" : "A* debug off", 1.4);
        PersistSettings();
    }

    public void ToggleHash()
    {
        _world.ShowHash = !_world.ShowHash;
        _world.Debug.Flash(_world.ShowHash ? "Hash debug on" : "Hash debug off", 1.4);
        PersistSettings();
    }

    public void SpawnShape(ShapeKind? kind) => _world.SpawnShape(kind);

    public void SpawnBoss(TankId? kind) => _world.SpawnBoss(kind);

    public void CloseArena() => _world.CloseArena();

    public void ToggleWindowCollisions()
    {
        _world.CollideWindows = !_world.CollideWindows;
        _world.Debug.Flash(_world.CollideWindows ? "Window collide on" : "Window collide off", 1.4);
        PersistSettings();
    }

    public void ToggleCursorCollisions()
    {
        _world.CollideCursor = !_world.CollideCursor;
        _world.Debug.Flash(_world.CollideCursor ? "Cursor collide on" : "Cursor collide off", 1.4);
        PersistSettings();
    }

    public void SetRenderStyle(RenderStyle style)
    {
        style = RenderLooks.Normalize(style);
        _world.RenderStyle = style;
        _world.Debug.Flash($"Render: {RenderLooks.Label(style)}", 1.4);
        PersistSettings();
        InvalidateVisual();
    }

    public void ReloadMods() => _mods?.Reload();

    public void SetModEnabled(string id, bool enabled) => _mods?.SetEnabled(id, enabled);

    private static void PersistSettings(GameWorld world) => AppSettings.SaveFrom(world);

    private void PersistSettings() => PersistSettings(_world);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_started) return;
        _started = true;
        AppSettings.Load().Apply(_world);
        _mods = new ModHost(_world);
        _world.Mods = _mods;
        _mods.Start();
        ResetWorld();
        CompositionTarget.Rendering += OnRendering;
        SizeChanged += (_, _) => _world.Resize(ActualWidth, ActualHeight);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        PersistSettings();
        CompositionTarget.Rendering -= OnRendering;
        _mods?.Dispose();
        _mods = null;
        _world.Mods = null;
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
        DrawNotifications(dc, new Size(ActualWidth, ActualHeight));
        _debugOverlay.Draw(dc, _world, new Size(ActualWidth, ActualHeight));
    }

    private void DrawNotifications(DrawingContext dc, Size size)
    {
        var y = 18.0;
        foreach (var n in _world.Notifications.Items)
        {
            var fade = Math.Clamp(n.Life / Math.Min(0.45, n.MaxLife * 0.25), 0, 1);
            var alpha = (byte)(150 * fade);
            var text = _draw.Text(n.Text, 16, Color.FromArgb(alpha, 255, 255, 255));
            var padX = 16.0;
            var padY = 8.0;
            var w = text.Width + padX * 2;
            var h = text.Height + padY * 2;
            var x = (size.Width - w) / 2;
            var bg = n.Color;
            dc.DrawRectangle(
                _draw.Brush(Color.FromArgb(alpha, bg.R, bg.G, bg.B)),
                null,
                new Rect(x, y, w, h));
            dc.DrawText(text, new Point(x + padX, y + padY));
            y += h + 8;
        }
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
