using MoonSharp.Interpreter;

namespace DesktopDiep;

internal sealed class ModEvents
{
    private readonly Dictionary<string, List<(LoadedMod Mod, DynValue Fn)>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public void Clear() => _handlers.Clear();

    public void On(LoadedMod mod, string name, DynValue fn)
    {
        if (fn.Type != DataType.Function)
            return;
        if (!_handlers.TryGetValue(name, out var list))
        {
            list = [];
            _handlers[name] = list;
        }
        list.Add((mod, fn));
    }

    public void Off(LoadedMod mod, string? name = null)
    {
        if (name is null)
        {
            foreach (var list in _handlers.Values)
                list.RemoveAll(h => h.Mod == mod);
            return;
        }
        if (_handlers.TryGetValue(name, out var handlers))
            handlers.RemoveAll(h => h.Mod == mod);
    }

    public void Emit(string name, params DynValue[] args)
    {
        if (!_handlers.TryGetValue(name, out var list) || list.Count == 0)
            return;
        foreach (var (mod, fn) in list.ToArray())
        {
            if (!mod.Enabled)
                continue;
            try
            {
                mod.Script.Call(fn, args);
            }
            catch (Exception ex)
            {
                mod.Fail(ex);
            }
        }
    }

    /// <summary>Returns true if any handler cancelled (returned false or set e.cancel).</summary>
    public bool EmitCancelable(Script script, string name, Table eventTable, params DynValue[] extraArgs)
    {
        if (!_handlers.TryGetValue(name, out var list) || list.Count == 0)
            return false;

        var args = new DynValue[1 + extraArgs.Length];
        args[0] = DynValue.NewTable(eventTable);
        for (var i = 0; i < extraArgs.Length; i++)
            args[i + 1] = extraArgs[i];

        var cancelled = false;
        foreach (var (mod, fn) in list.ToArray())
        {
            if (!mod.Enabled)
                continue;
            try
            {
                var result = mod.Script.Call(fn, args);
                if (result.Type == DataType.Boolean && !result.Boolean)
                    cancelled = true;
                if (eventTable.Get("cancel").Type == DataType.Boolean && eventTable.Get("cancel").Boolean)
                    cancelled = true;
            }
            catch (Exception ex)
            {
                mod.Fail(ex);
            }
        }
        return cancelled;
    }
}
