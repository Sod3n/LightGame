# LightGame Godot-Style Refactor — Progress Report

**Branch:** `refactor/godot-style-architecture` (5 commits ahead of `main`)

## What was done

### 1. Top-level rename
`Assets/Light and controller/` → `Assets/_Game/`
- Preserves all .meta GUIDs (no scene / prefab references broken)
- Root name matches Godot's convention of a short, unique features root

### 2. New Core framework (`Assets/_Game/Core/`)
Godot-mirror base classes at `_Game/Core/`:
- `Model.cs` — mirror of `class_name Model extends Resource` with property-changed signal
- `ViewModel.cs<TModel>` — mirror of `class_name ViewModel extends Resource`, wraps a Model and adds view-only state (e.g. optimistic predictions)
- `View.cs<TModel>` — MonoBehaviour view base with `SetData(model)` + property-changed subscription
- `Locker.cs` — counting + named lock, mirror of `features/core/locker.gd`; used to gate state updates while animations play

Existing interfaces moved into `Core/`:
- `IInitializable.cs`, `ITogglable.cs` (top-level Core)
- `IDamageable.cs`, `IHealable.cs`, `ILightable.cs`, `IWeight.cs` (Core/Interfaces/)

`Root.cs` moved from top-level Scripts to `Core/`. Namespace normalized to `LightGame.Core`.

### 3. New Globals autoloads (`Assets/_Game/Globals/`)
Mirrors Godot's `globals/*.gd`:
- **`EventBus.cs`** (moved from `Scripts/Systems/`) — existing type-safe pub/sub
- **`Game.cs`** (renamed from `GD.cs`) — Addressables-loaded game data
- **`SceneLoader.cs`, `SceneName.cs`** (moved) — scene navigation
- **`SaveManager.cs`** (renamed from `SaveSystem.cs`) — PlayerPrefs wrapper
- **`Screens.cs`** (new) — screen stack façade with `Changed` event
- **`Audio.cs`** (new) — thin wrapper over `SoundManager`
- **`Player.cs`** (new) — cached player GameObject accessor
- **`Settings.cs`** (new) — PlayerPrefs-backed audio/locale

### 4. Feature folders (`Assets/_Game/Features/<Feature>/`)
19 feature folders replacing the flat `Scripts/Components/` structure:
- **Player** (was `Controller/`) — MonoBehaviour, state machine, input, essentials
- **Platform** (was `Controller/Scripts/Platform Movement/`)
- **JumpPad** (was `Controller/Scripts/JumpPad*.cs`)
- **Abilities** (was `Scripts/GameData/AbilitiesSystem/` + `Scripts/Systems/AbilityManager.cs`)
- **Enemy** (was `Scripts/Components/Enemy/`)
- **Health** — HealthSystem, DamageTrigger, DamageOverTime, DangerLight, HealInLight, HealOverTime
- **Light** — LightDetector, Light2DGlobalListener, LightRayScaller
- **Teleport** — PlayerTeleportSystem, PlayerTeleportSkill, TeleportDestination
- **Checkpoint** — Checkpoint
- **Projectile** — Projectile, ProjectileSpawner
- **PushableObject** (was `Scripts/Components/PushableObject/`)
- **Weight** — Weight, WeightTrigger, PushStrength, ObjectsWeight
- **SceneTransition** — LevelChangeTrigger, SimpleTeleportTrigger, Trigger
- **Environment** — DistanceAlphaFade, DistanceAnimationControl, ParallaxEffect, SceneRoot, Mirror, MovedPlatform
- **VFX** — 10 files: DissolveObjectHider, ObjectHider, ModularWallDissolve, SimpleFadeHider, DestroyOnAnimationEnd, SpriteAlignToGround, SpriteBendToGround, SpritePhysicsSync, SimpleBlockToSpriteSync, SimpleEditModeSprite, EditModeSpriteRenderer
- **Audio** (was `Scripts/Audio/`)
- **UI** (was `Scripts/UI/`) + UICameraBinder
- **Selectable** (was `Scripts/Selectable/`)
- **Tween** (was `Scripts/Tween/`)

### 5. Namespace normalization
Bulk rewrite across 90 `.cs` files and 56 Unity YAML files (scenes / prefabs / assets):

