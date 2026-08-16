using System.IO;
using System.Text.Json;

namespace DesktopDiep;

internal sealed class AppSettings
{
    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopDiep",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public bool Interpolate { get; set; } = true;
    public bool ShowSelectionHalo { get; set; }
    public bool ShowNav { get; set; }
    public bool ShowHash { get; set; }
    public bool CollideWindows { get; set; } = true;
    public bool CollideCursor { get; set; } = true;
    public bool DebugOverlay { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(Path))
                return new AppSettings();
            var json = File.ReadAllText(Path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Apply(GameWorld world)
    {
        world.Interpolate = Interpolate;
        world.ShowSelectionHalo = ShowSelectionHalo;
        world.ShowNav = ShowNav;
        world.ShowHash = ShowHash;
        world.CollideWindows = CollideWindows;
        world.CollideCursor = CollideCursor;
        world.Debug.Enabled = DebugOverlay;
    }

    public void Capture(GameWorld world)
    {
        Interpolate = world.Interpolate;
        ShowSelectionHalo = world.ShowSelectionHalo;
        ShowNav = world.ShowNav;
        ShowHash = world.ShowHash;
        CollideWindows = world.CollideWindows;
        CollideCursor = world.CollideCursor;
        DebugOverlay = world.Debug.Enabled;
    }

    public void Save()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(Path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // ignore disk errors
        }
    }

    public static void SaveFrom(GameWorld world)
    {
        var s = new AppSettings();
        s.Capture(world);
        s.Save();
    }
}
