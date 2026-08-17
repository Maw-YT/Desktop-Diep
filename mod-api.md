# Desktop Diep Mod API

Lua mods live in `%LocalAppData%\DesktopDiep\mods\<modName>\` with `mod.json` + an entry script (usually `main.lua`). Toggle and reload them from the tray **Mods** menu. Logs go to `%LocalAppData%\DesktopDiep\mods\mods.log`.

Scripts run in MoonSharp’s **soft sandbox** (`math`, `table`, `string`, `bit32`, and `require` of files inside the mod folder). There is no `io` / `os` / network access; persist data with `Util.read_data` / `Util.write_data`.

An `example_chaos` mod is copied into the mods folder on first launch.

---

## Layout

```
%LocalAppData%\DesktopDiep\mods\
  my_mod\
    mod.json
    main.lua
    extra.lua          -- optional; require("extra")
    data\              -- created as needed for save files
```

### `mod.json`

```json
{
  "id": "my_mod",
  "name": "My Mod",
  "version": "1.0.0",
  "author": "you",
  "entry": "main.lua"
}
```

| Field | Required | Notes |
| --- | --- | --- |
| `id` | yes | Stable key used by the tray enable/disable list |
| `name` | no | Falls back to `id` |
| `version` | no | Display only |
| `author` | no | Display only |
| `entry` | no | Defaults to `main.lua` |

`require("extra")` resolves `extra.lua` or `extra/init.lua` under the mod folder.

---

## Globals

| Global | Role |
| --- | --- |
| `Mod` | This mod’s identity and paths |
| `World` | Arena, entities, spawn, pause, RNG |
| `Events` | Subscribe / unsubscribe / emit |
| `Catalog` | Tank, shape, and projectile definitions |
| `Notify` | On-screen messages |
| `Timers` | One-shot and repeating callbacks |
| `Util` | Math, JSON, colors, files |
| `Draw` | Per-frame overlay primitives |
| `Input` | Cursor and keyboard |

Colors in Draw/Notify accept **0–1 or 0–255** per channel. Angles are **radians**. Coordinates are overlay pixels (origin top-left of the virtual screen). The simulation ticks at **25 Hz** (`World.dt()` ≈ `0.04`).

`print(...)` is routed to the mods log as `[id] ...`.

---

## `Mod`

| Field | Type | Notes |
| --- | --- | --- |
| `id` | string | From `mod.json` |
| `name` | string | |
| `version` | string | |
| `author` | string | |
| `path` | string | Mod folder |
| `data` | string | `path/data` save directory |

---

## `World`

Most getters are functions. Setters that take an optional argument return the current value.

### Arena

| Call | Returns | Notes |
| --- | --- | --- |
| `width()` | number | Overlay width |
| `height()` | number | Overlay height |
| `tick()` | number | Integer simulation tick |
| `time()` | number | Seconds = `tick * dt` |
| `dt()` | number | Fixed step (`1/25`) |
| `fps()` | number | Smoothed render FPS |
| `paused([bool])` | bool | Get/set pause |
| `debug([bool])` | bool | Debug overlay |
| `interpolate([bool])` | bool | Motion interpolation |
| `halo([bool])` | bool | Selection ring |
| `arena_closing()` | bool | Arena closers are out |
| `boss_in([sec])` | number | Seconds until a random boss; set to reschedule |
| `collide_windows([bool])` | bool | Window obstacles + A* |
| `collide_cursor([bool])` | bool | Mouse physics |
| `render_style([name])` | string | `"Old"` / `"New"` / `"Shaded"` |
| `reset()` | | Wipe world and respawn a tank |
| `close_arena()` | | Start arena-closer ending (cancelable via `arena_close`) |

### Lists and queries

| Call | Returns |
| --- | --- |
| `selected()` | tank or `nil` |
| `tanks()` | all tanks (pets, bosses, closers) |
| `players()` | non-boss, non-closer tanks |
| `bosses()` | boss tanks |
| `closers()` | arena closers |
| `shapes()` | living shapes |
| `bullets()` | living bullets |
| `count([kind])` | `"tank"` / `"player"` / `"boss"` / `"shape"` / `"bullet"`; omit for all |
| `find_tank(id)` | tank or `nil` |
| `nearest_tank(x, y [, max [, exclude_id]])` | nearest living tank |
| `nearest_shape(x, y [, max])` | nearest living shape |
| `nearest_bullet(x, y [, max])` | nearest living bullet |
| `in_radius(x, y, r [, kind])` | mixed list; `kind` is `"tank"` / `"shape"` / `"bullet"` |

`select(index)` uses a **0-based list index** into `World.tanks()`. `select(tank)` and `select_id(id)` select by entity / tank id.

### Spawn

```lua
World.spawn_tank()                          -- random spot, Basic
World.spawn_tank(x, y)
World.spawn_tank(x, y, "Twin")
World.spawn_tank({ x = 400, y = 300, class = "Destroyer", level = 45 })

