Perfect! Now let me create a comprehensive summary of my findings. I have gathered enough information to create a thorough inventory. Let me compile all this data into a detailed report.

---

# Unity Project Inventory: LightGame - Very Thorough

## Executive Summary

This is a **2D platformer game** built with **URP (Universal Render Pipeline)** in Unity. The architecture uses:
- **Event Bus** pattern for decoupled inter-system communication
- **State Machine** for player movement (complex with wall-climbing, dashing, teleport, etc.)
- **Manager-based design** for abilities, audio, health, and UI
- **Addressables** for asset management
- **MVVM pattern** (third-party plugin) for UI binding
- **DOTween** for animations and tweening

The project has a **strong separation of concerns** but is still relatively monolithic within the `Light and controller` folder.

---

## 1. Assets/ Folder Tree (3-4 Levels Deep)

```
Assets/
├── Light and controller/                    [USER-AUTHORED - Main game folder]
│   ├── Controller/                          [USER-AUTHORED - Player controller subsystem]
│   │   ├── Animations/
│   │   ├── Materials/
│   │   ├── Prefabs/
│   │   │   ├── Coyote Time Jump.prefab
│   │   │   ├── FreePlatform.prefab
│   │   │   ├── Player.prefab                [CORE PREFAB]
│   │   │   ├── Platform.prefab
│   │   │   ├── Swinging Platform.prefab
│   │   │   └── [test room variations...]
│   │   ├── Scenes/
│   │   │   └── TestScene_PC.unity
│   │   └── Scripts/                         [USER-AUTHORED - 33 files, 12 folders]
│   │       ├── Essentials/
│   │       │   ├── Custom Attributes/       [Custom property drawers]
│   │       │   ├── Extensions/              [Vector2, AnimationCurve extensions]
│   │       │   └── EssentialPhysics.cs
│   │       ├── Input System/
│   │       │   ├── InputActions.cs          [NewInputSystem wrapper]
│   │       │   └── InputManager/
│   │       │       └── PlayerInputManager.cs
│   │       ├── Platform Movement/
│   │       │   ├── Platform.cs
│   │       │   ├── SwingingPlatform.cs
│   │       │   └── Editor/
│   │       ├── State System/                [PLAYER STATE MACHINE - 13 states]
│   │       │   ├── Base States/
│   │       │   │   └── MainState.cs         [Base class for all states]
│   │       │   ├── Child States/            [PlayerIdleState, PlayerWalkState, PlayerJumpState, etc.]
│   │       │   │   ├── Interfaces/
│   │       │   │   └── [13 state implementations]
│   │       │   └── PlayerStateMachine.cs
│   │       ├── PlayerMain.cs                [CORE CONTROLLER - owns state machine]
│   │       ├── PlayerData.cs                [Data object for player stats/config]
│   │       ├── PlayerView.cs
│   │       ├── JumpPad.cs
│   │       └── JumpPadAdvanced.cs
│   │
│   ├── Scripts/                             [USER-AUTHORED - Game logic, 138+ files, 19 folders]
│   │   ├── Audio/                           [4 files - SoundManager, SoundData, interfaces]
│   │   ├── Components/                      [45 files - gameplay components]
│   │   │   ├── Enemy/                       [EnemyData, EnemyMovement, EnemyStateMachine, etc.]
│   │   │   │   └── States/                  [SlimeEnemy behavior states]
│   │   │   ├── PushableObject/              [ManualInputManager, PushableObject]
│   │   │   ├── Checkpoint.cs                [Respawn system]
│   │   │   ├── DamageInDarkSystem.cs        [Gameplay mechanic]
│   │   │   ├── DamageOverTimeEffect.cs
│   │   │   ├── DamageTrigger.cs
│   │   │   ├── HealthSystem.cs              [Health/damage framework]
│   │   │   ├── PlayerHealthSystem.cs
│   │   │   ├── LightDetector.cs             [Light-based mechanics]
│   │   │   ├── PlayerTeleportSystem.cs      [Ability system]
│   │   │   ├── Projectile.cs
│   │   │   ├── WeightTrigger.cs
│   │   │   └── [other interactive components...]
│   │   ├── Editor/                          [Editor tools: LevelCreatorWindow, EditorSpriteToggle]
│   │   ├── Events/                          [8 files - event data classes]
│   │   │   ├── GlobalLightEvents.cs         [Event bus for light control]
│   │   │   ├── OpenWindowEvent.cs
│   │   │   ├── CloseWindowEvent.cs
│   │   │   ├── PendingDamageEvent.cs
│   │   │   ├── PlayerDiedEvent.cs
│   │   │   ├── ShakeCameraEvent.cs
│   │   │   └── [RequestLevelChangeEvent, etc.]
│   │   ├── GameData/                        [Game configuration & abilities]
│   │   │   ├── AbilitiesSystem/             [Ability unlock system]
│   │   │   │   ├── LevelAbility.cs          [Base ability class (ScriptableObject)]
│   │   │   │   ├── LevelAbilities.cs        [Ability database]
│   │   │   │   ├── DamageInDarkAbility.cs
│   │   │   │   ├── DashAbility.cs
│   │   │   │   ├── DoubleJumpAbility.cs
│   │   │   │   ├── HealInLightAbility.cs
│   │   │   │   ├── TeleportAbility.cs
│   │   │   │   └── [other ability implementations]
│   │   │   ├── GD.cs                        [GLOBAL DATA SINGLETON - uses Addressables]
│   │   │   ├── LevelOrder.cs                [Level progression]
│   │   │   └── ObjectsWeight.cs
│   │   ├── Systems/                         [2 core system files]
│   │   │   ├── EventBus.cs                  [CORE EVENT SYSTEM - global + GameObject-scoped]
│   │   │   └── AbilityManager.cs            [MonoBehaviour manager for ability unlocking]
│   │   ├── Selectable/                      [UI selection/state system - 11 files]
│   │   │   ├── Button/
│   │   │   ├── Toggle/
│   │   │   ├── ISelectable.cs
│   │   │   ├── SelectableStateBinder.cs
│   │   │   ├── SelectableStateColor.cs
│   │   │   └── [other state binders: Scale, Sound, Sprite, etc.]
│   │   ├── Tween/                           [Tween animations - 15 files]
│   │   │   ├── TweenableBase.cs             [Base class]
│   │   │   ├── ComposableTween.cs
│   │   │   ├── Light2DIntensityTween.cs
│   │   │   ├── HealthSegmentTween.cs
│   │   │   ├── JumpPadBounceTween.cs
│   │   │   ├── Editor/
│   │   │   └── [other tween implementations]
│   │   ├── UI/                              [13 files - UI Views/Utils]
│   │   │   ├── Views/                       [UI screens/panels]
│   │   │   │   ├── MainMenuView.cs
│   │   │   │   ├── PlayerDeathView.cs
│   │   │   │   ├── HealthView.cs
│   │   │   │   ├── HealthSectorView.cs
│   │   │   │   ├── JumpPadView.cs
│   │   │   │   ├── CameraView.cs
│   │   │   │   └── [other views...]
│   │   │   └── Utils/
│   │   │       └── ParentedContentSizeFitter.cs
│   │   ├── Root.cs                          [BOOTSTRAP - Awake initializer]
│   │   ├── SceneLoader.cs                   [Scene management + additive loading]
│   │   ├── SceneName.cs                     [Enum of all scene names]
│   │   ├── SaveSystem.cs                    [PlayerPrefs-based save/load]
│   │   ├── IInitializable.cs                [Interface for bootstrap]
│   │   ├── ITogglable.cs
│   │   ├── Trigger.cs
│   │   ├── ObjectHider.cs
│   │   ├── DissolveObjectHider.cs           [Uses Advanced Dissolve shader]
│   │   ├── EnumExtensions.cs
│   │   ├── [other utility files...]
│   │
│   ├── Scenes/                              [LEVEL SCENES - 40+ Unity scenes]
│   │   ├── MainMenu.unity                   [BOOTSTRAP SCENE - Scene 0 in EditorBuildSettings]
│   │   ├── Shared.unity                     [SHARED PERSISTENT SCENE - Scene 2]
│   │   ├── Levels/                          [17 designed levels]
│   │   │   ├── FirstDesignedLevel.unity
│   │   │   ├── LevelTemplate.unity
│   │   │   ├── LevelCubeTriggerLight*.unity
│   │   │   ├── LevelGetCubeUpper.unity
│   │   │   └── [many more...]
│   │   └── TestLevels/                      [20+ test scenes]
│   │       ├── Level1.unity
│   │       ├── LevelTest.unity
│   │       └── [LevelTestHint, LevelTestEnemy, etc.]
│   │
│   ├── Prefabs/                             [GAME ENTITY PREFABS - ~120 files]
│   │   ├── Cameras.prefab                   [Camera rig with Cinemachine]
│   │   ├── GeneralLevel.prefab              [Level container template]
│   │   ├── Player.prefab                    [Player entity - handled in Controller/]
│   │   ├── Enemy-related/
│   │   │   ├── SlimeEnemy.prefab
│   │   │   ├── EnemyProjectile.prefab
│   │   │   └── ProjectileSpawner.prefab
│   │   ├── Interactive Objects/
│   │   │   ├── MovableCube.prefab
│   │   │   ├── HiddableMovableCube.prefab
│   │   │   ├── LightMovableCube.prefab
│   │   │   ├── Projectile.prefab
│   │   │   └── [weight, pushable variations]
│   │   ├── Environment/
│   │   │   ├── Ground.prefab
│   │   │   ├── Wall.prefab
│   │   │   ├── OneWayPlatform.prefab
│   │   │   ├── MovingPlatform.prefab
│   │   │   └── [Jumpad, Spike, etc.]
│   │   ├── Light entities/
│   │   │   ├── Point Light.prefab
│   │   │   ├── Spot Light.prefab
│   │   │   ├── StationaryLight.prefab
│   │   │   ├── MovingLight.prefab
│   │   │   ├── LevelEntryLight.prefab
│   │   │   ├── LevelChangeLight.prefab
│   │   │   └── LightRay.prefab
│   │   ├── UI/                              [UI Prefabs]
│   │   │   ├── Views/
│   │   │   │   ├── MainMenu.prefab
│   │   │   │   ├── PlayerUI.prefab
│   │   │   │   ├── HealthSector.prefab
│   │   │   │   └── Hint.prefab
│   │   │   └── Buttons/
│   │   │       └── Button.prefab
│   │   ├── TilemapPrefabs/                  [60+ tilemap brush prefabs]
│   │   │   ├── Checkpoint.prefab
│   │   │   ├── Cube.prefab
│   │   │   ├── Ground*.prefab
│   │   │   ├── BrushHint*.prefab            [Visual hint brushes for level design]
│   │   │   └── [many more...]
│   │   ├── Hints/
│   │   │   ├── Arrow.prefab
│   │   │   └── Dot.prefab
│   │   └── [Checkpoint, Hint, etc.]
│   │
│   ├── GameData/                            [ScriptableObject data files]
│   │   ├── Abilities/                       [Ability SO definitions]
│   │   └── [other data assets]
│   │
│   ├── Materials/                           [Custom materials]
│   ├── Shaders/                             [Custom shaders]
│   ├── Sprites/                             [2D sprite graphics]
│   │   ├── FirstLocation/
│   │   ├── HiddablePlatforms/
│   │   ├── VFX/                             [VFX sprite sheets]
│   │   └── UI/
│   ├── Tilemap/                             [Tilemap definitions]
│   ├── VFX/                                 [Particle effects & animations]
│   ├── Sounds/                              [Audio clips & music]
│   ├── Fonts/                               [Text fonts]
│   ├── Localization/                        [Localization data]
│   ├── PhysicsMaterials/
│   ├── LightMask/                           [Light masking system data]
│   ├── Controller/                          [See above]
│   ├── Samples/                             [Demo/example scenes & prefabs]
│   │   └── PlaygroundSamples/               [Tutorial levels and sample prefabs]
│   │
│   └── [Sprite/VFX prefabs - 12 files]
│
├── Plugins/                                 [THIRD-PARTY / VENDORED - DO NOT MOVE]
│   ├── Demigiant/                           [VENDORED]
│   │   ├── DOTween/                         [Animation tweening library]
│   │   ├── DOTweenPro/                      [Pro version features]
│   │   ├── DemiLib/                         [Demigiant utilities]
│   │   └── DOTweenPro Examples/             [Example scenes/scripts]
│   ├── MVVM/                                [VENDORED - Model-View-ViewModel pattern]
│   │   ├── Runtime/                         [MVVM.asmdef - 234 files]
│   │   ├── Editor/                          [Editor tools]
│   │   ├── Tests/                           [Unit tests]
│   │   └── Examples/                        [Scroller example]
│   ├── ModularSettings/                     [VENDORED - Settings UI framework]
│   │   ├── Scripts/                         [65 files - settings menu system]
│   │   ├── Prefabs/
│   │   │   ├── MenuTemplate.prefab
│   │   │   ├── DropdownTemplate.prefab
│   │   │   ├── SliderTemplate.prefab
│   │   │   └── [Toggle, ScrollablePanel]
│   │   └── ReadmeAssets/
│   └── Neonalig/                            [VENDORED]
│       └── SceneToolbar/                    [Scene toolbar plugin]
│
├── Amazing Assets/                          [VENDORED - Advanced Dissolve shader]
│   └── Advanced Dissolve/                   [Shader + 14 example scenes + editor tools]
│       ├── Editor/                          [Advanced Dissolve editor tools - 6 asmdef assemblies]
│       ├── Scripts/                         [Example scripts]
│       ├── Example Scenes/                  [Tutorial & demo scenes with prefabs]
│       └── [Shaders, materials, textures]
│
├── com.unity.uiextensions/                  [VENDORED - UI Extensions]
│   ├── Runtime/                             [UnityUIExtensions.asmdef]
│   ├── Editor/                              [UnityUIExtensions.Editor.asmdef]
│   └── Documentation/
│
├── Settings/                                [UNITY-GENERATED - Project settings]
├── Resources/                               [UNITY-GENERATED - DOTweenSettings, test data]
├── ProjectSettings/                         [EDITOR-CONFIGURED - Scenes, Tags, Layers]
├── Samples/                                 [UNITY-GENERATED - UIEffect samples]
├── SpriteOutline/                           [VENDORED - Sprite outline shader]
│   ├── Shaders/
│   └── Demo/
├── VOiD1 Gaming - 2D Hologram Shader/       [VENDORED - Shader pack]
│   ├── ShaderGraph/
│   ├── Material/
│   └── Sprites/
├── Tests/                                   [UNITY-GENERATED - Test assemblies]
│   └── TestShaders/
├── AddressableAssetsData/                   [UNITY-GENERATED - Addressables config]
│   ├── AssetGroups/
│   ├── DataBuilders/
│   └── Windows/
├── Packages/                                [NUGET/NPM packages - no user config]
│   ├── R3.1.3.0/                            [Reactive framework]
│   ├── ObservableCollections.*/
│   ├── System.*/
│   └── [other dotnet packages]
│
└── _Recovery/                               [BACKUP - Old scene backups]
    └── [4 numbered unity files]
```

