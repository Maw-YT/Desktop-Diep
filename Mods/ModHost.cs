using System.IO;
using System.Text;
using System.Text.Json;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace DesktopDiep;

internal sealed class ModHost : IDisposable
{
    public static ModHost? Current { get; private set; }

    public static string ModsRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopDiep",
        "mods");

    public GameWorld World { get; }
    public ModEvents Events { get; } = new();
    public ModTimers Timers { get; } = new();
    public ModDraw Draw { get; } = new();
    public IReadOnlyList<LoadedMod> Mods => _mods;

    private readonly List<LoadedMod> _mods = [];
    private readonly List<ModListEntry> _inactive = [];
    private readonly HashSet<string> _disabled = new(StringComparer.OrdinalIgnoreCase);
    private readonly StringBuilder _log = new();
    private bool _started;

    private static string DisabledPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopDiep",
        "disabled-mods.json");

    public IReadOnlyList<ModListEntry> ListEntries()
    {
        var list = new List<ModListEntry>(_mods.Count + _inactive.Count);
        foreach (var mod in _mods)
        {
            list.Add(new ModListEntry
            {
                Id = mod.Manifest.Id,
                Name = mod.Manifest.Name,
                Version = mod.Manifest.Version,
                Author = mod.Manifest.Author,
                Folder = mod.Folder,
                Enabled = mod.Enabled,
                Error = mod.LastError
            });
        }
        list.AddRange(_inactive);
        return list;
    }

    public ModHost(GameWorld world) => World = world;

    public void Start()
    {
        if (_started) return;
        _started = true;
        Current = this;
        EntityProxies.EnsureRegistered();
        Directory.CreateDirectory(ModsRoot);
        EnsureExampleMod();
        LoadDisabled();
        LoadAll();
        Events.Emit("init");
    }

    public void Stop()
    {
        if (!_started) return;
        Events.Emit("unload");
        foreach (var mod in _mods)
            Events.Off(mod);
        Timers.Clear();
        Draw.Clear();
        TankCatalog.ClearModRegistrations();
        _mods.Clear();
        _inactive.Clear();
        _started = false;
        if (Current == this)
            Current = null;
    }

    public void Reload()
    {
        var w = World;
        Stop();
        _started = false;
        Current = this;
        _started = true;
        EntityProxies.EnsureRegistered();
        LoadAll();
        Events.Emit("init");
        w.Debug.Flash($"Mods: {_mods.Count(m => m.Enabled)} loaded", 2);
        w.Notifications.Server($"Reloaded mods ({_mods.Count(m => m.Enabled)} active)", 3, "mods");
    }

    public void SetEnabled(string id, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;
        if (enabled)
            _disabled.Remove(id);
        else
            _disabled.Add(id);
        SaveDisabled();
        Reload();
    }

    private bool IsDisabled(string id, string folderName) =>
        _disabled.Contains(id) || _disabled.Contains(folderName);

    private void LoadDisabled()
    {
        _disabled.Clear();
        try
        {
            if (!File.Exists(DisabledPath))
                return;
            var ids = JsonSerializer.Deserialize<string[]>(File.ReadAllText(DisabledPath));
            if (ids is null)
                return;
            foreach (var id in ids)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _disabled.Add(id.Trim());
            }
        }
        catch
        {
            // ignore
        }
    }

    private void SaveDisabled()
    {
        try
        {
            var dir = Path.GetDirectoryName(DisabledPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var ids = _disabled.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            File.WriteAllText(DisabledPath, JsonSerializer.Serialize(ids, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // ignore
        }
    }

    public void Tick(double dt)
    {
        Timers.Tick(dt);
        Emit("tick", DynValue.NewNumber(dt));
    }

    public void PostTick(double dt) => Emit("post_tick", DynValue.NewNumber(dt));

    /// <summary>Clear draw list, let mods queue primitives, then leave them for the renderer.</summary>
    public void BeginFrameDraw()
    {
        Draw.Clear();
        Emit("draw");
    }

    public void Emit(string name, params DynValue[] args) => Events.Emit(name, args);

    public bool EmitCancel(string name, Action<Table>? fill = null, params object?[] entities)
    {
        if (_mods.Count == 0) return false;
        // Use first enabled mod's script for table allocation; event is shared conceptually.
        var script = _mods.FirstOrDefault(m => m.Enabled)?.Script;
        if (script is null) return false;
        var e = new Table(script) { ["cancel"] = false, ["name"] = name };
        fill?.Invoke(e);
        var extras = new List<DynValue>();
        foreach (var obj in entities)
        {
            extras.Add(obj switch
            {
                TankEntity tank => EntityProxies.Tank(script, tank, World),
                ShapeEntity shape => EntityProxies.Shape(script, shape, World),
                BulletEntity bullet => EntityProxies.Bullet(script, bullet, World),
                DynValue dv => dv,
                double d => DynValue.NewNumber(d),
                int i => DynValue.NewNumber(i),
                string s => DynValue.NewString(s),
                bool b => DynValue.NewBoolean(b),
                _ => DynValue.Nil
            });
        }
        return Events.EmitCancelable(script, name, e, extras.ToArray())
               || (e.Get("cancel").Type == DataType.Boolean && e.Get("cancel").Boolean);
    }

    public double EmitDamage(string name, double amount, params object?[] entities)
    {
        if (_mods.Count == 0) return amount;
        var script = _mods.FirstOrDefault(m => m.Enabled)?.Script;
        if (script is null) return amount;
        var e = new Table(script)
        {
            ["cancel"] = false,
            ["damage"] = amount,
            ["name"] = name
        };
        var extras = new List<DynValue>();
        foreach (var obj in entities)
        {
            extras.Add(obj switch
            {
                TankEntity tank => EntityProxies.Tank(script, tank, World),
                ShapeEntity shape => EntityProxies.Shape(script, shape, World),
                BulletEntity bullet => EntityProxies.Bullet(script, bullet, World),
                _ => DynValue.Nil
            });
        }
        Events.EmitCancelable(script, name, e, extras.ToArray());
        if (e.Get("cancel").Type == DataType.Boolean && e.Get("cancel").Boolean)
            return 0;
        var dmg = e.Get("damage");
        return dmg.Type == DataType.Number ? dmg.Number : amount;
    }

    public void ReportModError(LoadedMod mod, Exception ex)
    {
        Log($"Mod '{mod.Manifest.Id}' error: {ex.Message}");
        World.Debug.Flash($"Mod error: {mod.Manifest.Id}", 3);
        World.Notifications.Server($"Mod '{mod.Manifest.Name}' disabled: {ex.Message}", 6, $"mod_err_{mod.Manifest.Id}");
        Events.Off(mod);
        Timers.ClearMod(mod);
    }

    public void Log(string message)
    {
        _log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        if (_log.Length > 8000)
            _log.Remove(0, _log.Length - 6000);
        try
        {
            var path = Path.Combine(ModsRoot, "mods.log");
            File.AppendAllText(path, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }

    public void EmitTank(string name, TankEntity tank) =>
        Emit(name, EntityArg(tank));

    public void EmitShape(string name, ShapeEntity shape) =>
        Emit(name, EntityArg(shape));

    public void EmitBullet(string name, BulletEntity bullet) =>
        Emit(name, EntityArg(bullet));

    private DynValue EntityArg(object entity)
    {
        var script = _mods.FirstOrDefault(m => m.Enabled)?.Script ?? new Script();
        return entity switch
        {
            TankEntity tank => EntityProxies.Tank(script, tank, World),
            ShapeEntity shape => EntityProxies.Shape(script, shape, World),
            BulletEntity bullet => EntityProxies.Bullet(script, bullet, World),
            _ => DynValue.Nil
        };
    }

    public void Dispose() => Stop();

    private void LoadAll()
    {
        LoadDisabled();
        _mods.Clear();
        _inactive.Clear();
        Events.Clear();
        Timers.Clear();
        TankCatalog.ClearModRegistrations();

        foreach (var dir in Directory.EnumerateDirectories(ModsRoot))
        {
            var name = Path.GetFileName(dir);
            if (name.StartsWith('.'))
                continue;
            var manifest = ModManifest.TryLoad(dir);
            if (manifest is null)
            {
                Log($"Skip '{name}': missing/invalid mod.json");
                _inactive.Add(new ModListEntry
                {
                    Id = name,
                    Name = name,
                    Folder = dir,
                    Enabled = false,
                    CanToggle = false,
                    Error = "Missing or invalid mod.json"
                });
                continue;
            }

            if (IsDisabled(manifest.Id, name))
            {
                Log($"Disabled '{manifest.Id}'");
                _inactive.Add(new ModListEntry
                {
                    Id = manifest.Id,
                    Name = string.IsNullOrWhiteSpace(manifest.Name) ? manifest.Id : manifest.Name,
                    Version = manifest.Version,
                    Author = manifest.Author,
                    Folder = dir,
                    Enabled = false
                });
                continue;
            }

            try
            {
                LoadMod(dir, manifest);
            }
            catch (Exception ex)
            {
                Log($"Failed '{manifest.Id}': {ex.Message}");
                World.Notifications.Server($"Failed to load mod '{manifest.Id}': {ex.Message}", 5);
                _inactive.Add(new ModListEntry
                {
                    Id = manifest.Id,
                    Name = string.IsNullOrWhiteSpace(manifest.Name) ? manifest.Id : manifest.Name,
                    Version = manifest.Version,
                    Author = manifest.Author,
                    Folder = dir,
                    Enabled = false,
                    Error = ex.Message
                });
            }
        }
        Log($"Loaded {_mods.Count} mod(s)");
    }

    private void LoadMod(string folder, ModManifest manifest)
    {
        var entry = Path.Combine(folder, manifest.Entry);
        if (!File.Exists(entry))
            throw new FileNotFoundException($"Entry script not found: {manifest.Entry}");

        var script = new Script(CoreModules.Preset_SoftSandbox);
        script.Options.DebugPrint = s => Log($"[{manifest.Id}] {s}");
        script.Options.ScriptLoader = new FileSystemScriptLoader
        {
            ModulePaths = [Path.Combine(folder, "?.lua"), Path.Combine(folder, "?", "init.lua")]
        };

        var mod = new LoadedMod
        {
            Manifest = manifest,
            Folder = folder,
            Script = script
        };

        script.Globals["Mod"] = ModApi.BuildModTable(script, mod);
        script.Globals["World"] = ModApi.BuildWorld(script, World);
        script.Globals["Events"] = ModApi.BuildEvents(script, this, mod);
        script.Globals["Catalog"] = ModApi.BuildCatalog(script);
        script.Globals["Notify"] = ModApi.BuildNotify(script, World);
        script.Globals["Timers"] = ModApi.BuildTimers(script, this, mod);
        script.Globals["Util"] = ModApi.BuildUtil(script, mod);
        script.Globals["Draw"] = ModApi.BuildDraw(script, this);
        script.Globals["Input"] = ModApi.BuildInput(script, World);

        script.DoFile(entry);
        _mods.Add(mod);
        Log($"Loaded mod '{manifest.Id}' v{manifest.Version}");
    }

    private void EnsureExampleMod()
    {
        var example = Path.Combine(ModsRoot, "example_chaos");
        var marker = Path.Combine(ModsRoot, ".example_installed");
        if (File.Exists(marker) || Directory.Exists(example))
            return;
        Directory.CreateDirectory(example);
        File.WriteAllText(Path.Combine(example, "mod.json"), """
{
  "id": "example_chaos",
  "name": "Example Chaos",
  "version": "1.0.0",
  "author": "Desktop Diep",
  "entry": "main.lua"
}
""");
        File.WriteAllText(Path.Combine(example, "main.lua"), ExampleMainLua);
        File.WriteAllText(marker, "1");
        Log("Installed example_chaos mod");
    }

    private const string ExampleMainLua = """
-- Example Chaos: tiny demo of Desktop Diep Lua mods.
Util.log("example_chaos ready")

Events.on("boss_spawn", function(boss)
  Notify.server("Mod saw boss: " .. (boss and boss.class_name or "?"))
end)

Events.on("level_up", function(tank)
  Notify.flash((tank and tank.class_name or "Tank") .. " leveled up")
end)

local acc = 0
Events.on("tick", function(dt)
  acc = acc + dt
  if acc < 8 then return end
  acc = 0
  if World.paused() then return end
  if World.rng() < 0.35 then
    World.spawn_shape()
  end
end)
""";
}
