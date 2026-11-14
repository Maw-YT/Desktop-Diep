using System.Windows.Media;

namespace DesktopDiep;

public enum GameMode
{
    Ffa,
    Teams,
    FourTeams,
    Domination,
    Mothership,
    Survival,
    Maze,
    Sandbox
}

internal static class Teams
{
    public const int Blue = 1;
    public const int Red = 2;
    public const int Purple = 3;
    public const int Green = 4;

    public static readonly int[] Two = [Blue, Red];
    public static readonly int[] Four = [Blue, Red, Purple, Green];

    public static bool Allied(int a, int b) => a != 0 && a == b;

    public static System.Windows.Media.Color Color(int team) => team switch
    {
        Blue => System.Windows.Media.Color.FromRgb(0x00, 0xB2, 0xE1),
        Red => System.Windows.Media.Color.FromRgb(0xF1, 0x4E, 0x54),
        Purple => System.Windows.Media.Color.FromRgb(0xBF, 0x7F, 0xF5),
        Green => System.Windows.Media.Color.FromRgb(0x00, 0xE1, 0x6E),
        _ => DiepColors.Tank
    };

    public static string Name(int team) => team switch
    {
        Blue => "Blue",
        Red => "Red",
        Purple => "Purple",
        Green => "Green",
        _ => ""
    };

    public static int[] For(GameMode mode) => mode switch
    {
        GameMode.Teams or GameMode.Domination or GameMode.Mothership => Two,
        GameMode.FourTeams => Four,
        _ => []
    };
}

internal static class GameModes
{
    public static string Id(GameMode mode) => mode switch
    {
        GameMode.Teams => "teams",
        GameMode.FourTeams => "4teams",
        GameMode.Domination => "dom",
        GameMode.Mothership => "mot",
        GameMode.Survival => "survival",
        GameMode.Maze => "maze",
        GameMode.Sandbox => "sandbox",
        _ => "ffa"
    };

    public static string Label(GameMode mode) => mode switch
    {
        GameMode.Teams => "2 Teams",
        GameMode.FourTeams => "4 Teams",
        GameMode.Domination => "Domination",
        GameMode.Mothership => "Mothership",
        GameMode.Survival => "Survival",
        GameMode.Maze => "Maze",
        GameMode.Sandbox => "Sandbox",
        _ => "FFA"
    };

    public static GameMode Parse(string? id) => id?.Trim().ToLowerInvariant() switch
    {
        "teams" or "2teams" or "2tdm" => GameMode.Teams,
        "4teams" or "4tdm" => GameMode.FourTeams,
        "dom" or "domination" => GameMode.Domination,
        "mot" or "mothership" => GameMode.Mothership,
        "survival" => GameMode.Survival,
        "maze" => GameMode.Maze,
        "sandbox" => GameMode.Sandbox,
        _ => GameMode.Ffa
    };

    public static bool UsesTeams(GameMode mode) => Teams.For(mode).Length > 0;

    public static int XpMultiplier(GameMode mode) => mode switch
    {
        GameMode.Domination => 2,
        GameMode.Mothership or GameMode.Survival => 3,
        _ => 1
    };

    public static void Rebuild(GameWorld world, bool respawnShips)
    {
        for (var i = world.Objects.Count - 1; i >= 0; i--)
        {
            if (world.Objects[i].ModeOwned)
                world.Objects.RemoveAt(i);
        }

        if (world.Width > 40 && world.Height > 40)
        {
            switch (world.Mode)
            {
                case GameMode.Teams:
                    BuildEdgeBases(world, Teams.Two);
                    break;
                case GameMode.FourTeams:
                    BuildCornerBases(world, Teams.Four);
                    break;
                case GameMode.Domination:
                    BuildCornerBases(world, Teams.Two);
                    BuildDominators(world);
                    break;
            }
            if (world.Mode == GameMode.Maze)
                BuildMaze(world);
        }

        if (respawnShips)
            ReplaceMotherships(world);

        var allowed = Teams.For(world.Mode);
        foreach (var tank in world.Tanks)
        {
            if (!tank.IsPet)
                continue;
            if (allowed.Length == 0)
            {
                world.SetTeam(tank, 0);
                continue;
            }
            if (Array.IndexOf(allowed, tank.Team) >= 0)
                continue;
            world.SetTeam(tank, PickTeam(world));
        }
        Unstick(world);
    }

    public static void OnSpawn(GameWorld world, TankEntity tank)
    {
        if (!tank.IsPet)
            return;
        world.SetTeam(tank, PickTeam(world));
    }

