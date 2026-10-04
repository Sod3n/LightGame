---
name: playtest
description: Playtest LightGame in the user's open Unity editor - open a level, enter Play Mode, move the player with simulated keys, pause/step frames, dump objects and component fields, take screenshots, read errors. Use to check a gameplay, animation, lighting or level-setup change in the real game.
---

# Playtest LightGame

`Assets/Editor/PlaytestDriver.cs` runs inside the open editor and executes commands
written to `Temp/playtest/cmd.txt`. Send them with `scripts/pt.sh`:

```bash
P=.claude/skills/playtest/scripts
$P/pt.sh "play FirstDesignedLevel"   # opens the level and enters Play Mode
cat Temp/playtest/status.txt         # heartbeat: playing / paused / compiling / scenes / player pos + state
```

## Prerequisites

- Unity 6000.2.7f2 open on the repo root.
- **The editor only imports file changes while its window is focused**, and it defers imports
  during Play Mode. After editing assets or scripts: `pt.sh stop`, then `scripts/focus-unity.sh`
  (brings Unity forward, waits for the refresh, restores focus, prints compile errors).
  Once the driver is loaded, `pt.sh refresh` also forces an import without focus.

## Commands

| Command | Does |
|---|---|
| `play [scene]` | enter Play Mode; with a scene name or path, open it first (fails if open scenes have unsaved changes) |
| `stop` | exit Play Mode and release all keys |
| `open <scene>` | open a scene in edit mode, e.g. `open LevelTemplate` |
| `pause [0]` / `step [n]` | pause (or `pause 0` to resume) / advance n frames while paused |
| `timescale <x>` | set `Time.timeScale`, e.g. `0.2` to slow animations down |
| `press <keys> [sec]` | hold keys for sec (default 0.1) on a virtual keyboard, e.g. `press d 1.5`, `press d+space 0.3` |
| `release` | release all held keys |
| `refresh` | `AssetDatabase.Refresh` (force-import changed files) |
| `find <text>` | active objects whose name contains text |
| `dump <path>` | subtree with world position, components, sprite/color, current animation clip, Light2D color |
| `inspect <path>[@Component]` | serialized fields of every component, or just one, e.g. `inspect Player@PlayerMain` |
| `select <path>` | select and ping the object in the editor |
| `setactive <path> 0\|1` | toggle an object |
| `shot <abs.png> [scale]` | Game view screenshot (Play Mode only, default 2x, written a frame later) |
| `logs` | errors/exceptions since the last call |
| `status` | same line as `status.txt`, on demand |
| `invoke <Namespace.Type.Method>` | call a static editor method (e.g. a one-off prefab/scene edit via Unity's API) and print its return value |

`<path>` is a full hierarchy path or its suffix. Duplicate names take `#n` for the n-th match in
hierarchy order: `Spikes/Spike#2`.

Player keys: `a`/`d` walk, `space` jump, `e` dash, `s` crouch, `q` wall grab, `w`/`s` wall climb,
`j` attack, `f` teleport. Key hold times are real seconds, so they stretch in game time under a
lowered `timescale`.

## Typical run

```bash
$P/pt.sh "play FirstDesignedLevel"
until grep -q "playing=True.*player=" Temp/playtest/status.txt; do sleep 1; done
$P/pt.sh "press d 1" ; sleep 1.2
$P/pt.sh "press space 0.3" "pause" "dump Player" "shot $PWD/Temp/jump.png" logs
$P/pt.sh "step 5" ; $P/pt.sh "dump Player"     # watch animation frames advance
$P/pt.sh stop
```

Level scenes load `Shared` additively on their own (`SceneRoot`), so playing any level directly
works. Look at every screenshot.

The repo also ships Unity-MCP skills (`gameobject-*`, `scene-*`, `screenshot-*`), which need
the MCP server connected; this driver needs only the open editor.