World.spawn_boss()                          -- random boss
World.spawn_boss("Guardian")

World.spawn_shape()                         -- random kind/spot
World.spawn_shape("Square", x, y)
World.spawn_shape({
  kind = "Pentagon", x = 200, y = 200,
  health = 400, radius = 40, xp = 200, mass = 12,
  r = 1, g = 0.2, b = 0.2,                  -- or fill = { r, g, b }
  notify = false                            -- skip debug flash
})

World.spawn_bullet({
  x = 0, y = 0, angle = 0, speed = 12,      -- or vx, vy
  radius = 8, damage = 10, health = 20, life = 45,
  kind = "Bullet", owner = tank,            -- or owner = id
  visible = true,
  r = 1, g = 0.85, b = 0.2                  -- or fill = { r, g, b }
})
```

`spawn_tank` / `spawn_boss` return `nil` if the arena is closing, the tank cap is hit, or a boss is already alive. Bullet cap is 200 for mod-spawned shots.

`kind` for bullets: `Bullet`, `Trap`, `Drone`, `Swarm`, `Necrodrone`, `Minion`, `Skimmer`, `Rocket`, `Flame`, `Croc`, `Wall`.

### Control and RNG

| Call | Notes |
| --- | --- |
| `select(index\|tank)` | 0-based index **or** tank userdata |
| `select_id(id)` | Tank id |
| `remove(entity)` | Tank, shape, or bullet |
| `clear_shapes()` | Drop every shape |
| `clear_bullets()` | Drop every bullet |
| `rng()` | `0..1` |
| `rng(a, b)` | Float in `[a, b)` |
| `rng_int(a, b)` | Inclusive integers |
| `chance(p)` | True with probability `p` (default `0.5`) |

### Screen / windows

```lua
local c = World.cursor()
-- c.x, c.y, c.vx, c.vy, c.down, c.hovering, c.grabbing

for _, box in ipairs(World.windows()) do
  -- box.left, top, right, bottom, x, y, w, h
end