    public static int PickTeam(GameWorld world)
    {
        var teams = Teams.For(world.Mode);
        if (teams.Length == 0)
            return 0;
        var best = teams[0];
        var bestCount = int.MaxValue;
        foreach (var team in teams)
        {
            var n = 0;
            foreach (var tank in world.Tanks)
            {
                if (tank.IsPet && tank.Team == team)
                    n++;
            }
            if (n < bestCount)
            {
                bestCount = n;
                best = team;
            }
        }
        return best;
    }

    public static void Tick(GameWorld world, double dt)
    {
        foreach (var obj in world.Objects)
        {
            if (!obj.Alive)
                continue;
            obj.Angle += obj.Spin;
            if (obj.Vx != 0 || obj.Vy != 0)
            {
                obj.X += obj.Vx;
                obj.Y += obj.Vy;
                obj.Vx *= 0.9;
                obj.Vy *= 0.9;
            }
        }

        TickZones(world);
        if (world.Mode == GameMode.Domination)
            TickDominators(world);
        if (world.Mode == GameMode.Survival)
            TickSurvival(world, dt);
        ModContent.TickObjects(world, dt);
    }

    public static (double X, double Y) MothershipHome(GameWorld world, int team)
    {
        var x = team == Teams.Red ? world.Width * 0.82 : world.Width * 0.18;
        return (x, world.Height * 0.5);
    }

    private static void TickZones(GameWorld world)
    {
        foreach (var obj in world.Objects)
        {
            if (!obj.Alive || obj.Kind == "dominator")
                continue;
            if (obj.DamagePerTick <= 0 && obj.HealPerTick <= 0)
                continue;
            foreach (var tank in world.Tanks)
            {
                if (!tank.Alive || tank.Destroy.Active || tank.IsArenaCloser)
                    continue;
                if (!obj.Hits(tank.X, tank.Y, tank.Radius))
                    continue;
                if (Teams.Allied(obj.Team, tank.Team))
                {
                    if (obj.HealPerTick > 0)
                        tank.Health = Math.Min(tank.MaxHealth, tank.Health + obj.HealPerTick);
                    continue;
                }
                if (obj.DamagePerTick > 0 && obj.Team != 0)
                    world.HurtTank(tank, obj.DamagePerTick, null);
            }

            if (obj.Team == 0 || obj.DamagePerTick <= 0)
                continue;
            foreach (var bullet in world.Bullets)
            {
                if (bullet.Health <= 0 || bullet.Destroy.Active)
                    continue;
                if (Teams.Allied(obj.Team, bullet.Team) || !obj.Hits(bullet.X, bullet.Y, bullet.Radius))
                    continue;
                bullet.Health = 0;
                bullet.Life = 0;
            }
        }
    }

    private static void TickDominators(GameWorld world)
    {
        foreach (var obj in world.Objects)
        {
            if (!obj.Alive || obj.Kind != "dominator")
                continue;
            TankEntity? attacker = null;
            foreach (var tank in world.Tanks)
            {
                if (!tank.Alive || tank.Destroy.Active || tank.IsArenaCloser || tank.Team == 0)
                    continue;
                if (!obj.Hits(tank.X, tank.Y, tank.Radius))
                    continue;
                if (Teams.Allied(obj.Team, tank.Team))
                {
                    tank.Health = Math.Min(tank.MaxHealth, tank.Health + 0.25);
                    continue;
                }
                obj.Health -= Math.Max(0.4, TankStats.BodyDamage(tank) * 0.08);
                attacker = tank;
                if (obj.Team != 0)
                    world.HurtTank(tank, 0.35, null);
            }

            foreach (var bullet in world.Bullets)
            {
                if (bullet.Health <= 0 || bullet.Destroy.Active || bullet.Team == 0)
                    continue;
                if (!obj.Hits(bullet.X, bullet.Y, bullet.Radius))
                    continue;
                if (Teams.Allied(obj.Team, bullet.Team))
                    continue;
                obj.Health -= Math.Max(0.5, bullet.Damage * 0.04);
                bullet.Health = 0;
                bullet.Life = 0;
                attacker = world.FindTank(bullet.OwnerId) ?? attacker;
            }

            if (obj.Health <= 0 && attacker is { Team: > 0 })
            {
                obj.Team = attacker.Team;
                obj.Fill = Teams.Color(obj.Team);
                obj.Health = obj.MaxHealth;
                world.Notifications.Mode($"{obj.Label} captured by {Teams.Name(obj.Team)}", 4);
            }
            else if (obj.Health < obj.MaxHealth && attacker is null)
            {
                obj.Health = Math.Min(obj.MaxHealth, obj.Health + 0.15);
            }

            if (obj.Team == 0 || obj.ShootCooldown-- > 0)
                continue;
            obj.ShootCooldown = 45;
            TankEntity? foe = null;
            var best = double.MaxValue;
            foreach (var tank in world.Tanks)
            {
                if (!tank.Alive || tank.Destroy.Active || Teams.Allied(obj.Team, tank.Team))
                    continue;
                var dx = tank.X - obj.X;
                var dy = tank.Y - obj.Y;
                var d = dx * dx + dy * dy;
                if (d >= best)
                    continue;
                best = d;
                foe = tank;
            }
            if (foe is null)
                continue;
            var angle = Math.Atan2(foe.Y - obj.Y, foe.X - obj.X);
            var speed = DiepView.Px(7);
            var shot = world.SpawnBulletFromMod(
                obj.X, obj.Y, Math.Cos(angle) * speed, Math.Sin(angle) * speed, angle,
                DiepView.Px(8), 14, 10, 55, foe.Id == 0 ? 0 : 0, ProjectileKind.Bullet,
                Teams.Color(obj.Team), true);
            if (shot is not null)
                shot.Team = obj.Team;
        }
    }

