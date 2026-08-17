using MoonSharp.Interpreter;

namespace DesktopDiep;

internal sealed class ModTimers
{
    private sealed class Entry
    {
        public required LoadedMod Mod;
        public required DynValue Fn;
        public double Remaining;
        public double Interval; // 0 = once
        public bool Alive = true;
    }

    private readonly List<Entry> _entries = [];
    private int _nextId = 1;
    private readonly Dictionary<int, Entry> _byId = [];

    public void Clear()
    {
        _entries.Clear();
        _byId.Clear();
    }

    public void ClearMod(LoadedMod mod)
    {
        foreach (var e in _entries)
        {
            if (e.Mod == mod)
                e.Alive = false;
        }
    }

    public int After(LoadedMod mod, double seconds, DynValue fn)
    {
        var id = _nextId++;
        var e = new Entry { Mod = mod, Fn = fn, Remaining = Math.Max(0, seconds), Interval = 0 };
        _entries.Add(e);
        _byId[id] = e;
        return id;
    }

    public int Every(LoadedMod mod, double seconds, DynValue fn)
    {
        var id = _nextId++;
        var interval = Math.Max(0.05, seconds);
        var e = new Entry { Mod = mod, Fn = fn, Remaining = interval, Interval = interval };
        _entries.Add(e);
        _byId[id] = e;
        return id;
    }

    public void Cancel(int id)
    {
        if (_byId.TryGetValue(id, out var e))
            e.Alive = false;
    }

    public void Tick(double dt)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var e = _entries[i];
            if (!e.Alive || !e.Mod.Enabled)
            {
                _entries.RemoveAt(i);
                continue;
            }
            e.Remaining -= dt;
            if (e.Remaining > 0)
                continue;
            try
            {
                e.Mod.Script.Call(e.Fn);
            }
            catch (Exception ex)
            {
                e.Mod.Fail(ex);
                e.Alive = false;
            }
            if (e.Interval > 0 && e.Alive)
                e.Remaining += e.Interval;
            else
            {
                e.Alive = false;
                _entries.RemoveAt(i);
            }
        }
    }
}