---

## 2. User-Authored Scripts Inventory (by Domain)

### Total: 764 C# files
- **User-authored**: ~175 in `Light and controller/` (Controller + Scripts)
- **Third-party/vendored**: ~589 in Plugins/ and Amazing Assets/

### **Player Controller / Input (11 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Controller/Scripts/PlayerMain.cs` | `PlayerMain` | Root controller MonoBehaviour; owns state machine and coordinates components |
| `Controller/Scripts/PlayerData.cs` | `PlayerData` | Data container for player movement stats (speed, jump, acceleration, etc.) |
| `Controller/Scripts/PlayerView.cs` | `PlayerView` | Visual feedback (animation binding, visual effects) |
| `Controller/Scripts/Input System/InputManager/PlayerInputManager.cs` | `PlayerInputManager` | Wraps New Input System; provides input events to state machine |
| `Controller/Scripts/Input System/InputActions.cs` | `InputActions` | Generated class from Input Asset (or manual wrapper) |
| `Controller/Scripts/Essentials/EssentialPhysics.cs` | `EssentialPhysics` | Physics utility functions (raycasts, ground checks, etc.) |
| `Controller/Scripts/Essentials/Extensions/Vector2Extensions.cs` | `Vector2Extensions` | Vector math helpers |
| `Controller/Scripts/Essentials/Extensions/AnimationCurveExtensions.cs` | `AnimationCurveExtensions` | Animation curve utilities |