    private static void TickSurvival(GameWorld world, double dt)
    {
        _ = dt;
        var alive = 0;
        TankEntity? last = null;
        foreach (var tank in world.Tanks)
        {
            if (!tank.IsPet || !tank.Alive)
                continue;
            alive++;
            last = tank;
            if (world.Debug.Tick % 5 == 0)
                tank.Score += 1;
        }
        if (alive >= 2)
            world.SurvivalArmed = true;
        if (!world.SurvivalArmed || alive != 1 || last is null || world.ArenaClosing)
            return;
        world.SurvivalArmed = false;
        world.Notifications.Server($"{last.Class.Name} wins Survival", 8, "survival");
        world.CloseArena();
    }

    private static void BuildEdgeBases(GameWorld world, int[] teams)
    {
        var width = Math.Max(80, world.Width * 0.16);
        for (var i = 0; i < teams.Length; i++)
        {
            var right = i == 1;
            AddBase(world, teams[i], right ? world.Width - width * 0.5 : width * 0.5, world.Height * 0.5, width, world.Height);
        }
    }

    private static void BuildCornerBases(GameWorld world, int[] teams)
    {
        var size = Math.Max(90, Math.Min(world.Width, world.Height) * 0.22);
        (double X, double Y)[] corners =
        [
            (size * 0.5, size * 0.5),
            (world.Width - size * 0.5, world.Height - size * 0.5),
            (world.Width - size * 0.5, size * 0.5),
            (size * 0.5, world.Height - size * 0.5)
        ];
        for (var i = 0; i < teams.Length && i < corners.Length; i++)
            AddBase(world, teams[i], corners[i].X, corners[i].Y, size, size);
    }

    private static void AddBase(GameWorld world, int team, double x, double y, double w, double h)
    {
        world.AddObject(new ArenaObject
        {
            Kind = "base",
            Label = Teams.Name(team),
            Team = team,
            X = x,
            Y = y,
            W = w,
            H = h,
            ModeOwned = true,
            DamagePerTick = 0.7,
            HealPerTick = 0.45,
            Opacity = 0.14,
            Fill = Teams.Color(team)
        });
    }

    private static void BuildDominators(GameWorld world)
    {
        var r = Math.Max(36, DiepView.Px(46));
        (string Name, double X, double Y)[] spots =
        [
            ("SE", world.Width * 0.72, world.Height * 0.72),
            ("SW", world.Width * 0.28, world.Height * 0.72),
            ("NW", world.Width * 0.28, world.Height * 0.28),
            ("NE", world.Width * 0.72, world.Height * 0.28)
        ];
        foreach (var spot in spots)
        {
            world.AddObject(new ArenaObject
            {
                Kind = "dominator",
                Label = spot.Name + " Dominator",
                X = spot.X,
                Y = spot.Y,
                Radius = r,
                Sides = 6,
                ModeOwned = true,
                Health = 280,
                MaxHealth = 280,
                Opacity = 0.85,
                Fill = DiepColors.Neutral
            });
        }
    }