| Old | New |
|---|---|
| `Light_and_controller.Scripts.Systems` | `LightGame.Globals` |
| `Light_and_controller.Scripts.AbilitiesSystem` | `LightGame.Features.Abilities` |
| `Light_and_controller.Scripts.Components.Enemy` | `LightGame.Features.Enemy` |
| `Light_and_controller.Scripts.Components.PushableObject` | `LightGame.Features.PushableObject` |
| `Light_and_controller.Scripts.Components` | `LightGame.Features` |
| `Light_and_controller.Scripts.Events` | `LightGame.Events` |
| `Light_and_controller.Scripts.UI.Views` | `LightGame.Features.UI.Views` |
| `Light_and_controller.Scripts.UI.Utils` | `LightGame.Features.UI.Utils` |
| `Light_and_controller.Scripts.UI` | `LightGame.Features.UI` |
| `Light_and_controller.Scripts.Editor` | `LightGame.Editor` |
| `Light_and_controller.Scripts` | `LightGame` |
| `LightGame.Audio` | `LightGame.Features.Audio` |

Post-rewrite normalization ensured:
- `Globals/*.cs` all live under `LightGame.Globals`
- `Core/**/*.cs` all live under `LightGame.Core`
- `using LightGame.Core;` / `using LightGame.Globals;` added where consumers needed access across sub-namespaces

Class renames: `GD` → `Game`, `SaveSystem` → `SaveManager`. All callers updated.

Unity YAML files (`.asset`, `.prefab`, `.unity`, etc.) had matching `m_EditorClassIdentifier` type strings updated so ScriptableObject asset references resolve to the new namespaces.

## Assemblies (unchanged)
- `LightGame.asmdef` moved from `_Game/Scripts/` to `_Game/` root — scope now covers everything except Features/Player subtree
- `PlayerController.asmdef` remains at `_Game/Features/Player/Scripts/` — separate assembly for player subsystem

## Vendored assets (untouched)
- `Assets/Plugins/` (DOTween, MVVM, ModularSettings, Neonalig)
- `Assets/Amazing Assets/` (AdvancedDissolve)
- `Assets/com.unity.uiextensions/`
- `Assets/AddressableAssetsData/`, `Assets/Resources/`, `Assets/Settings/`, `Assets/Packages/`
- `Assets/Tests/`, `Assets/SpriteOutline/`, `Assets/VOiD1 Gaming - 2D Hologram Shader/`, `Assets/_Recovery/`

## Stashed (not restored)
`stash@{0}` on `main`: `pre-refactor-stash-1783502451`. Contains:
- `Packages/manifest.json` + `Packages/packages-lock.json` changes
- `ProjectSettings/PackageManagerSettings.asset` change
- Untracked additions: `Assets/Editor/PortReferenceCapture/`, `Assets/Light and controller/Scripts/PortHarness/`, `Assets/Resources/Unity-MCP-ConnectionConfig.json`

To reintegrate:
```
git stash pop stash@{0}
# then move PortHarness files: they were at Scripts/PortHarness/ — target is now
# G:/GitHub/LightGame/Assets/_Game/Features/Player/Scripts/PortHarness/
```

## Next steps when you're back

1. **Open Unity Editor**. It will:
   - Regenerate `.csproj` and `.sln` files
   - Assign fresh `.meta` GUIDs to the ~13 newly created files (Model, ViewModel, View, Locker, Screens, Audio, Player, Settings, etc.)
   - Trigger a full script recompile — watch the console for any missed `using` statements or namespace mismatches
2. **Fix any compile errors** the console reports. Most likely candidates:
   - Files that used a type by unqualified name (implicit-same-namespace access) that no longer works because sub-namespaces split
   - `SoundData` etc. from `LightGame.Features.Audio` — some consumers may still expect the old `LightGame.Audio`
3. **Restore `stash@{0}`** if PortHarness / MCP config are wanted — see command above
4. **Optional next passes** (not done here):
   - Convert individual gameplay entities to `Model` + `ViewModel` + `View` triads (mirroring how `card.gd` / `card_vm.gd` / `card_view.gd` split responsibilities in card-game-nakama)
   - Wire `Screens` façade to actual UI window prefabs so nav uses `Screens.Push(key)` instead of raw `OpenWindowEvent`
   - Migrate `AbilityManager` namespace from `LightGame.Globals` to `LightGame.Features.Abilities` to match its folder
   - Split the top-level Editor/ from Features editor tools; consolidate ~7 Editor asmdefs from AdvancedDissolve

## Summary of commits
```
0e3ce2a Add missing usings for cross-namespace references
1190818 Rewrite namespaces and add Godot-style Globals wrappers
b15656b Restructure scripts into Godot-style feature folders
8600296 Rename Assets/Light and controller/ to Assets/_Game/
22f7498 Add refactor plan and Unity inventory
```

## Diff scale
1504 files changed, +2067 / -506 (majority: file renames + meta pairs)