### **Player State Machine (14 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Controller/Scripts/State System/PlayerStateMachine.cs` | `PlayerStateMachine` | State machine driver; stores current/previous states |
| `Controller/Scripts/State System/Base States/MainState.cs` | `MainState` | Base class for all states; contains shared logic (ground checks, velocity) |
| `Controller/Scripts/State System/Child States/PlayerIdleState.cs` | `PlayerIdleState` | Idle/standing state |
| `Controller/Scripts/State System/Child States/PlayerWalkState.cs` | `PlayerWalkState` | Walking state with acceleration |
| `Controller/Scripts/State System/Child States/PlayerJumpState.cs` | `PlayerJumpState` | Jump initiation |
| `Controller/Scripts/State System/Child States/PlayerLandState.cs` | `PlayerLandState` | Landing recovery |
| `Controller/Scripts/State System/Child States/PlayerDashState.cs` | `PlayerDashState` | Dash/dodge ability |
| `Controller/Scripts/State System/Child States/PlayerCrouchIdleState.cs` | `PlayerCrouchIdleState` | Crouching idle |
| `Controller/Scripts/State System/Child States/PlayerCrouchWalkState.cs` | `PlayerCrouchWalkState` | Crouching walk |
| `Controller/Scripts/State System/Child States/PlayerWallGrabState.cs` | `PlayerWallGrabState` | Clinging to wall |
| `Controller/Scripts/State System/Child States/PlayerWallClimbState.cs` | `PlayerWallClimbState` | Climbing wall |
| `Controller/Scripts/State System/Child States/PlayerWallSlideState.cs` | `PlayerWallSlideState` | Sliding down wall |
| `Controller/Scripts/State System/Child States/PlayerWallJumpState.cs` | `PlayerWallJumpState` | Jumping off wall |
| `Controller/Scripts/State System/Child States/PlayerDirectionalJumpState.cs` | `PlayerDirectionalJumpState` | Directional jump (dash-like) |

### **Platform Movement (4 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Controller/Scripts/Platform Movement/Platform.cs` | `Platform` | Base platform component; detects player contact |
| `Controller/Scripts/Platform Movement/SwingingPlatform.cs` | `SwingingPlatform` | Swinging pendulum platform |
| `Controller/Scripts/JumpPad.cs` | `JumpPad` | Launch pad that boosts player upward |
| `Controller/Scripts/JumpPadAdvanced.cs` | `JumpPadAdvanced` | Advanced jump pad with configurable force |

### **Audio System (4 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Audio/SoundManager.cs` | `SoundManager` | Manager for playing sound effects and music |
| `Scripts/Audio/SoundData.cs` | `SoundData` | Audio clip container with metadata |
| `Scripts/Audio/ISoundPlayer.cs` | `ISoundPlayer` | Interface for audio playback |
| `Scripts/Audio/UnitySoundPlayer.cs` | `UnitySoundPlayer` | Unity implementation of audio player |

### **Health & Damage System (7 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Components/HealthSystem.cs` | `HealthSystem` | Base health component; tracks HP and damage |
| `Scripts/Components/PlayerHealthSystem.cs` | `PlayerHealthSystem` | Player-specific health behavior |
| `Scripts/Components/DamageTrigger.cs` | `DamageTrigger` | Trigger zone that damages player |
| `Scripts/Components/DamageOverTimeEffect.cs` | `DamageOverTimeEffect` | Damage-over-time effect processor |
| `Scripts/Components/DamageInDarkSystem.cs` | `DamageInDarkSystem` | Applies damage when player is in dark areas |
| `Scripts/Components/HealOverTimeEffect.cs` | `HealOverTimeEffect` | Healing-over-time effect |
| `Scripts/Components/HealInLightSystem.cs` | `HealInLightSystem` | Applies healing when in light areas |

### **Light Detection & Interaction (5 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Components/LightDetector.cs` | `LightDetector` | Detects whether object is in light (uses Light2D.isActiveAndEnabled) |
| `Scripts/Components/Light2DGlobalListener.cs` | `Light2DGlobalListener` | Subscribes to global light events |
| `Scripts/Tween/GlobalLight2DController.cs` | `GlobalLight2DController` | Tween-based light intensity control |
| `Scripts/Tween/Light2DIntensityTween.cs` | `Light2DIntensityTween` | Tweens light intensity |
| `Scripts/LightRayScaller.cs` | `LightRayScaller` | Scales light ray visual effects |

### **Teleport System (3 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Components/PlayerTeleportSystem.cs` | `PlayerTeleportSystem` | Teleport mechanic implementation |
| `Scripts/Components/PlayerTeleportSkill.cs` | `PlayerTeleportSkill` | Teleport ability definition |
| `Scripts/Components/TeleportDestination.cs` | `TeleportDestination` | Teleport target marker |

### **Enemy System (9 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Components/Enemy/EnemyData.cs` | `EnemyData` | Enemy stats/config data |
| `Scripts/Components/Enemy/EnemyStateMachine.cs` | `EnemyStateMachine` | State machine for enemy behavior |
| `Scripts/Components/Enemy/EnemyState.cs` | `EnemyState` | Base enemy state |
| `Scripts/Components/Enemy/EnemyMovement.cs` | `EnemyMovement` | Legacy enemy movement |
| `Scripts/Components/Enemy/EnemyMovementNew.cs` | `EnemyMovementNew` | New enemy movement system |
| `Scripts/Components/Enemy/EnemyInputManager.cs` | `EnemyInputManager` | Enemy input simulation |
| `Scripts/Components/Enemy/SlimeEnemy.cs` | `SlimeEnemy` | Slime enemy implementation |
| `Scripts/Components/Enemy/EnemyProjectile.cs` | `EnemyProjectile` | Projectile fired by enemies |
| `Scripts/Components/Enemy/States/Slime*.cs` (3 files) | `SlimeAggroState`, `SlimeAttackState`, `SlimeWanderState` | Slime AI states |

### **Pushable Objects (2 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Components/PushableObject/PushableObject.cs` | `PushableObject` | Cube that can be pushed by player |
| `Scripts/Components/PushableObject/ManualInputManager.cs` | `ManualInputManager` | Manual input for pushable object |

### **Checkpoint & Respawn (1 file)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Components/Checkpoint.cs` | `Checkpoint` | Respawn point; tracks active checkpoint via static property |

### **Interactive Components (15+ files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Components/Projectile.cs` | `Projectile` | Projectile entity with damage |
| `Scripts/Components/ProjectileSpawner.cs` | `ProjectileSpawner` | Spawns projectiles on trigger |
| `Scripts/Components/WeightTrigger.cs` | `WeightTrigger` | Trigger activated by weighted objects |
| `Scripts/Components/Weight.cs` | `Weight` | Weight component for objects |
| `Scripts/Components/IWeight.cs` | `IWeight` | Interface for weighted objects |
| `Scripts/Components/LevelChangeTrigger.cs` | `LevelChangeTrigger` | Triggers scene transition |
| `Scripts/Components/SimpleTeleportTrigger.cs` | `SimpleTeleportTrigger` | Quick teleport trigger |
| `Scripts/Components/Trigger.cs` | `Trigger` | Base trigger component |
| `Scripts/Components/DistanceAlphaFade.cs` | `DistanceAlphaFade` | Fade sprite by distance |
| `Scripts/Components/DistanceAnimationControl.cs` | `DistanceAnimationControl` | Control animation speed by distance |
| `Scripts/Components/ParallaxEffect.cs` | `ParallaxEffect` | Parallax scrolling effect |
| `Scripts/Components/SceneRoot.cs` | `SceneRoot` | Scene root initializer |
| `Scripts/Components/UICameraBinder.cs` | `UICameraBinder` | Binds UI to camera |
| `Scripts/Trigger.cs` | `Trigger` | Trigger base class |
| `Scripts/ObjectHider.cs` | `ObjectHider` | Hide/show objects |