World.can_see(x1, y1, x2, y2)   -- true if line of sight (or window collide off)
World.blocked(x1, y1, x2, y2)   -- true if a window blocks the segment
```

---

## Entities

Returned objects are userdata. Fields without `()` are properties (many are writable). Methods use colon syntax: `tank:heal()`.

### Tank

**Identity / flags**

| Field | Type | Notes |
| --- | --- | --- |
| `id` | int | Stable until the tank is removed |
| `kind` | string | Always `"tank"` |
| `class_id` | string | Catalog id, e.g. `"Twin"` |
| `class_name` | string | Display name |
| `alive` | bool | |
| `ai` | bool | `false` skips pet AI — drive with `steer` / `aim_at` / `wants_shot` |
| `is_boss` | bool | read-only |
| `is_closer` | bool | read-only |
| `is_ram` | bool | Smasher-style |
| `is_selected` | bool | |

**Motion / body**

`x`, `y`, `vx`, `vy`, `angle`, `radius`, `mass`, `health`, `max_health`, `fill_r`, `fill_g`, `fill_b`

**Progress**

`level` (set reclamps 1–45 and heals to full), `score`, `skill_points`, `xp_into`, `xp_for_next` (read-only)

**Combat / AI**

`wants_shot`, `aim_x`, `aim_y`, `fleeing`, `has_target`, `combat_timer`, `respawn`, `body_damage`, `move_speed`, `barrel_count`

| Method | Notes |
| --- | --- |
| `set_fill(r, g, b)` | |
| `get_stat(i)` / `set_stat(i, v)` | Stat index `0..7`, value `0..7` |
| `upgrade_stat(i)` | Spend one skill point; returns bool |
| `stats()` | Table with `1..8` and named keys (`regen`, `max_health`, `body`, `bullet_speed`, `pen`, `bullet_damage`, `reload`, `move`) |
| `barrels()` | List of barrel userdata |
| `enemy()` / `shape_target()` | Current AI targets or `nil` |
| `heal([amount])` | Omit / negative = full heal |
| `hurt(amount [, killer])` | Goes through `tank_hurt` |
| `kill([killer])` | |
| `set_class(id)` | Instant class change |
| `add_xp(n)` | Goes through `xp_gain` |
| `teleport(x, y)` | Snaps interpolation |
| `push(dx, dy)` | Add to velocity |
| `impulse(angle, mag)` | Kick along an angle |
| `steer(ax, ay)` | Diep-style accel; use with `ai = false` |
| `look_at(x, y)` | Face a point |
| `aim_at(x, y)` | Face + set AI aim |
| `shoot()` | Force-fire every barrel now |
| `select()` | Make this the selected tank |
| `remove()` | Returns false if it would delete the last pet |

#### Barrel

`index`, `angle`, `offset`, `size`, `width`, `reload`, `pos` (writable), `ready`, `projectile`

#### Stat indices

| Index | Name |
| --- | --- |
| 0 | Health Regen |
| 1 | Max Health |
| 2 | Body Damage |
| 3 | Bullet Speed |
| 4 | Bullet Penetration |
| 5 | Bullet Damage |
| 6 | Reload |
| 7 | Movement Speed |

### Shape

`kind` is always `"shape"`. `shape` is the kind string (`Square`, `Triangle`, `Pentagon`, `AlphaPentagon`, `Crasher`) and is writable.

`x`, `y`, `vx`, `vy`, `angle`, `radius`, `mass`, `health`, `max_health`, `ram_damage`, `spin`, `xp`, `destroying`, `fill_r/g/b`

| Method | |
| --- | --- |
| `set_fill(r, g, b)` | |
| `hurt(amount)` | Goes through `shape_hurt` |
| `kill([killer])` | Awards XP to killer |
| `teleport(x, y)` | Also resets idle orbit center |
| `push(dx, dy)` | |

### Bullet

`kind` is always `"bullet"`. `projectile` is the projectile type (writable).

`x`, `y`, `vx`, `vy`, `angle`, `radius`, `mass`, `damage`, `health`, `life`, `accel`, `spin`, `opacity`, `sides`, `age` (read-only), `visible`, `can_control`, `owner_id`

| Method | |
| --- | --- |
| `owner()` | Firing tank or `nil` |
| `set_owner(tank)` | `nil` clears |
| `set_fill(r, g, b)` | |
| `teleport(x, y)` | |
| `push(dx, dy)` | |
| `destroy()` | Sets health and life to 0 |

`visible = false` skips the stock renderer (use with `Draw` for custom VFX).

---

## `Events`

```lua
Events.on("tick", function(dt) end)
Events.off("tick")          -- this mod only, one event
Events.off()                -- this mod, all events
Events.emit("my_event", a, b)  -- every listening mod
```

Cancelable events receive a table `e` first. Cancel with `return false` **or** `e.cancel = true`.

### Lifecycle

| Event | Args | Notes |
| --- | --- | --- |
| `init` | | After all mods load (and after reload) |
| `unload` | | Before scripts are torn down |
| `tick` | `dt` | Start of each sim tick (paused world does not tick) |
| `post_tick` | `dt` | After physics / prune |
| `draw` | | Every render frame; queue `Draw.*` here. List is cleared each frame (max 2500 commands) |
| `world_reset` | | After `World.reset()` / tray reset |

### Spawns / deaths

| Event | Args |
| --- | --- |
| `tank_spawn` | tank |
| `boss_spawn` | tank |
| `shape_spawn` | shape |
| `bullet_spawn` | bullet |
| `tank_death` | tank |
| `shape_death` | shape |
| `tank_respawn` | tank |
| `kill` | killer tank (when a tank dies to another tank) |

### Combat (cancelable)

| Event | Args | Mutate |
| --- | --- | --- |
| `pre_shoot` | `e`, tank | `e.barrel`, `e.facing`; cancel to silence that barrel |
| `post_shoot` | tank, bullet | After a shot is created |
| `tank_hurt` | `e`, tank, killer-or-nil | `e.damage`; cancel / `0` skips the hit |
| `shape_hurt` | `e`, shape | `e.damage` |
| `collision` | `e`, a, b | `e.kind_a/b`, `e.index_a/b`; extra args are tank/shape/bullet or `nil` (cursor). Cancel skips knockback and damage |
| `xp_gain` | `e`, tank | `e.xp` |
| `think` | `e`, tank | Pet AI only (`ai ~= false`). Set `e.aim_x`, `e.aim_y`, `e.wants_shot`, `e.move_x`, `e.move_y` |
| `arena_close` | `e` | Cancel to block Close Arena |
| `boss_timer` | `e` | Cancel to skip the timed boss spawn (timer resets) |

`level_up` and `class_upgrade` each receive the tank.

---

## `Catalog`

| Call | Returns |
| --- | --- |
| `list()` | Playable class ids |
| `bosses()` | Boss ids |
| `shapes()` | Shape kind names |
| `projectiles()` | Projectile kind names |
| `stats()` | Stat display names, 1-based |
| `get(id)` | Def table or `nil` |
| `upgrades(id)` | Class ids this tank upgrades into |
| `register_tank(def)` | New id string, or `false` |
| `unregister(id)` | bool — mod-registered tanks only |

`get` fields: `id`, `name`, `level`, `sides`, `speed`, `is_boss`, `pre_addon`, `post_addon`, `upgrades`, `barrels` (each barrel: `angle`, `offset`, `size`, `width`, `reload`, `recoil`, `projectile`).

### `register_tank`

```lua
Catalog.register_tank({
  id = "laser_basic",
  name = "Laser Basic",
  level = 15,
  sides = 1,
  speed = 1,
  is_boss = false,
  boss_name = "The Laser",       -- bosses only
  pre_addon = "",
  post_addon = "",
  upgrades = { "Twin" },
  barrels = {
    {
      angle = 0, offset = 0, size = 95, width = 42,
      delay = 0, reload = 1, recoil = 1,
      trapezoid = false, trap_dir = 0, distance = 0,
      drones = 0, control_drones = false, force_fire = false,
      addon = nil,
      bullet = {
        type = "Bullet", size = 1, health = 1, damage = 1,
        speed = 1, scatter = 1, life = 1, absorption = 1,
        sides = 0, neutral = false
      }
    }
  }
})
```

Addon ids include `smasher`, `landmine`, `spike`, `autosmasher`, `autoturret`, `auto3`, `auto5`, …

Registrations are cleared on reload.

### Built-in playable ids

`Basic`, `Twin`, `Triplet`, `TripleShot`, `QuadTank`, `OctoTank`, `Sniper`, `MachineGun`, `FlankGuard`, `TriAngle`, `Destroyer`, `Overseer`, `Overlord`, `TwinFlank`, `PentaShot`, `Assassin`, `Necromancer`, `TripleTwin`, `Hunter`, `Gunner`, `Stalker`, `Ranger`, `Booster`, `Fighter`, `Hybrid`, `Manager`, `Predator`, `Sprayer`, `Trapper`, `GunnerTrapper`, `Overtrapper`, `MegaTrapper`, `TriTrapper`, `Smasher`, `Landmine`, `AutoGunner`, `Auto5`, `Auto3`, `SpreadShot`, `Streamliner`, `AutoTrapper`, `Battleship`, `Annihilator`, `AutoSmasher`, `Spike`, `Factory`, `Skimmer`, `Rocketeer`

### Boss ids

`Guardian`, `Summoner`, `Defender`, `FallenBooster`, `FallenOverlord`

---

## `Notify`

| Call | Default duration |
| --- | --- |
| `flash(text [, sec])` | 1.4 — debug strip |
| `server(text [, sec])` | 4.5 — grey banner |
| `arena(text [, sec])` | 6 — red banner |
| `mode(text [, sec])` | 4.5 — blue banner |
| `push(text, r, g, b [, sec [, id]])` | 4.5 — custom color; same `id` replaces the previous |
| `clear()` | Drop all banners |

---

## `Timers`

```lua
local id = Timers.after(2.5, function() end)   -- once
local id = Timers.every(1.0, function() end)   -- repeating, min interval 0.05s
Timers.cancel(id)
```

Timers pause with the world (they tick from the sim loop). Errors disable the mod.

---

## `Util`

| Call | Notes |
| --- | --- |
| `pi` / `tau` | Constants (`tau` = 2π) |
| `log(msg)` | Mods log |
| `dist(x1,y1,x2,y2)` | |
| `angle(x1,y1,x2,y2)` | `atan2` from 1 → 2 |
| `clamp(x, a, b)` | |
| `lerp(a, b, t)` | |
| `lerp_angle(a, b, t)` | Shortest-arc |
| `wrap_angle(a)` | To (−π, π] |
| `deg(rad)` / `rad(deg)` | |
| `now()` | Unix time, seconds (UTC, fractional) |
| `team_color(i)` | `{ r, g, b }` 0–1 |
| `color(name)` | Named diep color → `{ r, g, b }` |
| `json_encode(value)` | |
| `json_decode(str)` | `nil` on failure |
| `read_data(name)` | File under `Mod.data` (no paths) |
| `write_data(name, body)` | bool |
| `exists(name)` | Data file exists |
| `list_data()` | Filenames in the data folder |
| `read_file(rel)` | File under the mod folder (`extra.lua`, nested ok; `..` rejected) |

Color names: `tank`/`blue`, `square`/`yellow`, `triangle`/`red`, `pentagon`, `alpha`, `crasher`/`pink`, `fallen`/`grey`, `health`/`green`, `xp`/`gold`, `damage`, `necro`, `neutral`, `purple`.

---

## `Draw`

Only from `Events.on("draw", ...)`. Commands are discarded at the start of the next frame.

```lua
Draw.line(x1, y1, x2, y2, r, g, b, a [, width])          -- width default 3
Draw.circle(x, y, radius, r, g, b, a [, filled])         -- filled default true
Draw.glow(x, y, radius, r, g, b, a)
Draw.beam(x1, y1, x2, y2, width, r, g, b, a)
Draw.rect(x, y, w, h, r, g, b, a [, filled [, stroke]])
Draw.triangle(x1,y1, x2,y2, x3,y3, r, g, b, a [, filled])
Draw.poly(points, r, g, b, a [, filled [, stroke]])
Draw.ring(x, y, inner, outer, r, g, b, a)
Draw.text(x, y, "hello", size, r, g, b, a)
```

`poly` points may be `{ {x=, y=}, ... }`, `{ {x, y}, ... }`, or a flat `{ x1, y1, x2, y2, ... }`.

Custom “beam weapons” are Draw + an invisible bullet, not a built-in projectile type:

```lua
Draw.beam(x1, y1, x2, y2, 10, 0.2, 0.9, 1, 1)
World.spawn_bullet({
  x = x1, y = y1, angle = ang, speed = 0,
  radius = 18, damage = 40, life = 2, health = 999,
  owner = tank, visible = false
})
```

---

## `Input`

Polled; there are no key-down events. The overlay is click-through except when grabbing entities, but `GetAsyncKeyState` still sees real keys.

| Call | Notes |
| --- | --- |
| `cursor()` | Same table as `World.cursor()` |
| `mouse(btn)` | `"left"` / `"lmb"` / `"mouse1"`, `"right"` / `"rmb"`, `"middle"` / `"mmb"` |
| `key(name)` | See below; or a Windows virtual-key number |
| `down(name)` | Key **or** mouse. `"left"` / `"right"` mean **arrow keys** here — use `mouse("left")` for LMB |

Key names: `a`–`z`, `0`–`9`, `space`, `shift`, `ctrl`, `alt`, `tab`, `enter`, `escape`/`esc`, `backspace`, `left`, `up`, `right`, `down`.

---

## Patterns

### Aim the selected tank at the nearest shape

```lua
Events.on("tick", function()
  local tank = World.selected()
  if not tank then return end
  local s = World.nearest_shape(tank.x, tank.y, 400)
  if s then tank:aim_at(s.x, s.y) end
end)
```

### Drive a tank yourself

```lua
Events.on("tick", function()
  local tank = World.selected()
  if not tank then return end
  tank.ai = false
  local ax, ay = 0, 0
  if Input.key("w") or Input.key("up") then ay = ay - 1 end
  if Input.key("s") or Input.key("down") then ay = ay + 1 end
  if Input.key("a") or Input.key("left") then ax = ax - 1 end
  if Input.key("d") or Input.key("right") then ax = ax + 1 end
  tank:steer(ax, ay)
  local c = Input.cursor()
  tank:aim_at(c.x, c.y)
  tank.wants_shot = Input.mouse("left")
end)
```

### Save / load

```lua
local raw = Util.read_data("save.json")
local data = raw and Util.json_decode(raw) or { kills = 0 }

Events.on("kill", function()
  data.kills = data.kills + 1
  Util.write_data("save.json", Util.json_encode(data))
end)
```

### Cancelable damage

```lua
Events.on("tank_hurt", function(e, tank, killer)
  if tank.is_boss then
    e.damage = e.damage * 0.5
  end
end)
```

---

## Limits and pitfalls

- Max **12** pet tanks; bosses and closers do not count toward that cap.
- Max **~160** natural bullets, **200** for `spawn_bullet`.
- Draw list capped at **2500** commands per frame.
- A Lua error disables that mod until reload.
- `think` does not run when `tank.ai == false`.
- `World.select(n)` is a list index, not `tank.id` — use `select_id` / `tank:select()`.
- Reloading mods unregisters custom tanks and drops all timers/listeners.

After editing scripts: tray **Mods → Reload mods**.