    private static void BuildMaze(GameWorld world)
    {
        var cell = Math.Max(150, DiepView.Px(170));
        var thick = Math.Max(18, DiepView.Px(20));
        var cols = Math.Clamp((int)(world.Width / cell), 4, 16);
        var rows = Math.Clamp((int)(world.Height / cell), 3, 12);
        var cw = world.Width / cols;
        var ch = world.Height / rows;
        var hWall = new bool[cols, rows];
        var vWall = new bool[cols, rows];
        for (var x = 0; x < cols; x++)
        for (var y = 0; y < rows; y++)
        {
            hWall[x, y] = y > 0;
            vWall[x, y] = x > 0;
        }

        var seen = new bool[cols, rows];
        var stack = new Stack<(int X, int Y)>();
        var rng = Random.Shared;
        stack.Push((rng.Next(cols), rng.Next(rows)));
        seen[stack.Peek().X, stack.Peek().Y] = true;
        while (stack.Count > 0)
        {
            var (x, y) = stack.Peek();
            var options = new List<(int X, int Y, bool Vertical)>();
            if (x > 0 && !seen[x - 1, y]) options.Add((x - 1, y, true));
            if (x + 1 < cols && !seen[x + 1, y]) options.Add((x + 1, y, true));
            if (y > 0 && !seen[x, y - 1]) options.Add((x, y - 1, false));
            if (y + 1 < rows && !seen[x, y + 1]) options.Add((x, y + 1, false));
            if (options.Count == 0)
            {
                stack.Pop();
                continue;
            }
            var pick = options[rng.Next(options.Count)];
            if (pick.Vertical)
                vWall[Math.Max(x, pick.X), y] = false;
            else
                hWall[x, Math.Max(y, pick.Y)] = false;
            seen[pick.X, pick.Y] = true;
            stack.Push((pick.X, pick.Y));
        }

        for (var x = 0; x < cols; x++)
        for (var y = 0; y < rows; y++)
        {
            if (hWall[x, y])
            {
                world.AddObject(new ArenaObject
                {
                    Kind = "wall",
                    X = x * cw + cw * 0.5,
                    Y = y * ch,
                    W = cw + thick,
                    H = thick,
                    Solid = true,
                    ModeOwned = true,
                    Opacity = 1,
                    Fill = Color.FromRgb(0x6B, 0x6B, 0x6B)
                });
            }
            if (vWall[x, y])
            {
                world.AddObject(new ArenaObject
                {
                    Kind = "wall",
                    X = x * cw,
                    Y = y * ch + ch * 0.5,
                    W = thick,
                    H = ch + thick,
                    Solid = true,
                    ModeOwned = true,
                    Opacity = 1,
                    Fill = Color.FromRgb(0x6B, 0x6B, 0x6B)
                });
            }
        }
    }

    private static void ReplaceMotherships(GameWorld world)
    {
        for (var i = world.Tanks.Count - 1; i >= 0; i--)
        {
            if (!world.Tanks[i].IsMothership)
                continue;
            world.ClearOwnedBullets(world.Tanks[i].Id);
            if (world.Selected == world.Tanks[i])
                world.Selected = world.Tanks.Find(t => t.IsPet && t.Alive);
            world.Tanks.RemoveAt(i);
        }
        if (world.Mode != GameMode.Mothership || world.Width < 40)
            return;
        SpawnMothership(world, Teams.Blue);
        SpawnMothership(world, Teams.Red);
    }

    private static void SpawnMothership(GameWorld world, int team)
    {
        var (x, y) = MothershipHome(world, team);
        var tank = new TankEntity
        {
            Id = world.NextTankId(),
            IsMothership = true,
            Team = team,
            Fill = Teams.Color(team),
            BossAltName = Teams.Name(team) + " Mothership",
            AiEnabled = true
        };
        TankClasses.Set(tank, TankId.Basic);
        tank.ClassPlan.Clear();
        ApplyMothershipBody(tank);
        tank.X = x;
        tank.Y = y;
        tank.Snap();
        world.Tanks.Add(tank);
    }

    public static void ApplyMothershipBody(TankEntity tank)
    {
        tank.MaxHealth = 8000;
        tank.Health = 8000;
        tank.Radius = DiepView.Units(120);
        tank.Mass = 80;
        tank.Absorption = 0.04;
        tank.Level = 45;
        tank.XpForNext = 1;
        tank.XpIntoLevel = 0;
    }

    private static void Unstick(GameWorld world)
    {
        foreach (var tank in world.Tanks)
        {
            if (!tank.Alive || !OverlapsSolid(world, tank.X, tank.Y, tank.Radius))
                continue;
            for (var i = 0; i < 30; i++)
            {
                var x = 40 + Random.Shared.NextDouble() * Math.Max(40, world.Width - 80);
                var y = 40 + Random.Shared.NextDouble() * Math.Max(40, world.Height - 80);
                if (OverlapsSolid(world, x, y, tank.Radius))
                    continue;
                tank.X = x;
                tank.Y = y;
                tank.Vx = 0;
                tank.Vy = 0;
                tank.Snap();
                break;
            }
        }
    }

    private static bool OverlapsSolid(GameWorld world, double x, double y, double radius)
    {
        foreach (var obj in world.Objects)
        {
            if (obj.Alive && obj.Solid && obj.Hits(x, y, radius))
                return true;
        }
        return false;
    }
}