### **Event System (9 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Systems/EventBus.cs` | `EventBus` | **CORE EVENT SYSTEM** - Static global event bus with pub/sub; supports global and GameObject-scoped subscriptions |
| `Scripts/Events/GlobalLightEvents.cs` | `GlobalLightEvents`, `SetLightIntensityEvent`, etc. | Light control events (5 event classes) |
| `Scripts/Events/PendingDamageEvent.cs` | `PendingDamageEvent` | Damage event |
| `Scripts/Events/PendingHealEvent.cs` | `PendingHealEvent` | Heal event |
| `Scripts/Events/PlayerDiedEvent.cs` | `PlayerDiedEvent` | Death event |
| `Scripts/Events/RequestLevelChangeEvent.cs` | `RequestLevelChangeEvent` | Level transition event |
| `Scripts/Events/ShakeCameraEvent.cs` | `ShakeCameraEvent` | Camera shake event |
| `Scripts/Events/OpenWindowEvent.cs` | `OpenWindowEvent` | UI window open |
| `Scripts/Events/CloseWindowEvent.cs` | `CloseWindowEvent` | UI window close |

### **Ability System (9 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Systems/AbilityManager.cs` | `AbilityManager` | **Manages ability unlocking per level**; tracks active, permanent, disabled abilities; integrates with `GD` |
| `Scripts/GameData/AbilitiesSystem/LevelAbility.cs` | `LevelAbility` | Base class for abilities (ScriptableObject) |
| `Scripts/GameData/AbilitiesSystem/LevelAbilities.cs` | `LevelAbilities` | Database of all abilities; loads from Addressables |
| `Scripts/GameData/AbilitiesSystem/DashAbility.cs` | `DashAbility` | Dash ability |
| `Scripts/GameData/AbilitiesSystem/DoubleJumpAbility.cs` | `DoubleJumpAbility` | Extra jump ability |
| `Scripts/GameData/AbilitiesSystem/DamageInDarkAbility.cs` | `DamageInDarkAbility` | Take damage in dark ability |
| `Scripts/GameData/AbilitiesSystem/HealInLightAbility.cs` | `HealInLightAbility` | Heal in light ability |
| `Scripts/GameData/AbilitiesSystem/TeleportAbility.cs` | `TeleportAbility` | Teleport ability |
| `Scripts/GameData/AbilitiesSystem/PushStrengthAbility.cs` | `PushStrengthAbility` | Push strength boost |

### **Game Data & Globals (4 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/GameData/GD.cs` | `GD` | **GLOBAL STATIC SINGLETON**; loads LevelOrder, LevelAbilities, ObjectsWeight from Addressables on first `Init()` call |
| `Scripts/GameData/LevelOrder.cs` | `LevelOrder` | Defines level progression (ScriptableObject) |
| `Scripts/GameData/ObjectsWeight.cs` | `ObjectsWeight` | Weight values for interactive objects |
| `Scripts/GameData/AbilitiesSystem/LevelAbilities.cs` | (see above) | Ability database |

### **Scene Management (4 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Root.cs` | `Root` | **BOOTSTRAP INITIALIZER**; calls `IInitializable.Initialize()` on all children via ExecuteEvents hierarchy |
| `Scripts/SceneLoader.cs` | `SceneLoader` | Static scene manager; handles additive loading, maintains active level state |
| `Scripts/SceneName.cs` | `SceneName` | Enum of all scene names with extension methods |
| `Scripts/SaveSystem.cs` | `SaveSystem` | PlayerPrefs-based save/load (minimal) |

### **UI System (13 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/UI/Views/MainMenuView.cs` | `MainMenuView` | Main menu screen |
| `Scripts/UI/Views/PlayerDeathView.cs` | `PlayerDeathView` | Death screen |
| `Scripts/UI/Views/HealthView.cs` | `HealthView` | Health bar display |
| `Scripts/UI/Views/HealthSectorView.cs` | `HealthSectorView` | Segmented health display |
| `Scripts/UI/Views/CameraView.cs` | `CameraView` | Camera control UI |
| `Scripts/UI/Views/JumpPadView.cs` | `JumpPadView` | Jump pad indicator |
| `Scripts/UI/Views/WeightTriggerView.cs` | `WeightTriggerView` | Weight trigger feedback |
| `Scripts/UI/Views/LevelChangeView.cs` | `LevelChangeView` | Level transition screen |
| `Scripts/UI/Views/LightProjectorView.cs` | `LightProjectorView` | Light projector UI |
| `Scripts/UI/Views/ProjectileSpawnerView.cs` | `ProjectileSpawnerView` | Spawner feedback |
| `Scripts/UI/Views/SlimeView.cs` | `SlimeView` | Enemy UI |
| `Scripts/UI/Views/WindowView.cs` | `WindowView` | Generic window base |
| `Scripts/UI/Utils/ParentedContentSizeFitter.cs` | `ParentedContentSizeFitter` | Layout utility |

### **Selectable / UI Controls (11 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Selectable/ISelectable.cs` | `ISelectable` | Interface for selectable UI elements |
| `Scripts/Selectable/SelectableStateBinder.cs` | `SelectableStateBinder` | Base for state binding |
| `Scripts/Selectable/SelectableStateColor.cs` | `SelectableStateColor` | Color state visual |
| `Scripts/Selectable/SelectableStateScale.cs` | `SelectableStateScale` | Scale state visual |
| `Scripts/Selectable/SelectableStateSound.cs` | `SelectableStateSound` | Sound state feedback |
| `Scripts/Selectable/SelectableStateSprite.cs` | `SelectableStateSprite` | Sprite state visual |
| `Scripts/Selectable/SelectableStateTextMaterial.cs` | `SelectableStateTextMaterial` | Text material state |
| `Scripts/Selectable/SelectableStateSeparateUnityEvent.cs` | `SelectableStateSeparateUnityEvent` | Unity event binding |
| `Scripts/Selectable/SelectableStateUnityEvent.cs` | `SelectableStateUnityEvent` | Single event binding |
| `Scripts/Selectable/Button/ButtonExtended.cs` | `ButtonExtended` | Extended button behavior |
| `Scripts/Selectable/Toggle/ToggleExtended.cs` | `ToggleExtended` | Extended toggle behavior |

### **Tween / Animation System (15 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Tween/TweenableBase.cs` | `TweenableBase` | Base for tween-driven animations |
| `Scripts/Tween/ComposableTween.cs` | `ComposableTween` | Chain multiple tweens |
| `Scripts/Tween/Light2DIntensityTween.cs` | `Light2DIntensityTween` | Tween light intensity |
| `Scripts/Tween/HealthSegmentTween.cs` | `HealthSegmentTween` | Animate health bar segments |
| `Scripts/Tween/HealthSectorStateTween.cs` | `HealthSectorStateTween` | Health UI animation |
| `Scripts/Tween/JumpPadBounceTween.cs` | `JumpPadBounceTween` | Jump pad bounce animation |
| `Scripts/Tween/JumpPadCompressTween.cs` | `JumpPadCompressTween` | Jump pad compression animation |
| `Scripts/Tween/JumpPadReadyTween.cs` | `JumpPadReadyTween` | Jump pad idle animation |
| `Scripts/Tween/HintPopupScaleTween.cs` | `HintPopupScaleTween` | Hint scale animation |
| `Scripts/Tween/HintPopupSlideTween.cs` | `HintPopupSlideTween` | Hint slide animation |
| `Scripts/Tween/HintPopupCombinedTween.cs` | `HintPopupCombinedTween` | Combined hint animation |
| `Scripts/Tween/PulseTween.cs` | `PulseTween` | Pulse animation utility |
| `Scripts/Tween/ScaleTween.cs` | `ScaleTween` | Scale animation |
| `Scripts/Tween/SizeDeltaTween.cs` | `SizeDeltaTween` | Rect size animation |
| `Scripts/Tween/Editor/*.cs` | Editor tools | Animation conversion utilities |

