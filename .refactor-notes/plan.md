# LightGame Godot-Style Refactor Plan

## Source patterns (from user's Godot projects)

- `features/<name>/` folders — self-contained (scene + script + resource per feature)
- `globals/*.gd` autoloads — `EventBus`, `Screens`, `Player`, `Settings`, `Audio`, `SaveManager`
- `features/core/` — `Model`, `ViewModel`, `View`, `Locker`, `ViewBinder`
- MVC separation: Model = data (Resource with property-changed signals), Controller = logic (Node), View = rendering (Control)
- Direct signal connections, not a big event bus for game events

## Unity mapping

| Godot | Unity |
|---|---|
| `features/<name>/` | `Assets/_Game/Features/<Name>/` |
| `globals/*.gd` autoloads | `Assets/_Game/Globals/*.cs` (static classes) |
| `features/core/*.gd` | `Assets/_Game/Core/*.cs` (base classes) |
| Godot Resource (data) | `ScriptableObject` |
| Godot signals | C# `event` / existing `EventBus` |
| `class_name Foo extends Node` | `class Foo : MonoBehaviour` |
| `_ready()` | `Start()` |
| `_process(delta)` | `Update()` |

## Target Unity structure

```
Assets/_Game/                          # was: Light and controller/
├── LightGame.asmdef                   # was: Scripts/LightGame.asmdef
├── Core/                              # NEW - Godot features/core
│   ├── Model.cs
│   ├── ViewModel.cs
│   ├── View.cs
│   ├── Locker.cs
│   ├── SceneRoot.cs                   # was: Scripts/Root.cs
│   ├── IInitializable.cs
│   ├── ITogglable.cs
│   └── Interfaces/                    # IDamageable, IHealable, ILightable, IWeight
├── Globals/                           # NEW - Godot globals
│   ├── Game.cs                        # was: Scripts/GameData/GD.cs
│   ├── EventBus.cs                    # was: Scripts/Systems/EventBus.cs
│   ├── SceneLoader.cs                 # was: Scripts/SceneLoader.cs
│   ├── SceneName.cs                   # was: Scripts/SceneName.cs
│   ├── SaveManager.cs                 # was: Scripts/SaveSystem.cs (renamed)
│   ├── Screens.cs                     # NEW - screen stack facade
│   ├── Audio.cs                       # was: Scripts/Audio/SoundManager.cs
│   └── Player.cs                      # NEW - player facade
├── Events/                            # was: Scripts/Events/
├── Features/                          # NEW - Godot features
│   ├── Player/                        # was: Controller/Scripts/ + PlayerController.asmdef
│   │   ├── PlayerController.asmdef
│   │   ├── PlayerMain.cs
│   │   ├── PlayerData.cs
│   │   ├── PlayerView.cs
│   │   ├── Input/
│   │   ├── States/
│   │   ├── Essentials/
│   │   └── PortHarness/
│   ├── Platform/                      # was: Controller/Scripts/Platform Movement/
│   ├── JumpPad/                       # was: Controller/Scripts/JumpPad*.cs
│   ├── Abilities/                     # was: Scripts/GameData/AbilitiesSystem/ + AbilityManager
│   ├── Enemy/                         # was: Scripts/Components/Enemy/
│   ├── Health/                        # was: Scripts/Components/*Health*, *Damage*, *Heal*
│   ├── Light/                         # LightDetector, Light2DGlobalListener, LightRay*
│   ├── Teleport/                      # PlayerTeleport*
│   ├── Checkpoint/
│   ├── Projectile/
│   ├── PushableObject/                # was: Scripts/Components/PushableObject/
│   ├── SceneTransition/               # LevelChangeTrigger, SimpleTeleportTrigger
│   ├── Selectable/                    # was: Scripts/Selectable/
│   ├── Tween/                         # was: Scripts/Tween/
│   ├── UI/                            # was: Scripts/UI/
│   └── VFX/                           # DissolveObjectHider etc.
├── Scenes/                            # UNCHANGED (scene GUIDs preserved)
├── Prefabs/                           # UNCHANGED (prefab GUIDs preserved)
├── GameData/                          # UNCHANGED (SO data files)
├── Materials/, Shaders/, Sprites/, Sounds/, Fonts/, VFX/, Tilemap/, LightMask/, Localization/, PhysicsMaterials/, Samples/  # UNCHANGED
└── (all other content moves with folder rename)
```

## Namespace changes

- Old root: `Light_and_controller.Scripts.*`
- New root: `LightGame` (short, Godot-like via `class_name`)
- Sub-namespaces align with feature folders: `LightGame.Core`, `LightGame.Globals`, `LightGame.Events`, `LightGame.Features.Player`, etc.

## Execution order

1. Rename `Light and controller/` → `_Game/` (single atomic git mv)
2. Add `_Game/Core/` framework (Model, ViewModel, View, Locker, etc.)
3. Add `_Game/Globals/` autoload wrappers
4. Reorganize `_Game/Scripts/` and `_Game/Controller/Scripts/` into `_Game/Features/`
5. Update namespaces project-wide
6. Verify no orphaned .meta, no lost files, both asmdefs still valid

## Preserved constraints

- `.meta` GUIDs move with each script (via `git mv`)
- Prefabs / scenes stay in place (they reference scripts by GUID, unaffected by script folder moves)
- Vendored plugins (`Assets/Plugins/`, `Assets/Amazing Assets/`, `Assets/com.unity.uiextensions/`, `Assets/AddressableAssetsData/`, `Assets/Resources/`, `Assets/Settings/`) untouched
- Both existing asmdefs (`LightGame`, `PlayerController`) remain, just relocated to match new hierarchy
