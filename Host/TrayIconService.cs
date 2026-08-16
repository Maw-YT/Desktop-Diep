using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace DesktopDiep;

internal sealed class TrayIconService : IDisposable
{
    private readonly WinForms.NotifyIcon _tray;
    private readonly WinForms.ContextMenuStrip _menu;
    private readonly WinForms.ToolStripMenuItem _debugItem;
    private readonly WinForms.ToolStripMenuItem _pauseItem;
    private readonly WinForms.ToolStripMenuItem _interpItem;
    private readonly WinForms.ToolStripMenuItem _haloItem;
    private readonly WinForms.ToolStripMenuItem _tanksItem;
    private readonly WinForms.ToolStripMenuItem _classItem;
    private readonly WinForms.ToolStripMenuItem _statsItem;
    private GameWorld? _world;
    private bool _rebuilding;

    public event Action? ToggleDebug;
    public event Action? TogglePause;
    public event Action? ToggleInterpolate;
    public event Action? ToggleSelectionHalo;
    public event Action? Reset;
    public event Action? Spawn;
    public event Action? RemoveSelected;
    public event Action? Exit;
    public event Action<int>? SelectTank;
    public event Action<int, int>? SetStat;
    public event Action<TankId>? SetClass;

    public TrayIconService()
    {
        _debugItem = new WinForms.ToolStripMenuItem("Debug overlay") { CheckOnClick = true };
        _pauseItem = new WinForms.ToolStripMenuItem("Pause") { CheckOnClick = true };
        _interpItem = new WinForms.ToolStripMenuItem("Interpolate motion") { CheckOnClick = true, Checked = true };
        _haloItem = new WinForms.ToolStripMenuItem("Selection halo") { CheckOnClick = true };
        _tanksItem = new WinForms.ToolStripMenuItem("Tanks");
        _classItem = new WinForms.ToolStripMenuItem("Class");
        _statsItem = new WinForms.ToolStripMenuItem("Stats");

        _debugItem.Click += (_, _) => { if (!_rebuilding) ToggleDebug?.Invoke(); };
        _pauseItem.Click += (_, _) => { if (!_rebuilding) TogglePause?.Invoke(); };
        _interpItem.Click += (_, _) => { if (!_rebuilding) ToggleInterpolate?.Invoke(); };
        _haloItem.Click += (_, _) => { if (!_rebuilding) ToggleSelectionHalo?.Invoke(); };

        _menu = new WinForms.ContextMenuStrip();
        _menu.Opening += (_, _) => Rebuild();
        _menu.Items.Add(_tanksItem);
        _menu.Items.Add(_classItem);
        _menu.Items.Add(_statsItem);
        _menu.Items.Add(new WinForms.ToolStripSeparator());
        _menu.Items.Add(_debugItem);
        _menu.Items.Add(_pauseItem);
        _menu.Items.Add(_interpItem);
        _menu.Items.Add(_haloItem);
        _menu.Items.Add("Reset", null, (_, _) => Reset?.Invoke());
        _menu.Items.Add("Remove selected", null, (_, _) => RemoveSelected?.Invoke());
        _menu.Items.Add(new WinForms.ToolStripSeparator());
        _menu.Items.Add("Quit", null, (_, _) => Exit?.Invoke());

        _tray = new WinForms.NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "Desktop Diep",
            Visible = true,
            ContextMenuStrip = _menu
        };
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == WinForms.MouseButtons.Left)
                Spawn?.Invoke();
        };
    }

    public void Sync(GameWorld world)
    {
        _world = world;
        var debug = world.Debug;
        _debugItem.Checked = debug.Enabled;
        _pauseItem.Checked = debug.Paused;
        _interpItem.Checked = world.Interpolate;
        _haloItem.Checked = world.ShowSelectionHalo;
        var sel = world.Selected;
        _tray.Text = sel is null
            ? "Desktop Diep"
            : $"Desktop Diep  {sel.Class.Name}  Lv {sel.Level}";
    }

    private void Rebuild()
    {
        if (_world is null)
            return;
        _rebuilding = true;
        try
        {
            RebuildTanks(_world);
            RebuildClass(_world);
            RebuildStats(_world);
        }
        finally
        {
            _rebuilding = false;
        }
    }

    private void RebuildTanks(GameWorld world)
    {
        _tanksItem.DropDownItems.Clear();
        for (var i = 0; i < world.Tanks.Count; i++)
        {
            var tank = world.Tanks[i];
            var index = i;
            var item = new WinForms.ToolStripMenuItem($"{tank.Class.Name}  Lv {tank.Level}")
            {
                Checked = tank == world.Selected,
                CheckOnClick = true
            };
            item.Click += (_, _) => SelectTank?.Invoke(index);
            _tanksItem.DropDownItems.Add(item);
        }
        if (_tanksItem.DropDownItems.Count == 0)
            _tanksItem.DropDownItems.Add(new WinForms.ToolStripMenuItem("(none)") { Enabled = false });
    }

    private void RebuildClass(GameWorld world)
    {
        _classItem.DropDownItems.Clear();
        var selected = world.Selected;
        foreach (var def in TankCatalog.All)
        {
            var id = def.Id;
            var item = new WinForms.ToolStripMenuItem(def.Name)
            {
                Checked = selected is not null && selected.ClassId == id,
                Enabled = selected is not null
            };
            item.Click += (_, _) => SetClass?.Invoke(id);
            _classItem.DropDownItems.Add(item);
        }
    }

    private void RebuildStats(GameWorld world)
    {
        _statsItem.DropDownItems.Clear();
        var tank = world.Selected;
        for (var stat = 0; stat < 8; stat++)
        {
            var s = stat;
            var current = tank?.Stats[stat] ?? 0;
            var group = new WinForms.ToolStripMenuItem($"{TankStats.Names[stat]}  {current}");
            for (var v = 0; v <= 7; v++)
            {
                var value = v;
                var opt = new WinForms.ToolStripMenuItem(v.ToString())
                {
                    Checked = tank is not null && current == v,
                    Enabled = tank is not null
                };
                opt.Click += (_, _) => SetStat?.Invoke(s, value);
                group.DropDownItems.Add(opt);
            }
            _statsItem.DropDownItems.Add(group);
        }
    }

    public void Dispose()
    {
        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();
    }

    private static Drawing.Icon CreateIcon()
    {
        var bmp = new Drawing.Bitmap(32, 32);
        using (var g = Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Drawing.Color.Transparent);
            using var fill = new Drawing.SolidBrush(Drawing.Color.FromArgb(0, 178, 225));
            using var stroke = new Drawing.Pen(Drawing.Color.FromArgb(0, 120, 150), 2);
            g.FillRectangle(fill, 16, 12, 12, 8);
            g.DrawRectangle(stroke, 16, 12, 12, 8);
            g.FillEllipse(fill, 4, 4, 20, 20);
            g.DrawEllipse(stroke, 4, 4, 20, 20);
        }
        return Drawing.Icon.FromHandle(bmp.GetHicon());
    }
}