### **Visual Effects (8 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/DissolveObjectHider.cs` | `DissolveObjectHider` | Uses Advanced Dissolve shader to hide objects |
| `Scripts/ModularWallDissolve.cs` | `ModularWallDissolve` | Dissolve effect for walls |
| `Scripts/SimpleFadeHider.cs` | `SimpleFadeHider` | Simple fade-based hiding |
| `Scripts/ObjectHider.cs` | `ObjectHider` | Generic object hider |
| `Scripts/DestroyOnAnimationEnd.cs` | `DestroyOnAnimationEnd` | Destroy when animation finishes |
| `Scripts/SpritePhysicsSync.cs` | `SpritePhysicsSync` | Keep sprite in sync with physics |
| `Scripts/SpriteAlignToGround.cs` | `SpriteAlignToGround` | Align sprite to ground collider |
| `Scripts/SpriteBendToGround.cs` | `SpriteBendToGround` | Bend sprite along ground |

### **Editor Tools (4 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/Editor/LevelCreatorWindow.cs` | `LevelCreatorWindow` | EditorWindow for level building |
| `Scripts/Editor/EditorSpriteToggle.cs` | `EditorSpriteToggle` | Toggle sprite visibility in editor |
| `Scripts/Tween/Editor/AnimationToTweenConverter.cs` | Editor tool | Convert Animation to Tween |
| `Scripts/Tween/Editor/CustomTweenAnimationEditor.cs` | Editor tool | Tween animation inspector |

### **Utilities & Interfaces (12 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Scripts/IInitializable.cs` | `IInitializable` | Bootstrap initialization interface (extends IEventSystemHandler) |
| `Scripts/ITogglable.cs` | `ITogglable` | Enable/disable interface |
| `Scripts/IDamageable.cs` | `IDamageable` | Damage interface |
| `Scripts/IHealable.cs` | `IHealable` | Healing interface |
| `Scripts/ILightable.cs` | `ILightable` | Light detection interface |
| `Scripts/EnumExtensions.cs` | `EnumExtensions` | Enum utilities |
| `Scripts/Components/MonoBehaviourEffect.cs` | `MonoBehaviourEffect` | Base effect component |
| `Scripts/Components/MonoBehaviourWithData.cs` | `MonoBehaviourWithData` | Generic data-bound component |
| `Scripts/EditModeSpriteRenderer.cs` | `EditModeSpriteRenderer` | Editor-mode sprite rendering |
| `Scripts/SimpleEditModeSprite.cs` | `SimpleEditModeSprite` | Simple editor sprite |
| `Scripts/SimpleBlockToSpriteSync.cs` | `SimpleBlockToSpriteSync` | Sync collider to sprite |
| Root-level: `Mirror.cs`, `MovedPlatform.cs` | Various | Miscellaneous gameplay objects |

### **Custom Attributes (4 files)**

| File | Class | Purpose |
|------|-------|---------|
| `Controller/Scripts/Essentials/Custom Attributes/BoundedCurveAttribute.cs` | `BoundedCurveAttribute` | Inspector constraint for curves |
| `Controller/Scripts/Essentials/Custom Attributes/CustomRangeAttribute.cs` | `CustomRangeAttribute` | Inspector min/max range |
| `Controller/Scripts/Essentials/Custom Attributes/InformAttribute.cs` | `InformAttribute` | Inspector info label |
| `Controller/Scripts/Essentials/Custom Attributes/NonEditableAttribute.cs` | `NonEditableAttribute` | Read-only in inspector |

---

## 3. Scenes Inventory

### Bootstrap Scene
- **Scene 0 (Build Index 0)**: `Assets/Light and controller/Scenes/MainMenu.unity` - Opens first when game launches

### Persistent/Shared Scenes
- **Scene 2 (Build Index 2)**: `Assets/Light and controller/Scenes/Shared.unity` - Loaded additively; contains managers, UI canvas, systems that persist across level transitions

### Level Scenes (17 designed levels + 20+ test scenes)

**Designed Levels** (in `Scenes/Levels/`):
1. FirstDesignedLevel.unity
2. LevelTemplate.unity
3. LevelCubeTriggerLight*.unity (10 variations)
4. LevelGetCubeUpper.unity
5. LevelJumpadAndCubeSimple.unity
6. LevelTeachPullCube*.unity (3 variations)
7. LevelTeleportPlatformerSimple.unity
8. LevelWeightPlatformSimple.unity

**Test Scenes** (in `Scenes/TestLevels/`):
- Level1.unity, LevelTest.unity, LevelTestEnemy.unity, LevelTestJumpad.unity, LevelTestMovableCube.unity, etc. (20+ total)

### Boot Sequence
1. `MainMenu.unity` loads
2. Player selects level → `SceneLoader.LoadLevel(SceneName)` loads Shared + target level additively
3. `Root.cs` on scene root calls `ExecuteEvents.ExecuteHierarchy<IInitializable>()` to bootstrap all components

---

## 4. Prefabs Inventory

**Total: ~130 prefabs**

### Gameplay Entities (40 prefabs)

**Player & Core**:
- `Controller/Prefabs/Player.prefab` - Player character (PlayerMain + state machine)
- `Controller/Prefabs/Platform.prefab` - Basic platform
- `Controller/Prefabs/Swinging Platform.prefab` - Swinging platform
- `Controller/Prefabs/Swinging and Moving Platform.prefab` - Combined motion
- `Controller/Prefabs/FreePlatform.prefab`, `Coyote Time Jump.prefab`

**Enemies**:
- `Prefabs/SlimeEnemy.prefab` - Slime AI enemy

**Interactives**:
- `Prefabs/MovableCube.prefab` - Pushable cube
- `Prefabs/HiddableMovableCube.prefab` - Cube that can hide
- `Prefabs/LightMovableCube.prefab` - Cube interacting with light
- `Prefabs/Cube.prefab` - Static cube
- `Prefabs/Projectile.prefab` - Projectile entity
- `Prefabs/EnemyProjectile.prefab` - Enemy projectile
- `Prefabs/ProjectileSpawner.prefab` - Spawner component

**Environment**:
- `Prefabs/Ground.prefab` - Ground tile
- `Prefabs/Wall.prefab` - Wall tile
- `Prefabs/OneWayPlatform.prefab` - One-way platform
- `Prefabs/MovingPlatform.prefab` - Moving platform
- `Prefabs/Jumpad.prefab` - Jump pad
- `Prefabs/Spike.prefab` - Spike hazard
- `Prefabs/HidePlatforms.prefab`, `SkinedHidePlatform.prefab` - Hide/show platforms

**Special**:
- `Prefabs/Checkpoint.prefab` - Respawn point
- `Prefabs/ConeTrigger.prefab` - Cone trigger area
- `Prefabs/WeightPlaform.prefab` - Weight-sensitive platform
- `Prefabs/GeneralLevel.prefab` - Level container template

### Light Entities (6 prefabs)
- `Prefabs/Point Light.prefab`
- `Prefabs/Spot Light.prefab`
- `Prefabs/StationaryLight.prefab`
- `Prefabs/MovingLight.prefab`
- `Prefabs/LevelEntryLight.prefab` - Light at level start
- `Prefabs/LevelChangeLight.prefab` - Light for transitions
- `Prefabs/LightRay.prefab` - Light beam visual

