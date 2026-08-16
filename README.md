# Desktop Diep

An always-on-top [diep.io](https://diep.io)-style desktop pet for Windows. Tanks roam your screen, farm shapes, fight bosses, and path around open windows.

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
dotnet run --project DesktopDiep.csproj
```

Or build a release binary:

```bash
dotnet build DesktopDiep.csproj -c Release
```

The exe lands in `bin/Release/net10.0-windows/`.

## Controls

| Action | How |
| --- | --- |
| Tray menu | Right-click the tray icon |
| Spawn tank | Left-click the tray icon |
| Debug overlay | `Ctrl+Shift+D` or tray |
| Pause | `Ctrl+Shift+P` or tray |
| Reset world | `Ctrl+Shift+R` or tray |

Drag tanks/shapes with the mouse when the cursor is over them. The overlay is click-through otherwise.

## Features

- **Pets** — AI tanks that aim, orbit, upgrade, and class up like diep.io
- **Shapes** — squares, triangles, pentagons, alpha pentagons, crashers
- **Bosses** — Guardian, Summoner, Defender, Fallen Booster, Fallen Overlord (tray spawn + random every 10–15 minutes)
- **Window collisions** — A* pathfinding around visible windows (maximized windows ignored)
- **Cursor collisions** — optional physics push from the mouse
- **Arena closers** — tray **Close Arena** ends the session diep-style
- **Settings** — tray toggles persist under `%LocalAppData%\DesktopDiep\settings.json`

## Tray menu

Spawn shapes/bosses, pick tank class and stats, toggle debug (A*, spatial hash), interpolation, selection halo, window/cursor collisions, close the arena, or quit.

## Notes

Physics and boss layouts take cues from [diepcustom](https://github.com/abcxff/diepcustom). This is a fan project and is not affiliated with diep.io.
