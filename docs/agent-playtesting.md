# Agent playtesting workflow

How an AI agent (Claude Code or a subagent) works on LightGame with a real Unity editor: making
asset or code changes, playing the game to check them, and committing the result, without
touching the editor a person has open.

## How it fits together

```
your checkout  LightGame/          ← your editor, your branch, your scenes
agent checkout LightGame-agent/    ← git worktree, agent's own editor, agent/<task> branch
                 │
                 ├─ Assets/Editor/PlaytestDriver.cs   runs inside the agent's editor and
                 │                                    executes commands from Temp/playtest/cmd.txt
                 └─ .claude/skills/playtest/
                      SKILL.md                        the command reference agents load
                      scripts/editor.sh               start / stop / restart the agent's editor
                      scripts/pt.sh                   send driver commands, print the reply
                      scripts/focus-unity.sh          bring the editor forward to force an import
```

Unity allows one editor per project folder, so the agent gets its own folder through a git
worktree. Because that editor is the agent's own, the agent can kill and relaunch it when it
hangs, and none of this touches your open scenes or unsaved work.

## One-time setup

Run these from your checkout (`LightGame/`). They assume Unity 6000.2.7f2 is installed through
Unity Hub; `editor.sh` finds it from the Hub install path.

```bash
# 1. Agent checkout on its own branch
git worktree add ../LightGame-agent -b agent/<task>

# 2. Reuse your import cache so the first launch takes a minute instead of a full import
rsync -a --exclude '*-lock' --exclude '*.lock' Library/ ../LightGame-agent/Library/

# 3. Remove the Unity-MCP plugin from the agent checkout only (it has hung domain reloads)
cd ../LightGame-agent
python3 - <<'EOF'
import json, collections
for f in ['Packages/manifest.json', 'Packages/packages-lock.json']:
    d = json.load(open(f), object_pairs_hook=collections.OrderedDict)
    for k in ['com.ivanmurzak.unity.mcp', 'com.ivanmurzak.unity.mcp.animation']:
        d['dependencies'].pop(k, None)
    open(f, 'w').write(json.dumps(d, indent=2, ensure_ascii=False) + '\n')
EOF
git update-index --skip-worktree Packages/manifest.json Packages/packages-lock.json
```

`--skip-worktree` keeps the package removal out of every commit made from the agent checkout.

## Working loop

All commands run inside `LightGame-agent/`, with `P=.claude/skills/playtest/scripts`.

**1. Start the editor.** It opens in the background without taking focus, and logs to
`Logs/agent-editor.log`.

```bash
$P/editor.sh start
until [ -f Temp/playtest/status.txt ]; do sleep 2; done   # driver is loaded
$P/editor.sh status                                       # pid + heartbeat line
```

**2. Make the change.** Text edits (scripts, `.anim`, `.meta`, `.prefab` YAML) can be made
directly. When Unity's API is the safer route, as with slicing a sprite sheet or rewriting
animation curves, write a throwaway static method in `Assets/Editor/`, run it through the driver,
then delete it:

```bash
$P/pt.sh refresh                                   # compile the throwaway script
$P/pt.sh "invoke AnimFixOneOff.SliceDashBw"        # returns the method's string result
rm Assets/Editor/AnimFixOneOff.cs*                 # never commit it
```

After any script or asset edit, run `pt.sh stop` (imports wait while in Play Mode) and then
`pt.sh refresh`. If the editor still shows `compiling=True` after a minute, use
`$P/focus-unity.sh`.

**3. Check it in play mode.**

```bash
$P/pt.sh "play FirstDesignedLevel"                 # levels load Shared on their own
until grep -q "playing=True.*player=" Temp/playtest/status.txt; do sleep 1; done
$P/pt.sh "timescale 0.1" "press a+e 1.5"           # slow motion, dash left
$P/pt.sh "dump Player"                             # sprite, current clip, position
$P/pt.sh pause "step 1" "shot $PWD/Temp/frame.png 1"
$P/pt.sh logs stop
```

Read numbers from `dump` and `status`, and look at every screenshot. See the skill's
`SKILL.md` for the full command list and the player key map.

**4. Commit only what you meant to change.**

```bash
git status --short                     # expect only your files
git add <paths> && git commit
```

**5. Stop the editor when done.**

```bash
$P/editor.sh stop
```

## Bringing the work back

The agent's branch is an ordinary branch in the same repository. From your checkout:

```bash
git log --oneline playtest-driver..agent/<task>   # review
git merge agent/<task>                            # or cherry-pick / open a PR
```

To remove the agent checkout entirely:

```bash
git worktree remove ../LightGame-agent            # add --force if it has untracked files
git branch -d agent/<task>
```

For the next task, either reuse the agent checkout on a fresh branch
(`git switch -c agent/<next> <base>`) or repeat the setup.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `pt.sh`: no reply in 60s | Editor is compiling, importing, or hung. Check `editor.sh status`; if `compiling=True` doesn't clear, run `focus-unity.sh`; if the log has stopped moving, run `editor.sh restart`. |
| Log ends at `Begin MonoManager ReloadAssembly` and the editor sits near 0% CPU | Domain reload deadlock (seen with the Unity-MCP plugin connected). `editor.sh restart`; make sure the plugin is removed in the agent checkout. |
| Game frames stop advancing while Unity is in the background | Run In Background is off in Player Settings. The driver turns it on for each play session and restores it afterwards; this only appears if the driver isn't loaded. |
| `press` has no effect | Not in Play Mode, or the game is paused (`pause 0`). Hold times are real seconds, so under a low `timescale` they cover less game time. |
| Many modified `.asset`/`.prefab` files you didn't touch | Unity reformatted them on import, usually after a long-stale `Library`. Leave them out of commits. |
| `logs` shows the same exception many times | Grouped as `(xN)`; startup noise from `ImageWithRoundedCorners` is pre-existing. |
