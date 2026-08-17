using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using MoonSharp.Interpreter;

namespace DesktopDiep;

internal static class ModApi
{
    public static Table BuildWorld(Script script, GameWorld world)
    {
        var t = new Table(script);
        t["width"] = DynValue.NewCallback((_, _) => DynValue.NewNumber(world.Width));
        t["height"] = DynValue.NewCallback((_, _) => DynValue.NewNumber(world.Height));
        t["tick"] = DynValue.NewCallback((_, _) => DynValue.NewNumber(world.Debug.Tick));
        t["time"] = DynValue.NewCallback((_, _) => DynValue.NewNumber(world.Debug.Tick * GameWorld.TickDt));
        t["dt"] = DynValue.NewCallback((_, _) => DynValue.NewNumber(GameWorld.TickDt));
        t["fps"] = DynValue.NewCallback((_, _) => DynValue.NewNumber(world.Debug.Fps));
        t["arena_closing"] = DynValue.NewCallback((_, _) => DynValue.NewBoolean(world.ArenaClosing));
        t["paused"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.Boolean)
                world.Debug.Paused = a[0].Boolean;
            return DynValue.NewBoolean(world.Debug.Paused);
        });
        t["debug"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.Boolean)
                world.Debug.Enabled = a[0].Boolean;
            return DynValue.NewBoolean(world.Debug.Enabled);
        });
        t["interpolate"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.Boolean)
                world.Interpolate = a[0].Boolean;
            return DynValue.NewBoolean(world.Interpolate);
        });
        t["halo"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.Boolean)
                world.ShowSelectionHalo = a[0].Boolean;
            return DynValue.NewBoolean(world.ShowSelectionHalo);
        });
        t["selected"] = DynValue.NewCallback((_, _) => EntityProxies.Tank(script, world.Selected, world));
        t["tanks"] = DynValue.NewCallback((_, _) => ListTanks(script, world, _ => true));
        t["players"] = DynValue.NewCallback((_, _) => ListTanks(script, world, tank => !tank.IsBoss && !tank.IsArenaCloser));
        t["bosses"] = DynValue.NewCallback((_, _) => ListTanks(script, world, tank => tank.IsBoss));
        t["closers"] = DynValue.NewCallback((_, _) => ListTanks(script, world, tank => tank.IsArenaCloser));
        t["shapes"] = DynValue.NewCallback((_, _) => ListShapes(script, world));
        t["bullets"] = DynValue.NewCallback((_, _) => ListBullets(script, world));
        t["count"] = DynValue.NewCallback((_, a) => DynValue.NewNumber(CountKind(world, a)));
        t["spawn_tank"] = DynValue.NewCallback((_, a) => SpawnTankFromArgs(script, world, a));
        t["spawn_boss"] = DynValue.NewCallback((_, a) =>
        {
            TankId? id = null;
            if (a.Count > 0 && a[0].Type == DataType.String && TankCatalog.TryParseId(a[0].String, out var parsed))
                id = parsed;
            return EntityProxies.Tank(script, world.SpawnBoss(id), world);
        });
        t["spawn_shape"] = DynValue.NewCallback((_, a) => SpawnShapeFromArgs(script, world, a));
        t["spawn_bullet"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1 || a[0].Type != DataType.Table)
                return DynValue.Nil;
            return SpawnBulletFromTable(script, world, a[0].Table);
        });
        t["close_arena"] = DynValue.NewCallback((_, _) =>
        {
            world.CloseArena();
            return DynValue.Nil;
        });
        t["reset"] = DynValue.NewCallback((_, _) =>
        {
            world.ResetKeepSize();
            return DynValue.Nil;
        });
        t["select"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1) return DynValue.Nil;
            if (a[0].Type == DataType.UserData && a[0].UserData.Object is TankProxy tp)
                world.SelectById(tp.id);
            else if (a[0].Type == DataType.Number)
                world.SelectTank((int)a[0].Number);
            return DynValue.Nil;
        });
        t["select_id"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.Number)
                world.SelectById((int)a[0].Number);
            return DynValue.Nil;
        });
        t["remove"] = DynValue.NewCallback((_, a) => DynValue.NewBoolean(RemoveEntity(world, a)));
        t["clear_shapes"] = DynValue.NewCallback((_, _) =>
        {
            world.ClearShapes();
            return DynValue.Nil;
        });
        t["clear_bullets"] = DynValue.NewCallback((_, _) =>
        {
            world.ClearBullets();
            return DynValue.Nil;
        });
        t["rng"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count >= 2 && a[0].Type == DataType.Number && a[1].Type == DataType.Number)
                return DynValue.NewNumber(world.ModRandom.NextDouble() * (a[1].Number - a[0].Number) + a[0].Number);
            return DynValue.NewNumber(world.ModRandom.NextDouble());
        });
        t["rng_int"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 2 || a[0].Type != DataType.Number || a[1].Type != DataType.Number)
                return DynValue.NewNumber(world.ModRandom.Next());
            var lo = (int)Math.Min(a[0].Number, a[1].Number);
            var hi = (int)Math.Max(a[0].Number, a[1].Number);
            return DynValue.NewNumber(world.ModRandom.Next(lo, hi + 1));
        });
        t["chance"] = DynValue.NewCallback((_, a) =>
        {
            var p = a.Count > 0 && a[0].Type == DataType.Number ? a[0].Number : 0.5;
            return DynValue.NewBoolean(world.ModRandom.NextDouble() < p);
        });
        t["collide_windows"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.Boolean)
                world.CollideWindows = a[0].Boolean;
            return DynValue.NewBoolean(world.CollideWindows);
        });
        t["collide_cursor"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.Boolean)
                world.CollideCursor = a[0].Boolean;
            return DynValue.NewBoolean(world.CollideCursor);
        });
        t["render_style"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.String && Enum.TryParse<RenderStyle>(a[0].String, true, out var style))
                world.RenderStyle = RenderLooks.Normalize(style);
            return DynValue.NewString(world.RenderStyle.ToString());
        });
        t["find_tank"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1 || a[0].Type != DataType.Number)
                return DynValue.Nil;
            return EntityProxies.Tank(script, world.FindTank((int)a[0].Number), world);
        });
        t["nearest_tank"] = DynValue.NewCallback((_, a) => NearestTank(script, world, a));
        t["nearest_shape"] = DynValue.NewCallback((_, a) => NearestShape(script, world, a));
        t["nearest_bullet"] = DynValue.NewCallback((_, a) => NearestBullet(script, world, a));
        t["in_radius"] = DynValue.NewCallback((_, a) => InRadius(script, world, a));
        t["cursor"] = DynValue.NewCallback((_, _) => DynValue.NewTable(CursorTable(script, world)));
        t["windows"] = DynValue.NewCallback((_, _) => DynValue.NewTable(WindowsTable(script, world)));
        t["can_see"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 4) return DynValue.True;
            if (!world.CollideWindows) return DynValue.True;
            return DynValue.NewBoolean(world.Windows.CanSee(a[0].Number, a[1].Number, a[2].Number, a[3].Number));
        });
        t["blocked"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 4) return DynValue.False;
            if (!world.CollideWindows) return DynValue.False;
            return DynValue.NewBoolean(world.Windows.Blocked(a[0].Number, a[1].Number, a[2].Number, a[3].Number));
        });
        t["boss_in"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.Number)
                world.BossSpawnIn = a[0].Number;
            return DynValue.NewNumber(world.BossSpawnIn);
        });
        return t;
    }

    public static Table BuildDraw(Script script, ModHost host)
    {
        var t = new Table(script);
        t["line"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 7) return DynValue.Nil;
            var color = Rgba(a, 4, 1);
            var width = a.Count > 8 && a[8].Type == DataType.Number ? a[8].Number : 3;
            host.Draw.Line(a[0].Number, a[1].Number, a[2].Number, a[3].Number, color, width);
            return DynValue.Nil;
        });
        t["circle"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 6) return DynValue.Nil;
            var color = Rgba(a, 3, 1);
            var filled = a.Count <= 7 || a[7].Type != DataType.Boolean || a[7].Boolean;
            host.Draw.Circle(a[0].Number, a[1].Number, a[2].Number, color, filled);
            return DynValue.Nil;
        });
        t["glow"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 6) return DynValue.Nil;
            host.Draw.Glow(a[0].Number, a[1].Number, a[2].Number, Rgba(a, 3, 0.55));
            return DynValue.Nil;
        });
        t["beam"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 8) return DynValue.Nil;
            var width = a[4].Number;
            var color = Rgba(a, 5, 1);
            host.Draw.Beam(a[0].Number, a[1].Number, a[2].Number, a[3].Number, width, color);
            return DynValue.Nil;
        });
        t["rect"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 7) return DynValue.Nil;
            var color = Rgba(a, 4, 1);
            var filled = a.Count <= 8 || a[8].Type != DataType.Boolean || a[8].Boolean;
            var stroke = a.Count > 9 && a[9].Type == DataType.Number ? a[9].Number : 2;
            host.Draw.Rect(a[0].Number, a[1].Number, a[2].Number, a[3].Number, color, filled, stroke);
            return DynValue.Nil;
        });
        t["triangle"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 9) return DynValue.Nil;
            var color = Rgba(a, 6, 1);
            var filled = a.Count <= 10 || a[10].Type != DataType.Boolean || a[10].Boolean;
            host.Draw.Triangle(a[0].Number, a[1].Number, a[2].Number, a[3].Number, a[4].Number, a[5].Number, color, filled);
            return DynValue.Nil;
        });
        t["poly"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 4) return DynValue.Nil;
            var pts = ParsePoints(a[0]);
            if (pts.Length < 2) return DynValue.Nil;
            var color = Rgba(a, 1, 1);
            var filled = a.Count <= 5 || a[5].Type != DataType.Boolean || a[5].Boolean;
            var stroke = a.Count > 6 && a[6].Type == DataType.Number ? a[6].Number : 2;
            host.Draw.Poly(pts, color, filled, stroke);
            return DynValue.Nil;
        });
        t["ring"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 7) return DynValue.Nil;
            host.Draw.Ring(a[0].Number, a[1].Number, a[2].Number, a[3].Number, Rgba(a, 4, 1));
            return DynValue.Nil;
        });
        t["text"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 6) return DynValue.Nil;
            var text = a[2].CastToString() ?? "";
            var size = a[3].Type == DataType.Number ? a[3].Number : 14;
            host.Draw.Text(a[0].Number, a[1].Number, text, size, Rgba(a, 4, 1));
            return DynValue.Nil;
        });
        return t;
    }

    public static Table BuildCatalog(Script script)
    {
        var t = new Table(script);
        t["list"] = DynValue.NewCallback((_, _) => StringList(script, TankCatalog.Playable.Select(d => TankCatalog.KeyOf(d.Id))));
        t["bosses"] = DynValue.NewCallback((_, _) => StringList(script, TankCatalog.BossList.Select(d => TankCatalog.KeyOf(d.Id))));
        t["shapes"] = DynValue.NewCallback((_, _) => StringList(script, Enum.GetNames<ShapeKind>()));
        t["projectiles"] = DynValue.NewCallback((_, _) => StringList(script, Enum.GetNames<ProjectileKind>()));
        t["stats"] = DynValue.NewCallback((_, _) =>
        {
            var list = new Table(script);
            for (var i = 0; i < TankStats.Names.Length; i++)
                list[i + 1] = TankStats.Names[i];
            return DynValue.NewTable(list);
        });
        t["get"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1 || a[0].Type != DataType.String)
                return DynValue.Nil;
            if (!TankCatalog.TryParseId(a[0].String, out var id) || !TankCatalog.TryGet(id, out var def))
                return DynValue.Nil;
            return DynValue.NewTable(TankDefToTable(script, def));
        });
        t["upgrades"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1 || a[0].Type != DataType.String)
                return DynValue.Nil;
            if (!TankCatalog.TryParseId(a[0].String, out var id) || !TankCatalog.TryGet(id, out var def))
                return DynValue.Nil;
            return StringList(script, def.Upgrades.Select(TankCatalog.KeyOf));
        });
        t["register_tank"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1 || a[0].Type != DataType.Table)
                return DynValue.NewBoolean(false);
            var ok = TankCatalog.TryRegisterFromLua(a[0].Table, out var key);
            return ok ? DynValue.NewString(key) : DynValue.NewBoolean(false);
        });
        t["unregister"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1 || a[0].Type != DataType.String)
                return DynValue.NewBoolean(false);
            return DynValue.NewBoolean(TankCatalog.UnregisterMod(a[0].String));
        });
        return t;
    }

    public static Table BuildNotify(Script script, GameWorld world)
    {
        var t = new Table(script);
        t["flash"] = DynValue.NewCallback((_, a) =>
        {
            var text = a.Count > 0 ? a[0].CastToString() ?? "" : "";
            var sec = a.Count > 1 && a[1].Type == DataType.Number ? a[1].Number : 1.4;
            world.Debug.Flash(text, sec);
            return DynValue.Nil;
        });
        t["server"] = DynValue.NewCallback((_, a) =>
        {
            var text = a.Count > 0 ? a[0].CastToString() ?? "" : "";
            var sec = a.Count > 1 && a[1].Type == DataType.Number ? a[1].Number : 4.5;
            world.Notifications.Server(text, sec);
            return DynValue.Nil;
        });
        t["arena"] = DynValue.NewCallback((_, a) =>
        {
            var text = a.Count > 0 ? a[0].CastToString() ?? "" : "";
            var sec = a.Count > 1 && a[1].Type == DataType.Number ? a[1].Number : 6;
            world.Notifications.Arena(text, sec);
            return DynValue.Nil;
        });
        t["mode"] = DynValue.NewCallback((_, a) =>
        {
            var text = a.Count > 0 ? a[0].CastToString() ?? "" : "";
            var sec = a.Count > 1 && a[1].Type == DataType.Number ? a[1].Number : 4.5;
            world.Notifications.Mode(text, sec);
            return DynValue.Nil;
        });
        t["push"] = DynValue.NewCallback((_, a) =>
        {
            var text = a.Count > 0 ? a[0].CastToString() ?? "" : "";
            var r = a.Count > 1 && a[1].Type == DataType.Number ? a[1].Number : 0.5;
            var g = a.Count > 2 && a[2].Type == DataType.Number ? a[2].Number : 0.5;
            var b = a.Count > 3 && a[3].Type == DataType.Number ? a[3].Number : 0.5;
            var sec = a.Count > 4 && a[4].Type == DataType.Number ? a[4].Number : 4.5;
            var id = a.Count > 5 ? a[5].CastToString() ?? "" : "";
            world.Notifications.Push(text, Color.FromRgb(ToByte(r), ToByte(g), ToByte(b)), sec, id);
            return DynValue.Nil;
        });
        t["clear"] = DynValue.NewCallback((_, _) =>
        {
            world.Notifications.Clear();
            return DynValue.Nil;
        });
        return t;
    }

    public static Table BuildUtil(Script script, LoadedMod mod)
    {
        var t = new Table(script);
        t["pi"] = Math.PI;
        t["tau"] = Math.PI * 2;
        t["log"] = DynValue.NewCallback((_, a) =>
        {
            var msg = a.Count > 0 ? a[0].CastToString() ?? "" : "";
            ModHost.Current?.Log($"[{mod.Manifest.Id}] {msg}");
            return DynValue.Nil;
        });
        t["dist"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 4) return DynValue.NewNumber(0);
            var dx = a[2].Number - a[0].Number;
            var dy = a[3].Number - a[1].Number;
            return DynValue.NewNumber(Math.Sqrt(dx * dx + dy * dy));
        });
        t["angle"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 4) return DynValue.NewNumber(0);
            return DynValue.NewNumber(Math.Atan2(a[3].Number - a[1].Number, a[2].Number - a[0].Number));
        });
        t["clamp"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 3) return DynValue.NewNumber(0);
            return DynValue.NewNumber(Math.Clamp(a[0].Number, a[1].Number, a[2].Number));
        });
        t["lerp"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 3) return DynValue.NewNumber(0);
            return DynValue.NewNumber(a[0].Number + (a[1].Number - a[0].Number) * a[2].Number);
        });
        t["lerp_angle"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 3) return DynValue.NewNumber(0);
            return DynValue.NewNumber(Interp.LerpAngle(a[0].Number, a[1].Number, a[2].Number));
        });
        t["wrap_angle"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1) return DynValue.NewNumber(0);
            return DynValue.NewNumber(Math2.NormalizeAngle(a[0].Number));
        });
        t["deg"] = DynValue.NewCallback((_, a) =>
            DynValue.NewNumber(a.Count > 0 ? a[0].Number * (180.0 / Math.PI) : 0));
        t["rad"] = DynValue.NewCallback((_, a) =>
            DynValue.NewNumber(a.Count > 0 ? a[0].Number * (Math.PI / 180.0) : 0));
        t["now"] = DynValue.NewCallback((_, _) =>
            DynValue.NewNumber(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0));
        t["team_color"] = DynValue.NewCallback((_, a) =>
        {
            var i = a.Count > 0 && a[0].Type == DataType.Number ? (int)a[0].Number : 0;
            return DynValue.NewTable(ColorTable(script, DiepColors.Team(i)));
        });
        t["color"] = DynValue.NewCallback((_, a) =>
        {
            var name = a.Count > 0 ? a[0].CastToString() ?? "" : "";
            return DynValue.NewTable(ColorTable(script, NamedColor(name)));
        });
        t["json_encode"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1) return DynValue.NewString("{}");
            return DynValue.NewString(LuaJson.Encode(a[0]));
        });
        t["json_decode"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1 || a[0].Type != DataType.String)
                return DynValue.Nil;
            return LuaJson.Decode(script, a[0].String);
        });
        t["read_data"] = DynValue.NewCallback((_, a) =>
        {
            var name = a.Count > 0 ? a[0].CastToString() ?? "save.json" : "save.json";
            var path = SafeDataPath(mod, name);
            if (path is null || !File.Exists(path))
                return DynValue.Nil;
            return DynValue.NewString(File.ReadAllText(path));
        });
        t["write_data"] = DynValue.NewCallback((_, a) =>
        {
            var name = a.Count > 0 ? a[0].CastToString() ?? "save.json" : "save.json";
            var body = a.Count > 1 ? a[1].CastToString() ?? "" : "";
            var path = SafeDataPath(mod, name);
            if (path is null) return DynValue.NewBoolean(false);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, body);
            return DynValue.NewBoolean(true);
        });
        t["exists"] = DynValue.NewCallback((_, a) =>
        {
            var name = a.Count > 0 ? a[0].CastToString() ?? "" : "";
            var path = SafeDataPath(mod, name);
            return DynValue.NewBoolean(path is not null && File.Exists(path));
        });
        t["list_data"] = DynValue.NewCallback((_, _) =>
        {
            Directory.CreateDirectory(mod.DataPath);
            return StringList(script, Directory.EnumerateFiles(mod.DataPath)
                .Select(f => Path.GetFileName(f) ?? ""));
        });
        t["read_file"] = DynValue.NewCallback((_, a) =>
        {
            var name = a.Count > 0 ? a[0].CastToString() ?? "" : "";
            var path = SafeModPath(mod, name);
            if (path is null || !File.Exists(path))
                return DynValue.Nil;
            return DynValue.NewString(File.ReadAllText(path));
        });
        return t;
    }

    public static Table BuildTimers(Script script, ModHost host, LoadedMod mod)
    {
        var t = new Table(script);
        t["after"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 2 || a[0].Type != DataType.Number || a[1].Type != DataType.Function)
                return DynValue.Nil;
            return DynValue.NewNumber(host.Timers.After(mod, a[0].Number, a[1]));
        });
        t["every"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 2 || a[0].Type != DataType.Number || a[1].Type != DataType.Function)
                return DynValue.Nil;
            return DynValue.NewNumber(host.Timers.Every(mod, a[0].Number, a[1]));
        });
        t["cancel"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.Number)
                host.Timers.Cancel((int)a[0].Number);
            return DynValue.Nil;
        });
        return t;
    }

    public static Table BuildEvents(Script script, ModHost host, LoadedMod mod)
    {
        var t = new Table(script);
        t["on"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 2 || a[0].Type != DataType.String)
                return DynValue.Nil;
            host.Events.On(mod, a[0].String, a[1]);
            return DynValue.Nil;
        });
        t["off"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count > 0 && a[0].Type == DataType.String)
                host.Events.Off(mod, a[0].String);
            else
                host.Events.Off(mod);
            return DynValue.Nil;
        });
        t["emit"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1 || a[0].Type != DataType.String)
                return DynValue.Nil;
            var args = new DynValue[Math.Max(0, a.Count - 1)];
            for (var i = 1; i < a.Count; i++)
                args[i - 1] = a[i];
            host.Events.Emit(a[0].String, args);
            return DynValue.Nil;
        });
        return t;
    }

    public static Table BuildInput(Script script, GameWorld world)
    {
        var t = new Table(script);
        t["cursor"] = DynValue.NewCallback((_, _) => DynValue.NewTable(CursorTable(script, world)));
        t["mouse"] = DynValue.NewCallback((_, a) =>
        {
            var name = a.Count > 0 ? a[0].CastToString() ?? "left" : "left";
            return DynValue.NewBoolean(KeyDown(MouseVk(name)));
        });
        t["key"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1) return DynValue.False;
            if (!TryVk(a[0], out var vk))
                return DynValue.False;
            return DynValue.NewBoolean(KeyDown(vk));
        });
        t["down"] = DynValue.NewCallback((_, a) =>
        {
            if (a.Count < 1) return DynValue.False;
            var name = a[0].CastToString() ?? "";
            var mouse = MouseVk(name);
            if (mouse != 0 && (name.Contains("mouse", StringComparison.OrdinalIgnoreCase) ||
                               name is "lmb" or "rmb" or "mmb" or "left" or "right" or "middle"))
            {
                if (name is "left" or "right")
                    return DynValue.NewBoolean(KeyDown(KeyVk(name)));
                return DynValue.NewBoolean(KeyDown(mouse));
            }
            return DynValue.NewBoolean(TryVk(a[0], out var vk) && KeyDown(vk));
        });
        return t;
    }

    public static Table BuildModTable(Script script, LoadedMod mod)
    {
        var t = new Table(script);
        t["id"] = mod.Manifest.Id;
        t["name"] = mod.Manifest.Name;
        t["version"] = mod.Manifest.Version;
        t["author"] = mod.Manifest.Author;
        t["path"] = mod.Folder;
        t["data"] = mod.DataPath;
        return t;
    }

    private static DynValue SpawnTankFromArgs(Script script, GameWorld world, CallbackArguments a)
    {
        double? x = null, y = null;
        TankId? classId = null;
        int? level = null;
        if (a.Count > 0 && a[0].Type == DataType.Table)
        {
            var opts = a[0].Table;
            if (NumOpt(opts, "x", out var sx)) x = sx;
            if (NumOpt(opts, "y", out var sy)) y = sy;
            var cls = opts.Get("class");
            if (cls.Type == DataType.String && TankCatalog.TryParseId(cls.String, out var parsed) &&
                TankCatalog.TryGet(parsed, out var def) && !def.IsBoss)
                classId = parsed;
            if (NumOpt(opts, "level", out var lv))
                level = (int)lv;
        }
        else
        {
            if (a.Count > 0 && a[0].Type == DataType.Number) x = a[0].Number;
            if (a.Count > 1 && a[1].Type == DataType.Number) y = a[1].Number;
            if (a.Count > 2 && a[2].Type == DataType.String && TankCatalog.TryParseId(a[2].String, out var parsed) &&
                TankCatalog.TryGet(parsed, out var def) && !def.IsBoss)
                classId = parsed;
        }

        var tank = world.SpawnTank(x, y, classId);
        if (tank is not null && level is int lvSet)
            world.ModSetLevel(tank, lvSet);
        return EntityProxies.Tank(script, tank, world);
    }

    private static DynValue SpawnShapeFromArgs(Script script, GameWorld world, CallbackArguments a)
    {
        ShapeKind? kind = null;
        double? x = null, y = null;
        var notify = true;
        Table? extras = null;
        if (a.Count > 0 && a[0].Type == DataType.Table)
        {
            extras = a[0].Table;
            var k = extras.Get("kind");
            if (k.Type == DataType.String && Enum.TryParse<ShapeKind>(k.String, true, out var parsed))
                kind = parsed;
            if (NumOpt(extras, "x", out var sx)) x = sx;
            if (NumOpt(extras, "y", out var sy)) y = sy;
            var n = extras.Get("notify");
            if (n.Type == DataType.Boolean)
                notify = n.Boolean;
        }
        else
        {
            if (a.Count > 0 && a[0].Type == DataType.String && Enum.TryParse<ShapeKind>(a[0].String, true, out var k))
                kind = k;
            if (a.Count > 1 && a[1].Type == DataType.Number) x = a[1].Number;
            if (a.Count > 2 && a[2].Type == DataType.Number) y = a[2].Number;
        }

        var shape = world.SpawnShape(kind, x, y, notify);
        if (extras is not null)
        {
            if (NumOpt(extras, "health", out var hp))
            {
                shape.MaxHealth = hp;
                shape.Health = hp;
            }
            if (NumOpt(extras, "radius", out var r))
                shape.Radius = r;
            if (NumOpt(extras, "xp", out var xp))
                shape.Xp = (int)xp;
            if (NumOpt(extras, "mass", out var mass))
                shape.Mass = mass;
            ApplyFill(extras, c => shape.Fill = c);
            shape.Snap();
        }
        return EntityProxies.Shape(script, shape, world);
    }

    private static DynValue SpawnBulletFromTable(Script script, GameWorld world, Table opts)
    {
        var x = Num(opts, "x");
        var y = Num(opts, "y");
        var angle = Num(opts, "angle", 0);
        var speed = Num(opts, "speed", 0);
        var vx = Num(opts, "vx", Math.Cos(angle) * speed);
        var vy = Num(opts, "vy", Math.Sin(angle) * speed);
        var radius = Num(opts, "radius", 8);
        var damage = Num(opts, "damage", 10);
        var health = Num(opts, "health", 20);
        var life = Num(opts, "life", 45);
        var visible = opts.Get("visible").Type != DataType.Boolean || opts.Get("visible").Boolean;
        var ownerId = 0;
        var owner = opts.Get("owner");
        if (owner.Type == DataType.UserData && owner.UserData.Object is TankProxy tp)
            ownerId = tp.id;
        else if (owner.Type == DataType.Number)
            ownerId = (int)owner.Number;

        var kind = ProjectileKind.Bullet;
        if (opts.Get("kind").Type == DataType.String &&
            Enum.TryParse<ProjectileKind>(opts.Get("kind").String, true, out var parsed))
            kind = parsed;

        var fill = DiepColors.Tank;
        var fillT = opts.Get("fill");
        if (fillT.Type == DataType.Table)
        {
            var ft = fillT.Table;
            fill = Color.FromRgb(ToByte(Num(ft, "r", 1)), ToByte(Num(ft, "g", 0.85)), ToByte(Num(ft, "b", 0.2)));
        }
        else if (opts.Get("r").Type == DataType.Number)
        {
            fill = Color.FromRgb(
                ToByte(Num(opts, "r", 1)),
                ToByte(Num(opts, "g", 0.85)),
                ToByte(Num(opts, "b", 0.2)));
        }

        var shot = world.SpawnBulletFromMod(x, y, vx, vy, angle, radius, damage, health, life, ownerId, kind, fill, visible);
        return EntityProxies.Bullet(script, shot, world);
    }

    private static bool RemoveEntity(GameWorld world, CallbackArguments a)
    {
        if (a.Count < 1 || a[0].Type != DataType.UserData)
            return false;
        return a[0].UserData.Object switch
        {
            TankProxy tank => world.TryRemoveTank(tank.Raw),
            ShapeProxy shape => KillOrDropShape(world, shape.Raw),
            BulletProxy bullet => KillBullet(bullet.Raw),
            _ => false
        };
    }

    private static bool KillOrDropShape(GameWorld world, ShapeEntity s)
    {
        world.KillShape(s, null);
        return true;
    }

    private static bool KillBullet(BulletEntity b)
    {
        b.Health = 0;
        b.Life = 0;
        return true;
    }

    private static int CountKind(GameWorld world, CallbackArguments a)
    {
        var kind = a.Count > 0 ? a[0].CastToString() ?? "" : "";
        return kind.ToLowerInvariant() switch
        {
            "tank" or "tanks" => world.Tanks.Count,
            "player" or "players" => world.Tanks.Count(t => !t.IsBoss && !t.IsArenaCloser),
            "boss" or "bosses" => world.Tanks.Count(t => t.IsBoss),
            "shape" or "shapes" => world.Shapes.Count(s => !s.Destroy.Active),
            "bullet" or "bullets" => world.Bullets.Count(b => !b.Destroy.Active),
            _ => world.Tanks.Count + world.Shapes.Count(s => !s.Destroy.Active) + world.Bullets.Count(b => !b.Destroy.Active)
        };
    }

    private static DynValue NearestTank(Script script, GameWorld world, CallbackArguments a)
    {
        if (a.Count < 2) return DynValue.Nil;
        var x = a[0].Number;
        var y = a[1].Number;
        var max = a.Count > 2 && a[2].Type == DataType.Number ? a[2].Number : double.PositiveInfinity;
        var exclude = a.Count > 3 && a[3].Type == DataType.Number ? (int)a[3].Number : -1;
        TankEntity? best = null;
        var bestD = max * max;
        foreach (var tank in world.Tanks)
        {
            if (!tank.Alive || tank.Destroy.Active || tank.Id == exclude)
                continue;
            var d = Dist2(x, y, tank.X, tank.Y);
            if (d > bestD) continue;
            bestD = d;
            best = tank;
        }
        return EntityProxies.Tank(script, best, world);
    }

    private static DynValue NearestShape(Script script, GameWorld world, CallbackArguments a)
    {
        if (a.Count < 2) return DynValue.Nil;
        var x = a[0].Number;
        var y = a[1].Number;
        var max = a.Count > 2 && a[2].Type == DataType.Number ? a[2].Number : double.PositiveInfinity;
        ShapeEntity? best = null;
        var bestD = max * max;
        foreach (var s in world.Shapes)
        {
            if (s.Destroy.Active) continue;
            var d = Dist2(x, y, s.X, s.Y);
            if (d > bestD) continue;
            bestD = d;
            best = s;
        }
        return EntityProxies.Shape(script, best, world);
    }

    private static DynValue NearestBullet(Script script, GameWorld world, CallbackArguments a)
    {
        if (a.Count < 2) return DynValue.Nil;
        var x = a[0].Number;
        var y = a[1].Number;
        var max = a.Count > 2 && a[2].Type == DataType.Number ? a[2].Number : double.PositiveInfinity;
        BulletEntity? best = null;
        var bestD = max * max;
        foreach (var b in world.Bullets)
        {
            if (b.Destroy.Active) continue;
            var d = Dist2(x, y, b.X, b.Y);
            if (d > bestD) continue;
            bestD = d;
            best = b;
        }
        return EntityProxies.Bullet(script, best, world);
    }

    private static DynValue InRadius(Script script, GameWorld world, CallbackArguments a)
    {
        var list = new Table(script);
        if (a.Count < 3) return DynValue.NewTable(list);
        var x = a[0].Number;
        var y = a[1].Number;
        var r = a[2].Number;
        var filter = a.Count > 3 ? a[3].CastToString() ?? "" : "";
        var r2 = r * r;
        var i = 1;
        var wantTank = string.IsNullOrEmpty(filter) || filter.Equals("tank", StringComparison.OrdinalIgnoreCase);
        var wantShape = string.IsNullOrEmpty(filter) || filter.Equals("shape", StringComparison.OrdinalIgnoreCase);
        var wantBullet = string.IsNullOrEmpty(filter) || filter.Equals("bullet", StringComparison.OrdinalIgnoreCase);
        if (wantTank)
        {
            foreach (var tank in world.Tanks)
            {
                if (!tank.Alive || tank.Destroy.Active) continue;
                if (Dist2(x, y, tank.X, tank.Y) <= r2)
                    list[i++] = EntityProxies.Tank(script, tank, world);
            }
        }
        if (wantShape)
        {
            foreach (var s in world.Shapes)
            {
                if (s.Destroy.Active) continue;
                if (Dist2(x, y, s.X, s.Y) <= r2)
                    list[i++] = EntityProxies.Shape(script, s, world);
            }
        }
        if (wantBullet)
        {
            foreach (var b in world.Bullets)
            {
                if (b.Destroy.Active) continue;
                if (Dist2(x, y, b.X, b.Y) <= r2)
                    list[i++] = EntityProxies.Bullet(script, b, world);
            }
        }
        return DynValue.NewTable(list);
    }

    private static Table CursorTable(Script script, GameWorld world)
    {
        var c = world.Cursor;
        return new Table(script)
        {
            ["x"] = c.X,
            ["y"] = c.Y,
            ["vx"] = c.Vx,
            ["vy"] = c.Vy,
            ["down"] = c.Down,
            ["hovering"] = c.Hovering,
            ["grabbing"] = c.Grabbing
        };
    }

    private static Table WindowsTable(Script script, GameWorld world)
    {
        var list = new Table(script);
        var i = 1;
        foreach (var box in world.Windows.Boxes)
        {
            list[i++] = new Table(script)
            {
                ["left"] = box.Left,
                ["top"] = box.Top,
                ["right"] = box.Right,
                ["bottom"] = box.Bottom,
                ["x"] = (box.Left + box.Right) * 0.5,
                ["y"] = (box.Top + box.Bottom) * 0.5,
                ["w"] = box.Right - box.Left,
                ["h"] = box.Bottom - box.Top
            };
        }
        return list;
    }

    private static DynValue ListTanks(Script script, GameWorld world, Func<TankEntity, bool> pred)
    {
        var list = new Table(script);
        var i = 1;
        foreach (var tank in world.Tanks)
        {
            if (!pred(tank)) continue;
            list[i++] = EntityProxies.Tank(script, tank, world);
        }
        return DynValue.NewTable(list);
    }

    private static DynValue ListShapes(Script script, GameWorld world)
    {
        var list = new Table(script);
        var i = 1;
        foreach (var s in world.Shapes)
        {
            if (s.Destroy.Active) continue;
            list[i++] = EntityProxies.Shape(script, s, world);
        }
        return DynValue.NewTable(list);
    }

    private static DynValue ListBullets(Script script, GameWorld world)
    {
        var list = new Table(script);
        var i = 1;
        foreach (var b in world.Bullets)
        {
            if (b.Destroy.Active) continue;
            list[i++] = EntityProxies.Bullet(script, b, world);
        }
        return DynValue.NewTable(list);
    }

    private static Table TankDefToTable(Script script, TankDef def)
    {
        var upgrades = new Table(script);
        for (var i = 0; i < def.Upgrades.Length; i++)
            upgrades[i + 1] = TankCatalog.KeyOf(def.Upgrades[i]);
        var barrels = new Table(script);
        for (var i = 0; i < def.Barrels.Length; i++)
        {
            var b = def.Barrels[i];
            barrels[i + 1] = new Table(script)
            {
                ["angle"] = b.Angle,
                ["offset"] = b.Offset,
                ["size"] = b.Size,
                ["width"] = b.Width,
                ["reload"] = b.Reload,
                ["recoil"] = b.Recoil,
                ["projectile"] = b.Bullet.Type.ToString()
            };
        }
        return new Table(script)
        {
            ["id"] = TankCatalog.KeyOf(def.Id),
            ["name"] = def.Name,
            ["level"] = def.LevelRequirement,
            ["sides"] = def.Sides,
            ["speed"] = def.Speed,
            ["is_boss"] = def.IsBoss,
            ["pre_addon"] = def.PreAddon ?? "",
            ["post_addon"] = def.PostAddon ?? "",
            ["upgrades"] = upgrades,
            ["barrels"] = barrels
        };
    }

    private static DynValue StringList(Script script, IEnumerable<string> items)
    {
        var list = new Table(script);
        var i = 1;
        foreach (var item in items)
            list[i++] = item;
        return DynValue.NewTable(list);
    }

    private static Table ColorTable(Script script, Color c) => new(script)
    {
        ["r"] = c.R / 255.0,
        ["g"] = c.G / 255.0,
        ["b"] = c.B / 255.0
    };

    private static Color NamedColor(string name) => name.Trim().ToLowerInvariant() switch
    {
        "tank" or "blue" => DiepColors.Tank,
        "square" or "yellow" => DiepColors.Square,
        "triangle" or "red" => DiepColors.Triangle,
        "pentagon" => DiepColors.Pentagon,
        "alpha" or "alphapentagon" => DiepColors.AlphaPentagon,
        "crasher" or "pink" => DiepColors.Crasher,
        "fallen" or "grey" or "gray" => DiepColors.Fallen,
        "health" or "green" => DiepColors.Health,
        "xp" or "gold" => DiepColors.Xp,
        "damage" => DiepColors.Damage,
        "necro" => DiepColors.NecroSquare,
        "neutral" => DiepColors.Neutral,
        "purple" => DiepColors.TeamPurple,
        _ => DiepColors.Tank
    };

    private static Point[] ParsePoints(DynValue v)
    {
        if (v.Type != DataType.Table)
            return [];
        var nums = new List<double>();
        var pts = new List<Point>();
        foreach (var pair in v.Table.Values)
        {
            if (pair.Type == DataType.Number)
            {
                nums.Add(pair.Number);
                continue;
            }
            if (pair.Type != DataType.Table)
                continue;
            var p = pair.Table;
            if (p.Get("x").Type == DataType.Number)
                pts.Add(new Point(p.Get("x").Number, Num(p, "y")));
            else if (p.Get(1).Type == DataType.Number)
                pts.Add(new Point(p.Get(1).Number, p.Get(2).Type == DataType.Number ? p.Get(2).Number : 0));
        }
        if (pts.Count > 0)
            return [.. pts];
        for (var i = 0; i + 1 < nums.Count; i += 2)
            pts.Add(new Point(nums[i], nums[i + 1]));
        return [.. pts];
    }

    private static Color Rgba(CallbackArguments a, int rgbStart, double defaultAlpha)
    {
        var r = a[rgbStart].Number;
        var g = a[rgbStart + 1].Number;
        var b = a[rgbStart + 2].Number;
        var alpha = a.Count > rgbStart + 3 && a[rgbStart + 3].Type == DataType.Number
            ? a[rgbStart + 3].Number
            : defaultAlpha;
        return Color.FromArgb(ToByte(alpha <= 1 && alpha >= 0 ? alpha * 255 : alpha), ToByte(r), ToByte(g), ToByte(b));
    }

    private static double Num(Table t, string key, double fallback = 0)
    {
        var v = t.Get(key);
        return v.Type == DataType.Number ? v.Number : fallback;
    }

    private static bool NumOpt(Table t, string key, out double value)
    {
        var v = t.Get(key);
        if (v.Type == DataType.Number)
        {
            value = v.Number;
            return true;
        }
        value = 0;
        return false;
    }

    private static void ApplyFill(Table opts, Action<Color> set)
    {
        var fillT = opts.Get("fill");
        if (fillT.Type == DataType.Table)
        {
            var ft = fillT.Table;
            set(Color.FromRgb(ToByte(Num(ft, "r", 1)), ToByte(Num(ft, "g", 1)), ToByte(Num(ft, "b", 1))));
        }
        else if (opts.Get("r").Type == DataType.Number)
        {
            set(Color.FromRgb(ToByte(Num(opts, "r", 1)), ToByte(Num(opts, "g", 1)), ToByte(Num(opts, "b", 1))));
        }
    }

    private static double Dist2(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return dx * dx + dy * dy;
    }

    private static string? SafeDataPath(LoadedMod mod, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains("..") || name.Contains('/') || name.Contains('\\') || name.Contains(':'))
            return null;
        Directory.CreateDirectory(mod.DataPath);
        return Path.Combine(mod.DataPath, name);
    }

    private static string? SafeModPath(LoadedMod mod, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains("..") || name.Contains(':') || Path.IsPathRooted(name))
            return null;
        var combined = Path.GetFullPath(Path.Combine(mod.Folder, name.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(mod.Folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(combined, Path.GetFullPath(mod.Folder), StringComparison.OrdinalIgnoreCase))
            return null;
        return combined;
    }

    private static bool KeyDown(int vk) => vk != 0 && (Win32.GetAsyncKeyState(vk) & 0x8000) != 0;

    private static int MouseVk(string name) => name.Trim().ToLowerInvariant() switch
    {
        "left" or "lmb" or "mouse1" => Win32.VkLButton,
        "right" or "rmb" or "mouse2" => 0x02,
        "middle" or "mmb" or "mouse3" => 0x04,
        _ => 0
    };

    private static int KeyVk(string name) => name.Trim().ToLowerInvariant() switch
    {
        "left" => 0x25,
        "up" => 0x26,
        "right" => 0x27,
        "down" => 0x28,
        _ => 0
    };

    private static bool TryVk(DynValue v, out int vk)
    {
        if (v.Type == DataType.Number)
        {
            vk = (int)v.Number;
            return vk > 0;
        }
        vk = 0;
        if (v.Type != DataType.String)
            return false;
        var s = v.String.Trim();
        if (s.Length == 1)
        {
            var c = char.ToUpperInvariant(s[0]);
            if (c is >= 'A' and <= 'Z') { vk = c; return true; }
            if (c is >= '0' and <= '9') { vk = c; return true; }
        }
        vk = s.ToLowerInvariant() switch
        {
            "space" => 0x20,
            "shift" => 0x10,
            "ctrl" or "control" => 0x11,
            "alt" => 0x12,
            "tab" => 0x09,
            "enter" or "return" => 0x0D,
            "escape" or "esc" => 0x1B,
            "backspace" => 0x08,
            "left" => 0x25,
            "up" => 0x26,
            "right" => 0x27,
            "down" => 0x28,
            _ => MouseVk(s)
        };
        return vk != 0;
    }

    private static byte ToByte(double t) =>
        (byte)Math.Clamp(t <= 1.0 && t >= 0 ? t * 255.0 : t, 0, 255);
}