### UI Prefabs (5 prefabs)
- `Prefabs/UI/Views/MainMenu.prefab` - Main menu screen
- `Prefabs/UI/Views/PlayerUI.prefab` - HUD during gameplay
- `Prefabs/UI/Views/HealthSector.prefab` - Health display
- `Prefabs/UI/Views/Hint.prefab` - Tutorial hint popup
- `Prefabs/UI/Buttons/Button.prefab` - Standard button

### Framework Prefabs (1 prefab)
- `Prefabs/Cameras.prefab` - Cinemachine camera rig with follow

### Tilemap Prefabs (60+ brush prefabs)
Used as "brushes" for tilemap painting in editor:
- `TilemapPrefabs/BrushHint*.prefab` (15+ hint variants for level design visualization)
- `TilemapPrefabs/Ground*.prefab` (3 sizes × 2 angles = 6 variants)
- `TilemapPrefabs/Wall*.prefab` (6 variants)
- `TilemapPrefabs/{Checkpoint,Cube,Jumpad,MovingLight,etc}.prefab` (40+ object brushes)

### Sample Prefabs (5 prefabs)
- `Samples/PlaygroundSamples/Prefabs/Platformer/{Adventurer,OrangeCharacter}.prefab`
- `Samples/PlaygroundSamples/Prefabs/RPGPlatformer/RPGAdventurer.prefab`
- VFX sprite prefabs (12 variations: mm1-mm11)

### Tilemap Setup
- `Tilemap/Tilemap.prefab` - Tilemap GameObject container

---

## 5. Assemblies (`.asmdef`) Inventory

### User-Authored Assemblies

| Assembly | Namespace | Location | References | Purpose |
|----------|-----------|----------|-----------|---------|
| **LightGame** | (root) | `Light and controller/Scripts/LightGame.asmdef` | PlayerController, TextMeshPro, DOTweenPro, UniTask, UltEvents, MVVM, Localization, R3, URP 2D, InputSystem, Cinemachine, Addressables, Advanced Dissolve | **MAIN GAME ASSEMBLY** - All game logic |
| **PlayerController** | (root) | `Light and controller/Controller/Scripts/PlayerController.asmdef` | (GUID reference only - likely internal) | **PLAYER SUBSYSTEM** - Controller, states, input, movement |
| **Tests** | (root) | `Tests/Tests.asmdef` | (references unknown) | Test framework assembly |

### Third-Party Assemblies (DO NOT MOVE)

| Assembly | Location | Purpose |
|----------|----------|---------|
| **DOTween.Modules** | `Plugins/Demigiant/DOTween/` | Animation tweening library |
| **DOTweenPro.Scripts** | `Plugins/Demigiant/DOTweenPro/` | Pro tweening features |
| **DOTweenPro.EditorScripts** | `Plugins/Demigiant/DOTweenPro/Editor/` | Editor tools |
| **MVVM** | `Plugins/MVVM/Runtime/` | Model-View-ViewModel framework |
| **MVVM.Editor** | `Plugins/MVVM/Editor/` | MVVM editor tools |
| **MVVM.Tests** | `Plugins/MVVM/Tests/` | MVVM unit tests |
| **UnityUIExtensions** | `com.unity.uiextensions/Runtime/` | UI extension components |
| **UnityUIExtensions.Editor** | `com.unity.uiextensions/Editor/` | UI extensions editor |
| **Plugins.Neonalig.SceneToolbar** | `Plugins/Neonalig/SceneToolbar/` | Scene toolbar utility |
| **AmazingAssets.AdvancedDissolve** | `Amazing Assets/Advanced Dissolve/Scripts/` | Dissolve shader library |
| **AmazingAssets.AdvancedDissolve.Editor** | `Amazing Assets/Advanced Dissolve/Editor/` | 6 separate editor asmdef files |
| **AmazingAssets.AdvancedDissolve.Examples** | `Amazing Assets/Advanced Dissolve/Example Scenes/` | Example scripts |

---

## 6. Packages (User-Relevant)

**Manifest.json** at `Packages/manifest.json` - Key packages:

| Package | Version | Type | Purpose |
|---------|---------|------|---------|
| **com.cysharp.unitask** | (git) | Async/await | Async task management |
| **com.cysharp.r3** | (git) | Reactive | Reactive programming framework |
| **com.coffee.ui-effect** | (git) | VFX | UI visual effects |
| **com.svermeulen.extenject** | (git) | DI Framework | Dependency injection (Zenject) |
| **com.unity.addressables** | 2.7.4 | Asset System | Runtime asset loading and management |
| **com.unity.cinemachine** | 3.1.4 | Camera | Advanced camera system |
| **com.unity.inputsystem** | 1.14.2 | Input | New Input System |
| **com.unity.localization** | 1.5.8 | Localization | Multi-language support |
| **com.unity.render-pipelines.universal** | 17.2.0 | Graphics | URP pipeline |
| **com.unity.timeline** | 1.8.9 | Animation | Timeline editor |
| **com.unity.visualscripting** | 1.9.7 | Visual Scripting | Node-based scripting |
| _(All com.unity.modules.*)_ | 1.0.0 | Built-in | Standard Unity modules |

**NuGet Packages** (in Assets/Packages/):
- R3.1.3.0 (Reactive framework)
- ObservableCollections.3.3.4
- System.ComponentModel.Annotations.5.0.0
- System.Threading.Channels.8.0.0

---

## 7. Entry Points & Bootstrap

### Bootstrap Hierarchy

```
1. MainMenu.unity loads (Scene 0)
   ↓
2. Root.cs (MonoBehaviour on scene root)
   - Awake() calls ExecuteEvents.ExecuteHierarchy<IInitializable>()
   - Broadcasts Initialize() message to all children implementing IInitializable
   ↓
3. Components implementing IInitializable initialize themselves
```

### Manager Registration (in Shared.unity or level scenes)

Components that auto-initialize via `IInitializable`:
- **AbilityManager.cs** - Start() calls `GD.Init()` to load ability database from Addressables
- **SoundManager.cs** - Initializes audio systems
- Any UI Views with IInitializable

### Global Singletons

| Class | Type | Location | Purpose |
|-------|------|----------|---------|
| **EventBus** | Static class | `Scripts/Systems/EventBus.cs` | Global pub/sub event system; no MonoBehaviour needed |
| **GD** | Static class | `Scripts/GameData/GD.cs` | Global data singleton; loads LevelOrder, LevelAbilities, ObjectsWeight from Addressables on `Init()` |
| **SceneLoader** | Static class | `Scripts/SceneLoader.cs` | Scene management; tracks current level |
| **SaveSystem** | Static class | `Scripts/SaveSystem.cs` | PlayerPrefs wrapper |
| **Checkpoint** | Static property on Checkpoint class | `Scripts/Components/Checkpoint.cs` | `Checkpoint.Active` tracks current respawn point |

### No `RuntimeInitializeOnLoadMethod` found
- No static initialization on domain reload
- All bootstrap done through scene-based Root.cs

### Execution Order Notes
- No explicit `[DefaultExecutionOrder]` attributes found
- Relies on natural Unity execution order: Awake → OnEnable → Start
- EventBus.Publish() is thread-safe and can be called from any Update/coroutine

---

## 8. Existing Patterns to Preserve

### 1. **Event Bus Pattern (CORE)**
```csharp
// Global event publishing/subscription
EventBus.Publish(new PlayerDiedEvent());
EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);

// GameObject-scoped events
EventBus.Subscribe(healthSystem.gameObject, (event DamagePendingEvent) => {...});
```
- **Location**: `Scripts/Systems/EventBus.cs`
- **Status**: Fully implemented, generic, type-safe
- **Critical**: Used throughout for decoupled communication

### 2. **State Machine Pattern**
```csharp
// Player movement states: Idle → Walk → Jump → Land
public class PlayerStateMachine { }
public class MainState { } // Base with shared logic
public class PlayerIdleState : MainState { }
// 13+ child states total
```
- **Location**: `Controller/Scripts/State System/`
- **Status**: Well-established, handles complex platformer movement
- **Scale**: 14 classes for player + enemy also has state machine

