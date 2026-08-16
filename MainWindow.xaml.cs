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

        _hotkeys = new HotkeyService(source);
        _hotkeys.ToggleDebug += Canvas.ToggleDebug;
        _hotkeys.TogglePause += Canvas.TogglePause;
        _hotkeys.Reset += Canvas.ResetWorld;

        _tray = new TrayIconService();
        _tray.ToggleDebug += () => Dispatcher.Invoke(Canvas.ToggleDebug);
        _tray.TogglePause += () => Dispatcher.Invoke(Canvas.TogglePause);
        _tray.ToggleInterpolate += () => Dispatcher.Invoke(Canvas.ToggleInterpolate);
        _tray.ToggleSelectionHalo += () => Dispatcher.Invoke(Canvas.ToggleSelectionHalo);
        _tray.Reset += () => Dispatcher.Invoke(Canvas.ResetWorld);
        _tray.Spawn += () => Dispatcher.Invoke(Canvas.SpawnTank);
        _tray.RemoveSelected += () => Dispatcher.Invoke(Canvas.RemoveSelected);
        _tray.SelectTank += i => Dispatcher.Invoke(() => Canvas.SelectTank(i));
        _tray.SetStat += (s, v) => Dispatcher.Invoke(() => Canvas.SetStat(s, v));
        _tray.SetClass += id => Dispatcher.Invoke(() => Canvas.SetClass(id));
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
        if (_source is not null)
            Win32.SetClickThrough(_source, !Canvas.World.Cursor.WantsCapture);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        CompositionTarget.Rendering -= SyncTray;
        _hotkeys?.Dispose();
        _tray?.Dispose();
    }
}
