using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace DesktopDiep;

public partial class MainWindow : Window
{
    private HwndSource? _source;
    private HotkeyService? _hotkeys;
    private TrayIconService? _tray;
    private int _lastTrayFrame = -1;

    public MainWindow()
    {
        InitializeComponent();
        FitToVirtualScreen();
        Loaded += OnLoaded;
        Closed += OnClosed;
        SystemEvents.DisplaySettingsChanged += (_, _) => Dispatcher.Invoke(FitToVirtualScreen);
    }

    private void FitToVirtualScreen()
    {
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        if (IsLoaded)
            Canvas.World.Resize(Canvas.ActualWidth, Canvas.ActualHeight);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var source = (HwndSource)PresentationSource.FromVisual(this)!;
        _source = source;
        Win32.ApplyPetWindowStyle(source);
        Canvas.World.OverlayHwnd = source.Handle;

        _hotkeys = new HotkeyService(source);
        _hotkeys.ToggleDebug += Canvas.ToggleDebug;
        _hotkeys.TogglePause += Canvas.TogglePause;
        _hotkeys.Reset += Canvas.ResetWorld;

        _tray = new TrayIconService();
        _tray.ToggleDebug += () => Dispatcher.Invoke(Canvas.ToggleDebug);
        _tray.TogglePause += () => Dispatcher.Invoke(Canvas.TogglePause);
        _tray.ToggleInterpolate += () => Dispatcher.Invoke(Canvas.ToggleInterpolate);
        _tray.ToggleSelectionHalo += () => Dispatcher.Invoke(Canvas.ToggleSelectionHalo);
        _tray.ToggleNav += () => Dispatcher.Invoke(Canvas.ToggleNav);
        _tray.ToggleHash += () => Dispatcher.Invoke(Canvas.ToggleHash);
        _tray.ToggleWindowCollisions += () => Dispatcher.Invoke(Canvas.ToggleWindowCollisions);
        _tray.ToggleCursorCollisions += () => Dispatcher.Invoke(Canvas.ToggleCursorCollisions);
        _tray.SetRenderStyle += style => Dispatcher.Invoke(() => Canvas.SetRenderStyle(style));
        _tray.Reset += () => Dispatcher.Invoke(Canvas.ResetWorld);
        _tray.Spawn += () => Dispatcher.Invoke(Canvas.SpawnTank);
        _tray.SpawnShape += kind => Dispatcher.Invoke(() => Canvas.SpawnShape(kind));
        _tray.SpawnBoss += kind => Dispatcher.Invoke(() => Canvas.SpawnBoss(kind));
        _tray.CloseArena += () => Dispatcher.Invoke(Canvas.CloseArena);
        _tray.RemoveSelected += () => Dispatcher.Invoke(Canvas.RemoveSelected);
        _tray.SelectTank += i => Dispatcher.Invoke(() => Canvas.SelectTank(i));
        _tray.SetStat += (s, v) => Dispatcher.Invoke(() => Canvas.SetStat(s, v));
        _tray.SetClass += id => Dispatcher.Invoke(() => Canvas.SetClass(id));
        _tray.ReloadMods += () => Dispatcher.Invoke(Canvas.ReloadMods);
        _tray.SetModEnabled += (id, on) => Dispatcher.Invoke(() => Canvas.SetModEnabled(id, on));
        _tray.Exit += () => Dispatcher.Invoke(Close);

        CompositionTarget.Rendering += SyncTray;
    }

    private void SyncTray(object? sender, EventArgs e)
    {
        if (_tray is null) return;
        var frame = Canvas.World.Debug.Frame;
        if (frame == _lastTrayFrame) return;
        _lastTrayFrame = frame;
        _tray.Sync(Canvas.World);
        if (Canvas.World.ExitRequested)
        {
            Close();
            return;
        }
        if (_source is not null)
            Win32.SetClickThrough(_source, !Canvas.World.Cursor.WantsCapture);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= SyncTray;
        AppSettings.SaveFrom(Canvas.World);
        Canvas.World.Mods?.Dispose();
        _hotkeys?.Dispose();
        _tray?.Dispose();
    }
}