### 3. **Ability/Feature Unlock System**
```csharp
// ScriptableObject-based abilities unlocked per level
public class LevelAbility : ScriptableObject { 
    public UnlockBehavior unlockBehavior; // AlwaysUnlocked, UnlockAtLevel, TemporaryUnlock
    public virtual void Activate(GameObject player) { }
    public virtual void Deactivate(GameObject player) { }
}

// Manager handles activation/deactivation
public class AbilityManager : MonoBehaviour {
    public void UnlockAbilitiesForLevel(SceneName level) { }
}
```
- **Location**: `Scripts/GameData/AbilitiesSystem/` + `Scripts/Systems/AbilityManager.cs`
- **Status**: Extensible, supports 3 unlock behaviors
- **Scale**: 9 ability classes defined

### 4. **Component-Based Interfaces**
```csharp
// Damage system uses interfaces
public interface IDamageable { void TakeDamage(float amount); }
public interface IHealable { void Heal(float amount); }
public interface ILightable { bool IsInLight { get; } }
public interface IWeight { float Weight { get; } }
```
- **Location**: `Scripts/Components/`
- **Status**: Clean, composition-based
- **Critical**: Enables flexible gameplay interactions

### 5. **MVVM Usage (Third-Party)**
- Uses `com.svermeulen.extenject` (Zenject) for DI
- Uses custom UI binders (SelectableStateBinder, ButtonStateBinder, etc.)
- **Note**: Zenject is heavy; consider if refactoring to Godot

### 6. **Tween-Based Animation**
```csharp
// DOTween for animation sequences
public class TweenableBase : MonoBehaviour {
    public virtual void Play() { /* DOTween sequence */ }
}

// Compositions of tweens
public class ComposableTween : MonoBehaviour {
    public List<TweenableBase> tweens;
}
```
- **Location**: `Scripts/Tween/`
- **Status**: 15 tween classes, well-organized
- **Dependency**: DOTween library (vendored in Plugins/)

### 7. **Scene Management (Additive Loading)**
```csharp
// MainMenu.unity → LoadLevel(Level1) → unloads previous + loads Shared + Level1
public static void LoadLevel(SceneName newLevel) {
    if (_currentLevelScene.HasValue) UnloadScene(_currentLevelScene.Value);
    SceneManager.LoadSceneAsync(newLevel.KeyToString(), LoadSceneMode.Additive);
}
```
- **Location**: `Scripts/SceneLoader.cs`
- **Status**: Maintains persistent Shared scene + level-specific scenes
- **Critical**: Multi-scene architecture

### 8. **Addressables-Based Asset Loading**
```csharp
// Global data loaded from Addressables on demand
public static void Init() {
    LevelOrder = Addressables.LoadAssetAsync<LevelOrder>("LevelOrder").WaitForCompletion();
    LevelAbilities = Addressables.LoadAssetAsync<LevelAbilities>("LevelAbilities").WaitForCompletion();
}
```
- **Location**: `Scripts/GameData/GD.cs`
- **Status**: Centralized asset loading via named references
- **Note**: Uses blocking `WaitForCompletion()` - could be async in refactor

### 9. **Physics-Based Player Movement**
- Rigidbody2D + CapsuleCollider2D for collision detection
- Custom physics in MainState for slope handling, ground checks
- Velocity-based movement (not transform.Translate)

### 10. **Enemy AI (State Machine)**
```csharp
public class EnemyStateMachine { }
public class SlimeEnemy : MonoBehaviour { }
public class SlimeAggroState { } // Detect, chase
public class SlimeAttackState { } // Attack player
public class SlimeWanderState { } // Idle patrol
```
- **Location**: `Scripts/Components/Enemy/`
- **Status**: Similar to player state machine; 3 states implemented

---

## 9. Migration Map (User-Authored Content)

### Safety Assessment for Refactor

| Path | User-Authored? | Safe to Move? | Notes |
|------|---|---|---|
| `Light and controller/Controller/` | YES | YES | No external .meta GUID references; assembly boundary clean via `PlayerController.asmdef` |
| `Light and controller/Scripts/` | YES | YES | Main assembly `LightGame.asmdef` references all subfolders; move entire tree together |
| `Light and controller/Scenes/` | YES | YES | Scene paths are handled by SceneLoader enum; rename in SceneName.cs and all levels |
| `Light and controller/Prefabs/` | YES | CAREFUL | Prefabs reference scripts by GUID; safe if moving scripts + prefabs together in same refactor pass |
| `Light and controller/GameData/` | YES | YES | ScriptableObject assets; load via Addressables so path doesn't matter |
| `Light and controller/Materials/` | MIXED | YES | Custom materials safe; some may depend on Advanced Dissolve shaders (vendored) |
| `Light and controller/Shaders/` | YES | YES | Custom shader files |
| `Light and controller/Sprites/` | ASSET | YES | Non-code assets; safe to move |
| `Light and controller/Sounds/` | ASSET | YES | Audio files; safe to move |
| `Light and controller/Fonts/` | ASSET | YES | Font files |
| `Light and controller/Localization/` | ASSET | YES | Localization data |
| `Light and controller/VFX/` | ASSET | YES | Particle/VFX files |
| `Light and controller/Tilemap/` | MIXED | YES | Tilemap definitions; safe if brush prefabs moved together |
| `Plugins/Demigiant/DOTween/` | NO | **DO NOT MOVE** | Third-party library; DOTweenSettings.asset references this path; assembly references embedded |
| `Plugins/Demigiant/DOTweenPro/` | NO | **DO NOT MOVE** | Third-party; assembly references hardcoded |
| `Plugins/MVVM/` | NO | **DO NOT MOVE** | Third-party framework; multiple asmdef files with GUID references |
| `Plugins/ModularSettings/` | NO | **DO NOT MOVE** | Third-party settings UI; asmdef + script references |
| `Plugins/Neonalig/` | NO | **DO NOT MOVE** | Third-party toolbar; asmdef embedded |
| `Amazing Assets/Advanced Dissolve/` | NO | **DO NOT MOVE** | Third-party shader pack; 6 asmdef files + shaders; material references |
| `com.unity.uiextensions/` | NO | **DO NOT MOVE** | Third-party UI; asmdef references |
| `com.unity.uiextensions/` | NO | **DO NOT MOVE** | Third-party UI; asmdef references |
| `AddressableAssetsData/` | PARTIAL | CAREFUL | Addressables database; if you move assets, update address names in this folder |
| `Resources/` | MIXED | CAREFUL | DOTweenSettings.asset stored here; also test data files |
| `Settings/` | UNITY-GEN | NO | Project settings; don't move |
| `ProjectSettings/` | UNITY-GEN | NO | Editor settings; don't move |

### Critical .meta GUID Preservation Points

1. **Script References in Scenes/Prefabs**:
   - When moving `.cs` files, their `.meta` files must move with them
   - Scene/prefab YAML references scripts by `guid: XXXXXXXX` in the `.meta`
   - As long as you move `.cs` + `.meta` together, GUIDs stay valid

2. **Assembly References**:
   - `LightGame.asmdef` references `PlayerController` by name (not GUID)
   - `PlayerController.asmdef` references another assembly by GUID
   - Safe to move both asmdef files + their directories together

3. **Addressables References**:
   - GD.cs uses string names: `"LevelOrder"`, `"LevelAbilities"`, `"ObjectsWeight"`
   - Must update AddressableAssetsData/ to match new paths
   - Or load those assets differently in refactored code

4. **Vendors/Plugins**:
   - Never move Plugins/, Amazing Assets/, com.unity.uiextensions/, com.unity packages
   - Their assembly references are hardcoded with GUIDs
   - Moving them breaks every script that imports from them

### Recommended Refactor Strategy

1. **Phase 1 - Preserve Vendors**:
   - Keep all of `Plugins/`, `Amazing Assets/`, `com.unity.uiextensions/` in place
   - These can be symlinked or moved once at the end if needed