internal static class LuaJson
{
    public static string Encode(DynValue value)
    {
        try
        {
            return JsonSerializer.Serialize(ToObject(value));
        }
        catch
        {
            return "null";
        }
    }

    public static DynValue Decode(Script script, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return FromElement(script, doc.RootElement);
        }
        catch
        {
            return DynValue.Nil;
        }
    }

    private static object? ToObject(DynValue v) => v.Type switch
    {
        DataType.Nil => null,
        DataType.Boolean => v.Boolean,
        DataType.Number => v.Number,
        DataType.String => v.String,
        DataType.Table => TableToDict(v.Table),
        _ => v.ToString()
    };

    private static Dictionary<string, object?> TableToDict(Table table)
    {
        var d = new Dictionary<string, object?>();
        foreach (var pair in table.Pairs)
        {
            var key = pair.Key.Type == DataType.String ? pair.Key.String
                : pair.Key.Type == DataType.Number ? pair.Key.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : pair.Key.ToString();
            d[key ?? "?"] = ToObject(pair.Value);
        }
        return d;
    }

    private static DynValue FromElement(Script script, JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Null => DynValue.Nil,
        JsonValueKind.True => DynValue.True,
        JsonValueKind.False => DynValue.False,
        JsonValueKind.Number => el.TryGetInt64(out var i) ? DynValue.NewNumber(i) : DynValue.NewNumber(el.GetDouble()),
        JsonValueKind.String => DynValue.NewString(el.GetString()),
        JsonValueKind.Array => FromArray(script, el),
        JsonValueKind.Object => FromObject(script, el),
        _ => DynValue.Nil
    };

    private static DynValue FromArray(Script script, JsonElement el)
    {
        var t = new Table(script);
        var i = 1;
        foreach (var item in el.EnumerateArray())
            t[i++] = FromElement(script, item);
        return DynValue.NewTable(t);
    }

    private static DynValue FromObject(Script script, JsonElement el)
    {
        var t = new Table(script);
        foreach (var prop in el.EnumerateObject())
            t[prop.Name] = FromElement(script, prop.Value);
        return DynValue.NewTable(t);
    }
}