2. **Phase 2 - Restructure User Code**:
   - Move entire `Light and controller/` subtree as unit
   - Rename top-level folder to something shorter (e.g., `Game/`)
   - **Preserve internal structure** (Controller/, Scripts/, Scenes/, Prefabs/, etc.)
   - Update `SceneLoader.cs` → `SceneName.cs` enum to match new paths

3. **Phase 3 - Update Assembly References**:
   - Edit `Light and controller/Scripts/LightGame.asmdef`
   - If moving PlayerController folder, update reference path
   - Re-import project to validate

4. **Phase 4 - Address Addressables**:
   - Update `AddressableAssetsData/` group paths or rebuild
   - Update GD.cs Addressables.LoadAssetAsync() calls with new paths
   - Or refactor to use Resources/ or direct SO references

5. **Phase 5 - Scene Updates**:
   - Update EditorBuildSettings.asset with new scene paths
   - Validate all scene references are resolvable

---

## 10. Code Examples: Manager/Bootstrap Classes

### Root.cs (Bootstrap Entry Point)
```csharp
public class Root : MonoBehaviour
{
    private void Awake()
    {
        // Broadcast Initialize() to all IInitializable children
        ExecuteEvents.ExecuteHierarchy<IInitializable>(
            gameObject, 
            null, 
            (x, _) => x.Initialize()
        );
    }
}
```
**Key insight**: Uses ExecuteEvents pattern (rarely seen) for elegant, decoupled initialization.

---

### EventBus.cs (Global Event System)
```csharp
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _eventSubscribers 
        = new Dictionary<Type, List<Delegate>>();
    private static readonly Dictionary<GameObject, Dictionary<Type, List<Delegate>>> 
        _gameObjectEventSubscribers = new();

    // Global subscription
    public static void Subscribe<T>(Action<T> handler) where T : class { ... }
    
    // GameObject-scoped subscription
    public static void Subscribe<T>(GameObject target, Action<T> handler) where T : class { ... }
    
    // Publish with error handling
    public static void Publish<T>(T eventData) where T : class { ... }
}
```
**Key insight**: Dual-level event system (global + per-GameObject); robust exception handling.

---

### GD.cs (Global Data Singleton)
```csharp
public static class GD
{
    public static LevelOrder LevelOrder;
    public static LevelAbilities LevelAbilities;
    public static ObjectsWeight ObjectsWeight;
    private static bool _isInitialized;
    
    public static void Init()
    {
        if (_isInitialized) return;
        
        // Load from Addressables (blocking)
        LevelOrder = Addressables.LoadAssetAsync<LevelOrder>("LevelOrder")
            .WaitForCompletion();
        LevelAbilities = Addressables.LoadAssetAsync<LevelAbilities>("LevelAbilities")
            .WaitForCompletion();
        ObjectsWeight = Addressables.LoadAssetAsync<ObjectsWeight>("ObjectsWeight")
            .WaitForCompletion();
        
        _isInitialized = true;
    }
}
```
**Key insight**: Lazy initialization pattern; uses Addressables for centralized data loading.

---

### AbilityManager.cs (MonoBehaviour Manager)
```csharp
public class AbilityManager : MonoBehaviour
{
    private HashSet<LevelAbility> _activeAbilities = new();
    private HashSet<LevelAbility> _permanentAbilities = new();
    
    private void Start()
    {
        GD.Init();
        _player = GameObject.FindGameObjectWithTag("Player");
        
        // Unlock abilities from day 1
        ActivateAlwaysUnlockedAbilities();
        
        // Unlock abilities up to current level
        var currentLevel = SceneLoader.GetCurrentLevel();
        if (currentLevel.HasValue)
        {
            UnlockAbilitiesUpToLevel(currentLevel.Value);
        }
    }
    
    public void UnlockAbility(LevelAbility ability)
    {
        if (!_activeAbilities.Contains(ability))
        {
            _activeAbilities.Add(ability);
            ability.Activate(_player);
            
            // Track permanent vs temporary
            if (ability.unlockBehavior == UnlockBehavior.UnlockAtLevel 
                || ability.unlockBehavior == UnlockBehavior.AlwaysUnlocked)
            {
                _permanentAbilities.Add(ability);
            }
        }
    }
}
```
**Key insight**: Integrates with both global GD and local state; supports persistent + temporary ability unlocks.

---

### PlayerMain.cs (Player Controller Core)
```csharp
public class PlayerMain : MonoBehaviour
{
    public PlayerStateMachine _stateMachine;
    public MainState IdleState, WalkState, JumpState, ... ; // 13 states
    
    [NonSerialized] public Animator Animator;
    [NonSerialized] public Rigidbody2D Rigidbody2D;
    [NonSerialized] public PlayerInputManager InputManager;
    [NonSerialized] public CapsuleCollider2D CapsuleCollider2D;
    
    public PlayerData PlayerData; // Config data
    public float PushObjectSlowdown = 1f; // Can be modified by PushableObject
    public bool IsPushingObject = false;
    
    private void Awake()
    {
        // Get components
        Animator = GetComponent<Animator>();
        Rigidbody2D = GetComponent<Rigidbody2D>();
        InputManager = GetComponent<PlayerInputManager>();
        CapsuleCollider2D = GetComponent<CapsuleCollider2D>();
        
        // Initialize state machine
        _stateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, _stateMachine, AnimName.Idle, PlayerData);
        WalkState = new PlayerWalkState(this, _stateMachine, AnimName.Walk, PlayerData);
        // ... 11 more states
    }
}
```
**Key insight**: Owns state machine; states reference back to PlayerMain for shared data (Rigidbody2D, Input, Animator).

---

## Summary Statistics

| Category | Count |
|----------|-------|
| **Total .cs files in project** | 764 |
| **User-authored .cs files** | ~175 |
| **Third-party/vendored .cs files** | ~589 |
| **Assemblies (asmdef files)** | 18 (2 user, 16 third-party) |
| **Unity Scenes** | 60+ (40+ test/level, 1 bootstrap, 1 persistent) |
| **Prefabs** | ~130 (40 gameplay, 6 light, 5 UI, 1 camera, 60+ tilemap brushes, 12+ VFX) |
| **Components** | 45 (in Components/ folder alone) |
| **Event Types** | 8 custom event classes |
| **Abilities** | 9 (DashAbility, DoubleJump, Teleport, DamageInDark, HealInLight, HealthUpgrade, PushStrength) |
| **Player States** | 13 |
| **Enemy States** | 3 (SlimeEnemy) |
| **Installed Packages** | 40+ (mix of Unity, Cysharp, community plugins) |

---

## Godot Refactor Readiness

### Strengths to Preserve
1. ✅ Clean event bus → easily map to Godot signals/events
2. ✅ State machine architecture → direct translation to Godot State nodes
3. ✅ Ability system → convert ScriptableObjects to Godot Resource/GDScript classes
4. ✅ Modular components → Godot composition-based scenes
5. ✅ No RuntimeInitializeOnLoadMethod → simple scene bootstrapping
6. ✅ Physics-based movement → Godot 2D physics API

### Challenges
1. ❌ DOTween dependency → replace with Godot Tween API
2. ⚠️ MVVM + Zenject → Godot has built-in signals, less DI-heavy
3. ⚠️ Addressables + GD singleton → use Godot autoload (singleton) + Resource loader
4. ⚠️ Scene additive loading → different in Godot; use SubViewports or layer logic
5. ⚠️ 60+ scenes + complex hierarchy → plan scene structure carefully in Godot

### Key Migration Steps
1. Convert StateSystem hierarchy (MonoBehaviours) → Godot Node structure
2. Map EventBus → Godot signal_emit() or custom EventBus subclass
3. Convert LevelAbility ScriptableObjects → Godot Resource subclasses
4. Replace DOTween tweens → Godot Tween API (already modern)
5. Refactor MVVM binders → Godot `bind_property_toggle_button()` style helpers
6. Move from Addressables → Godot preload() / load() or ResourceLoader

This is a **very well-structured codebase** for refactoring—modular, event-driven, and relatively framework-agnostic. Godot's scene-based architecture aligns well with the component/composition patterns already in place.