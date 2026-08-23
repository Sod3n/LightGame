# LightGame — Mechanics Reference

*Purpose: single source of truth for every gameplay mechanic + its implementation footprint. Use this to spot over-engineering.*

## Table of Contents

- [Cross-cutting infrastructure](#cross-cutting-infrastructure)
- [Player Movement](#player-movement)
- [Player Health & Death](#player-health--death)
- [Damage Trigger (hazards)](#damage-trigger-hazards)
- [Damage In Dark / Heal In Light / Danger Light](#damage-in-dark--heal-in-light--danger-light)
- [Light System (core)](#light-system-core)
- [Light-based Teleport Skill](#light-based-teleport-skill)
- [Simple Teleport Trigger](#simple-teleport-trigger)
- [Scene Transition (LevelChange light)](#scene-transition-levelchange-light)
- [Jump Pad](#jump-pad)
- [Checkpoint](#checkpoint)
- [Weight Trigger & Weight](#weight-trigger--weight)
- [Push Strength](#push-strength)
- [Pushable Object](#pushable-object)
- [Projectile + ProjectileSpawner](#projectile--projectilespawner)
- [Enemy (Slime)](#enemy-slime)
- [Enemy Projectile](#enemy-projectile)
- [Abilities (progression)](#abilities-progression)
- [Moving / Swinging Platforms](#moving--swinging-platforms)
- [One-Way Platform, Ground, Wall, Spike, Mirror](#one-way-platform-ground-wall-spike-mirror)
- [Hide Platforms (light-visibility VFX)](#hide-platforms-light-visibility-vfx)
- [Parallax + Distance fade](#parallax--distance-fade)
- [Light Ray (growing beam)](#light-ray-growing-beam)
- [Main Menu](#main-menu)
- [Global observations](#global-observations)

---

## Cross-cutting infrastructure

These are shared systems that most mechanics below reference. Documented once here rather than in every section.

- **EventBus** (`Assets/_Game/Globals/EventBus.cs`) — static, type-keyed pub/sub. Two flavours: global (`Publish<T>(evt)`) and per-GameObject (`Publish<T>(target, evt)`). Almost every mechanic uses the per-GameObject variant to route damage / heal / light / weight events to the specific instance.
- **`Trigger`** (`Assets/_Game/Features/SceneTransition/Trigger.cs`) — the light-source-side component. One `Trigger` sits on every light zone. It: raycasts to a `LightDetector` overlapping its collider, calls `LightDetector.AddLightSource / RemoveLightSource`, carries a `LightType` (Default vs LevelChange), a next-scene or target-scene setting, and doubles as the teleport landing target (`GetTeleportLandingPosition()`). Auto-wires `collider2D`, `lightRigidbody`, `targetLightPoint` in `OnValidate()` and sets the tag to `"Teleport"` in `Reset()`.
- **`LightDetector`** (`Assets/_Game/Features/Light/LightDetector.cs`) — the receiver side. Sits on every object that reacts to light (player, cubes, hide-platforms, damage-in-dark objects). Tracks light sources per `LightType` and publishes `LightChangeEvent` per-GameObject whenever the lit state per type flips. Also exposes `LightBlockCheck()` for the `Trigger` raycast.
- **`LightChangeEvent`** (`Assets/_Game/Core/Interfaces/LightChangeEvent.cs`) — the event type every light-reactive system subscribes to via `EventBus.Subscribe<LightChangeEvent>(gameObject, handler)`. Carries the optional `LightType` (Default / LevelChange) and (for LevelChange) the target scene. The former `ILightable` interface was deleted this session — subscription IS the contract.
- **`PlayerMain`** (`Assets/_Game/Features/Player/Scripts/PlayerMain.cs`) — the player state-machine root; owns the shared state instances (Idle/Walk/Jump/Land/Dash/CrouchIdle/CrouchWalk/WallGrab/WallClimb/WallJump/WallSlide/DirectionalJump) and the animator/rigidbody/collider/input references. `PushableObject` inherits from this class (intentional black box; see Pushable Object entry). **Do not modify `PlayerMain` or its `PlayerData` — see next entry.**
- **`PlayerData`** (`Assets/_Game/Features/Player/Scripts/PlayerData.cs`) — **⚠️ Intentional black box.** The developer confirmed this is open-sourced third-party code, not authored in-house. It is a fat 380-line `[Serializable]` data blob attached to `PlayerMain`. Physics, Walk, Crouch, Jump, Land, Dash, Walls (Slide/Grab/Climb/Jump) — each a nested class with dozens of curves and timers. **Rule for future edits:** treat as opaque; do not restructure, do not split `[NonEditable]` runtime fields, do not refactor. Any Model/Runtime split concerns raised by the audit apply *outside* this file only.
- **`MonoBehaviourWithData<T>`** (`Assets/_Game/Core/MonoBehaviourWithData.cs`) — trivial generic base: one `[SerializeField] T data` field plus a `Data` property. Used by all the light-reactive systems and effect controllers.
- **`MonoBehaviourEffect<T>`** (`Assets/_Game/Core/MonoBehaviourEffect.cs`) — a `MonoBehaviourWithData<T>` subclass that ticks on a rate + duration + infinity flag. `DamageOverTimeEffect` and `HealOverTimeEffect` derive from it. Effects are added and removed with `AddComponent` / `Destroy` at runtime.
- **`Togglable`** (`Assets/_Game/Core/ITogglable.cs`) — abstract base with `Enable()` / `Disable()`. Only one concrete implementation in gameplay code (`LightProjectorView` in UI), but `WeightTrigger` still exposes a `List<Togglable>` in the inspector.
- **`Game` + `SceneLoader` + `LevelOrder`** (`Assets/_Game/Globals/`) — static globals. `Game.LevelOrder` returns the next scene; `SceneLoader.GetCurrentLevel()` returns the current scene enum.
- **Tags in use:** `"Player"`, `"Teleport"`, `"Light"`, `"PassLight"`, `"HideObject"`. **Layers in use:** `Player`, `Ground`, `Wall`, `BumpHead`, `LightLayer`, `LightSource`, `OneWayPlatform`, `MovingPlatform`, `Enemy`, `HiddenGround`, `Hidden`.

---

## Player Movement

**Player-facing:** The character walks, crouches, jumps (with multi-jump/coyote/jump-buffer), dashes, grabs / climbs / slides on walls, and wall-jumps. All movement is animation-curve driven for hand-tuned feel.

**Rules:**
- Movement is a state machine (12 states) driven by `PlayerData` config.
- Every acceleration and deceleration is a designer-authored `AnimationCurve` (SpeedUp / SlowDown / TurnBack for walk, crouch, wall-slide, wall-climb).
- Jumps are height-curves — the state derives a velocity curve from them at Start (see `PlayerMain.Start`).
- Slope handling: max walkable angle, corner-slide angle, moving-platform velocity damping — all curve-based.
- Coyote-time and jump-buffer per jump index and per wall-jump.
- Wall stamina: three exhaust modes (None / TimeBased / ClimbAmountBased); drains per second or per unit, regens per second, optional "can still jump when exhausted".
- Push slow-down: `PlayerMain.PushObjectSlowdown` (float) and `PlayerMain.IsPushingObject` (bool) are non-serialized fields that `PushableObject` sets when the player is pushing a cube; states are supposed to read these to slow down.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/Player/Scripts/PlayerMain.cs` — MonoBehaviour root, holds all state instances.
  - `Assets/_Game/Features/Player/Scripts/PlayerData.cs` — 380-line nested `[Serializable]` data blob (Physics, Walk, Crouch, Jump, Land, Dash, Walls).
  - `Assets/_Game/Features/Player/Scripts/PlayerView.cs` — animator/sprite view (86 lines).
  - `Assets/_Game/Features/Player/Scripts/Essentials/EssentialPhysics.cs` — shared ground/wall/head/slope checks (284 lines).
  - `Assets/_Game/Features/Player/Scripts/Input System/InputActions.cs` + `.inputactions` — generated input asset.
  - `Assets/_Game/Features/Player/Scripts/Input System/InputManager/PlayerInputManager.cs` — reads input, exposes `Input_Walk`, jump/dash/crouch buffers (153 lines).
  - `Assets/_Game/Features/Player/Scripts/State System/PlayerStateMachine.cs` — `ChangeState`/`Initialize`.
  - `Assets/_Game/Features/Player/Scripts/State System/Base States/MainState.cs` — abstract base state (98 lines).
  - `Assets/_Game/Features/Player/Scripts/State System/Child States/Player{Idle,Walk,Jump,Land,Dash,CrouchIdle,CrouchWalk,WallGrab,WallClimb,WallJump,WallSlide,DirectionalJump}State.cs` — twelve concrete states.
- **Prefab:** The player is a child of `GeneralLevel.prefab` (`Assets/_Game/Features/Environment/Prefabs/GeneralLevel.prefab`). Root player GameObject components: `SpriteRenderer, CapsuleCollider2D, Rigidbody2D, Animator, PlayerMain, PlayerInputManager, Weight, LightDetector, PlayerHealthSystem, PlayerTeleportSystem, LevelChangeTrigger, PushStrength, AbilityManager, PlayerView`. Children: `Eyeglass` (visual), `PointTP` (teleport landing collider — CircleCollider2D), `LandingEffectPoint` (transform marker).
- **Depends on:** `PlayerData` asset (per-player tuning), `EssentialPhysics` for physics probes.

**Level-designer configuration:** The `PlayerData` block on the Player GameObject exposes ~100 tunable fields (curves, times, forces, layer masks, stamina, dash cooldown, slope angles, damping curves). Almost all are direct-designer knobs.

**Complexity budget:** ⚠️ Suspicious — but this is where the game lives, so the tolerance is higher.
- `PlayerData` has ~40 fields marked `[NonEditable]` that are runtime scratch (`isGrounded`, `wallDirection`, `currentSlopeAngle`, `contacts`, `dashCooldownTimer`, etc.) intermixed with real config. Serializing all of it clutters the inspector and grows the scene YAML. Suggestion: split runtime scratch off into a separate non-`[Serializable]` `PlayerRuntime` class, leaving `PlayerData` purely as designer knobs.
- 12 state files is a lot but each is small (30–200 lines) — this is unusually not the biggest problem here.

---

## Player Health & Death

**Player-facing:** Player has integer HP. Contact with hazards or being in the dark drains it. On death, respawn.

**Rules:**
- `HealthSystem` clamps HP between 0 and `MaxHealth`. `TakeDamage` grants `invincibilityDuration` seconds of i-frames on non-lethal hits.
- `nearDeathThreshold` (default 1): entering / leaving near-death publishes `OnNearDeathStateChanged(bool)`. `PlayerHealthSystem` uses this to disable abilities that have `disableInNearDeath = true`.
- `PlayerHealthSystem.Die()` zeroes the rigidbody and publishes a global `PlayerDiedEvent`. Respawn is handled elsewhere (by a `Checkpoint` handler / `PlayerDeathView` UI feature not in scope here).
- Damage events route via `EventBus.Publish(gameObject, new DamageEvent { Amount })` so multiple systems can drive damage on the same object.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/Health/HealthSystem.cs` — base HP system, i-frames, near-death detection.
  - `Assets/_Game/Features/Health/PlayerHealthSystem.cs` — overrides `Die` and `TakeDamage` for player-specific behaviour, wires near-death to `AbilityManager`.
- **Prefab:** on the Player root (see Player Movement).
- **Depends on:** `EventBus` `DamageEvent`/`HealEvent`, `AbilityManager`, `Rigidbody2D` on same GameObject.

**Level-designer configuration:**
- `health` — starting/max HP.
- `invincibilityDuration` — i-frame seconds.
- `nearDeathThreshold` — HP at or below this triggers near-death state.

**Complexity budget:** ✅ Fits — clean two-file split (base + player-specific), well-scoped.

---

## Damage Trigger (hazards)

**Player-facing:** Any object with a `DamageTrigger` deals damage when the player overlaps (or collides).

**Rules:**
- Trigger vs collision mode is a bool. Trigger-mode requires the Collider to be `isTrigger`, collision-mode requires it to be non-trigger.
- Deals damage once per enter, tracked in a `HashSet<GameObject>`. Exit removes the entry so re-entry re-damages.
- Fires `DamageEvent` on the other GameObject via EventBus.

**Implementation:**
- **Scripts:** `Assets/_Game/Features/Health/DamageTrigger.cs` (43 lines).
- **Prefabs using it:** `EnemyProjectile.prefab`, `Spike.prefab` (both at repo prefab paths under `Features/Enemy` and `Features/Environment`).
- **Depends on:** `EventBus`, `Collider2D` on same GameObject, `HealthSystem` on target.

**Level-designer configuration:**
- `damage` — HP dealt per hit.
- `useCollision` — trigger vs collision mode.

**Complexity budget:** ✅ Fits — one file, one job, tooltips already added.

---

## Damage In Dark / Heal In Light / Danger Light

**Player-facing:** When the player is in the dark, HP drains over time; when in light, HP regens over time. `DangerLight` swaps the meaning (light hurts, dark heals) for boss/puzzle rooms.

**Rules:**
- Listens for per-GameObject `LightChangeEvent` filtered to `LightType.Default` (ignores LevelChange lights). `DangerLightSystem` does not filter and reacts to any light change.
- On state-flip, `AddComponent<DamageOverTimeEffect>` or `<HealOverTimeEffect>` and assigns the data, or `Destroy`s the running effect.
- `DamageOverTimeEffect` publishes a `PendingDamageEvent` for UI (health-bar preview of the next tick), then ticks damage every `Rate` seconds up to `Duration` (or forever if `IsInfinity`).
- If `ResetTickOnHealthChange`, the effect subscribes to `DamageEvent`/`HealEvent` on the same GameObject and resets its tick timer whenever the player takes other damage or heals (so you never take a "double hit" the frame you enter darkness).
- `DamageOverTimeEffect.Tick` clamps: never reduces HP below 1 (so damage-over-time alone can't kill the player).

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/Health/DamageInDarkSystem.cs` — MonoBehaviourWithData; light listener.
  - `Assets/_Game/Features/Health/HealInLightSystem.cs` — mirror of the above for healing.
  - `Assets/_Game/Features/Health/DangerLightSystem.cs` — carries two `EffectData` structs (heal + damage), swaps roles.
  - `Assets/_Game/Features/Health/DamageOverTimeEffect.cs` — the ticker; extends `MonoBehaviourEffect<EffectData>`.
  - `Assets/_Game/Features/Health/HealOverTimeEffect.cs` — twin of the above.
  - `Assets/_Game/Events/PendingDamageEvent.cs`, `PendingHealEvent.cs` — for UI preview.
- **Depends on:** `LightDetector` on same GameObject (via `[RequireComponent]`), `EventBus`, `HealthSystem` on same GameObject.

**Level-designer configuration:**
- Effect data: `Amount`, `Rate` (seconds per tick), `Duration`, `IsInfinity`, `ResetTickOnHealthChange`.

**Complexity budget:** ⚠️ Suspicious.
- `DamageInDarkSystem`, `HealInLightSystem`, `DangerLightSystem` are three near-identical files (~60 lines each) that only differ in "start on lit / start on dark / swap". Could collapse to one `LightReactiveEffect` with a `[SerializeField] enum { HealWhenLit, DamageWhenDark, Both }`.
- `Add/Destroy` component churn per light-transition is unusual — most projects would flip a boolean on a permanent component. Justified only if you truly want the effect's own lifecycle (`OnStart`, tick cadence reset on destroy) — that logic already lives in `MonoBehaviourEffect`, so it does buy something, but it's an unusual pattern for such a common gameplay condition.

---

## Light System (core)

**Player-facing:** Light zones (usually spot-cones cast by lamps) are safe — being inside them heals, unlocks teleport, hides some platforms. Being outside them hurts. This is the game's central mechanic.

**Rules:**
- Each light zone is a `Trigger` collider with a `LightType` (Default or LevelChange).
- `Trigger` raycasts from its `targetLightPoint` to every overlapping `LightDetector`. If the ray is not blocked by any non-`PassLight` collider on `lightBlockingLayers`, the `LightDetector` is registered as lit.
- `LightDetector` publishes a per-GameObject `LightChangeEvent(isInLight, LightType)` on each type-flip and a general one on any-lit-flip. Also fires two `UnityEvent<bool>` (`onChangeState` and `onChangeStateInverse`) that are wired in-prefab.
- On the light-source side, `Light2DGlobalListener` sits on the URP `Light2D` and lets global `EventBus` events (`SetLightIntensityEvent`, `FadeToBlackEvent`, `FadeToFullEvent`, `RestoreLightIntensityEvent`, `SetLightIntensityImmediateEvent`) tween the light's intensity — used by scene transitions.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/Light/LightDetector.cs` — 146 lines. Tracks per-type light sources, publishes events.
  - `Assets/_Game/Features/SceneTransition/Trigger.cs` — 117 lines. The light zone.
  - `Assets/_Game/Features/Light/Light2DGlobalListener.cs` — 197 lines. Ties URP Light2D to the global event bus for cinematic fades.
  - `Assets/_Game/Events/GlobalLightEvents.cs` — 5 event classes + `GlobalLightEvents` static facade.
  - `Assets/_Game/Core/Interfaces/ILightable.cs` — interface + `LightChangeEvent` payload.
- **Prefabs**
  - `Features/Light/Prefabs/StationaryLight.prefab` — root: `Rigidbody2D, Platform, LightProjectorView`. Child `Spot Light` (tag `Teleport`) holding two `Spot Light 2D` children, `Holder` and `Point` (both tag `Light`, layer `LightSource`, with `BoxCollider2D` + `Rigidbody2D` used as the ray endpoint), and `TriangleCollider` (tag `Teleport`) with `PolygonCollider2D + Trigger + Rigidbody2D` and a child `TeleportPoint`.
  - `MovingLight.prefab` — identical hierarchy, no `Holder`, driven by the `Platform` waypoint mover on the root.
  - `LevelChangeLight.prefab` — identical to StationaryLight but the `Trigger.lightType` is `LevelChange`.
  - `LevelEntryLight.prefab` — the "entered from" light shown briefly at spawn; no `Platform` component.
- **Depends on:** `PassLight` tag on cubes so they don't block themselves, `Light` tag on light-source colliders, `LightSource` layer, `LightLayer` layer.

**Level-designer configuration:**
- `Trigger`: `lightType`, `useNextScene`, `targetScene`, `teleportLandingPoint`, `teleportLandingOffset` (auto-wired: `collider2D`, `lightRigidbody`, `targetLightPoint`).
- `LightDetector`: `onChangeState` UnityEvent, `onChangeStateInverse` UnityEvent, `lightBlockingLayers` mask.
- `Light2DGlobalListener`: `lightType`, `duration`, `ease`, `autoRegisterOnEnable`.

**Complexity budget:** ⚠️ Suspicious.
- The prefab hierarchy is very deep (5+ nested GameObjects per light, several with their own `Rigidbody2D`). Two `Spot Light 2D` children per prefab (verify — likely one for the visible cone and one for a secondary halo; if not, one is dead weight). A `Rigidbody2D` on `TriangleCollider` and separately on the light root and on `Point` — three rigidbodies per light is a lot; likely only one is load-bearing.
- Both `LightDetector.OnChangeState` (`UnityEvent<bool>`) and per-GameObject `LightChangeEvent` on EventBus fire in parallel. Two parallel event channels for the same state doubles the surface area — the UnityEvent is used in-prefab; the EventBus form is used by C# subscribers. Consider standardising on one.
- The `_lightSourcesByType` dictionary hard-codes exactly `Default` and `LevelChange` — but the enum has only those two entries so a per-enum-value pair of bools + hashsets would be simpler than a `Dictionary<LightType, HashSet<>>` with runtime lookups.

---

## Light-based Teleport Skill

**Player-facing:** While the player is inside a light, they can aim (mouse or gamepad right-stick) at another light and press the execute key to teleport to it. Cancels ~0.5s after leaving light.

**Rules:**
- Enabled when the player is in a `LightType.Default` light zone; auto-disables ~0.5s (`disableDelay`) after leaving light.
- Aim raycasts up to `Radius` units. First `Teleport`-tagged collider is the "starting" light (must exit it first), the *next* `Teleport`-tagged collider is the destination. This is what `StartLightFiltr` gates.
- Landing position = the destination `Trigger`'s `GetTeleportLandingPosition()` (either `teleportLandingPoint.position` or the transform's position, plus `teleportLandingOffset`). Falls back to hit-point if no `Trigger` component.
- Executing forces the player state to `IdleState` so movement scratch (dash timer, coyote, etc.) is reset. Preserves horizontal velocity, zeros vertical.
- Uses the New Input System — `InputActions.Player.TeleportAim` + `.TeleportExecute`.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/Teleport/PlayerTeleportSkill.cs` — 170 lines. The actual skill: aim, raycast, execute.
  - `Assets/_Game/Features/Teleport/PlayerTeleportSystem.cs` — 98 lines. Owns a `PlayerTeleportSkill`, toggles it based on `LightChangeEvent`, with a `disableDelay` coroutine.
  - `Assets/_Game/Features/Abilities/AbilitiesSystem/TeleportAbility.cs` — `LevelAbility` ScriptableObject that `AddComponent`s `PlayerTeleportSystem` when the player unlocks it.
- **Prefab:** on the Player root; `Data.TeleportPoint` is `PointTP` child (the visual indicator sphere).
- **Depends on:** `Trigger` component on the destination light, `Teleport` tag on any teleportable collider, `LightDetector` on player, `PlayerMain` state machine, `Rigidbody2D` on player.

**Level-designer configuration:**
- `TeleportData.Radius` — max teleport distance.
- `TeleportData.TeleportPoint` — the target-preview GameObject.
- `PlayerTeleportSystem.disableDelay` — grace period before disable after leaving light.

**Complexity budget:** ⚠️ Suspicious.
- Two scripts to model "the ability is on/off" is one script too many. `PlayerTeleportSystem` mostly exists to `AddComponent<PlayerTeleportSkill>` and flip `.enabled`. The Skill's own `OnEnable/OnDisable` could subscribe to `LightChangeEvent` directly and drop the wrapper.
- `PlayerTeleportSkill.OnDestroy` unconditionally dereferences `Data.TeleportPoint.SetActive(false)` — this will NRE if Data is null on domain reload.
- Overlapping mechanic with `SimpleTeleportTrigger`: see [Global observations](#global-observations).

---

## Simple Teleport Trigger

**Player-facing:** A teleport pad. Walk into it, get teleported to a linked pad. Level-designer places pairs.

**Rules:**
- On trigger enter, checks `requiredTag` (default `"Player"`) and `teleportLayers` mask, respects a `teleportCooldown`.
- Moves `Rigidbody2D.position` (or `transform.position` if no rb) to destination + offset. Optionally zeros velocity, optionally keeps horizontal velocity.
- Forces `PlayerMain` state to Idle and calls `InputManager.ClearInput()` to prevent double-triggering.
- On teleport, temporarily disables `linkedTeleport` for `linkedTeleportDisableTime` so the player doesn't bounce back immediately.
- Fires `UnityEvent onTeleport` for VFX / audio.

**Implementation:**
- **Scripts:** `Assets/_Game/Features/SceneTransition/SimpleTeleportTrigger.cs` (135 lines).
- **Prefabs:** none — placed directly in scenes.
- **Depends on:** `PlayerMain`, `Rigidbody2D` on target.

**Level-designer configuration:**
- `destinationPoint`, `destinationOffset`, `linkedTeleport` (bidirectional link), `resetVelocity`, `preserveHorizontalVelocity`, `teleportCooldown`, `linkedTeleportDisableTime`, `teleportLayers`, `requiredTag`, `onTeleport`.

**Complexity budget:** ⚠️ Suspicious — the script itself is fine, but its very existence overlaps with the light-based teleport. See global observations. Also, `InputManager.ClearInput()` is called on `PlayerInputManager`; if a non-player object is ever routed through here it will NRE.

---

## Scene Transition (LevelChange light)

**Player-facing:** Stepping into a special "level-change" light triggers the transition (fade, load next scene). Configured either as "next in level order" or "specific target scene".

**Rules:**
- `Trigger.lightType = LightChange` marks a light as the transition light.
- When the `LightDetector` enters this light and the type is `LevelChange`, the `LightDetector` reads the target scene from the `Trigger` (either `Game.LevelOrder.GetNextScene()` or `Trigger.TargetScene`) and passes it in the `LightChangeEvent.TargetScene`.
- `LevelChangeTrigger` on the *player* (not the light) subscribes to that event, filters for `LightType.LevelChange`, and on the rising edge publishes a global `RequestLevelChangeEvent(targetScene)` for the scene-loader / fade-out system.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/SceneTransition/LevelChangeTrigger.cs` (61 lines) — the player-side listener.
  - `Assets/_Game/Features/SceneTransition/Trigger.cs` — same file as normal lights, with the `LightType` toggle.
  - `Assets/_Game/Events/RequestLevelChangeEvent.cs` — the event.
- **Prefab:** `LevelChangeLight.prefab` (see Light System). `LevelChangeTrigger` sits on the Player root.
- **Depends on:** `LightDetector` on player, `Trigger` with `LightType.LevelChange` on the light, `Game.LevelOrder`, `SceneLoader`.

**Level-designer configuration:** All on the light's `Trigger`: `lightType`, `useNextScene`, `targetScene`.

**Complexity budget:** ✅ Fits — clean use of the light infrastructure. The one wart: `LevelChangeTrigger.OnInLightChange(bool)` exists just to satisfy `ILightable` and does nothing; the real logic is in the private `OnInLightChange(bool, SceneName?)` overload.

---

## Jump Pad

**Player-facing:** Landing on a jump pad bounces the player upward (or in a configured direction) with a configurable force multiplier.

**Rules:**
- Reads collision normal — if `useCollisionNormal`, launches along inverted normal; otherwise uses fixed `launchDirection`.
- `activationAngle` filters collisions by angle vs the pad's launch direction.
- Picks jump curve from `PlayerData.Jump.Jumps[jumpIndex-1]`; force is `jumpInfo.MaxHeight * forceMultiplier`.
- For upward-only jumps it drives `PlayerJumpState`; for angled jumps it drives `PlayerDirectionalJumpState` with a per-invocation `SetLaunchParameters`.
- Cooldown per pad (`cooldownTime`), and a stuck-detection routine: every `stuckCheckInterval`, if the player's speed is below `stuckVelocityThreshold`, re-fire the bounce.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/JumpPad/JumpPad.cs` — 329 lines, the actual pad.
  - `Assets/_Game/Features/JumpPad/JumpPadAdvanced.cs` — 413 lines, **dead code**. No prefab references it (verified by grep). Modes (Vertical/Directional/VelocityBased/Curve), animation-curve profiles, tag/layer filter — a whole alternative implementation nobody uses.
- **Prefab:** `Jumpad.prefab` — root has `JumpPadView, BoxCollider2D, JumpPad`. Children: `Model` (SpriteRenderer + `Holder (1)` tagged Light on layer LightSource, so the pad is also a light source), `[ Compress Tween ]`, `[ Bounce Tween ]`, `[ Ready Tween ]` (view tween components).
- **Depends on:** `PlayerMain` on the colliding object, its `PlayerData.Jump.Jumps` list, `PlayerJumpState` and `PlayerDirectionalJumpState`.

**Notably:** `JumpPad` is also on the SlimeEnemy prefab root — so bouncing on the slime uses the same code.

**Level-designer configuration:**
- `jumpIndex` (1–3), `useCollisionNormal`, `launchDirection`, `forceMultiplier` (0.5–10), `preservePerpendicularVelocity`, `activationAngle` (0–90), `cooldownTime`, `stuckCheckInterval`, `stuckVelocityThreshold`, `targetLayers`, plus 4 `Action<>` events (`OnPlayerLand`, `OnPlayerBounce`, `OnCooldownComplete`, `OnBounce`).

**Complexity budget:** ❌ Over-engineered.
- `JumpPadAdvanced.cs` is 413 lines of dead code — delete it.
- The stuck-detection routine (35 lines + fields) fires a bounce again when the player is stuck. This is a workaround for the fact that the pad drives a *state machine* instead of just setting velocity — the state may not fire when the collision is already resolved. Simplify the underlying jump-state entry and drop the stuck check.
- 20+ `Debug.Log` calls in the hot path — fine for development, cut for final.
- Four `Action<>` callbacks where `OnBounce` is literally `OnPlayerBounce` without the parameter — collapse to one.
- The gizmo has ~50 lines drawing a cone. Nice but excessive for the shipping build.

---

## Checkpoint

**Player-facing:** Walking into a checkpoint marker makes it the current respawn point. On death the player is placed here.

**Rules:**
- On `OnTriggerEnter2D`, if the other object has `PlayerMain`, set `Checkpoint.Active = this`.
- `setAsStartCheckpoint` marks one checkpoint per level as the initial active one (set in `Awake`).
- `RespawnPlayer(GameObject)` moves the player to `RespawnPosition = transform.position + respawnOffset`, zeroes velocity, and `Heal(int.MaxValue)`.

**Implementation:**
- **Scripts:** `Assets/_Game/Features/Checkpoint/Checkpoint.cs` (65 lines).
- **Prefab:** `Assets/_Game/Features/Checkpoint/Prefabs/Checkpoint.prefab` — root: `Checkpoint, BoxCollider2D, SimpleEditModeSprite`.
- **Depends on:** `PlayerMain` tag/component detection, `HealthSystem`, `Rigidbody2D` on player.

**Level-designer configuration:**
- `setAsStartCheckpoint` (mark exactly one per level as start).
- `respawnOffset`.

**Complexity budget:** ✅ Fits — single file, clear job, tooltips already present.

---

## Weight Trigger & Weight

**Player-facing:** A pressure plate. Standing on it (or standing enough heavy stuff on it) opens doors / activates linked platforms. Three modes: weight, toggle-on-enter, press-F-to-interact.

**Rules:**
- `WeightTrigger` has three exclusive modes selected by two bools (`toggleMode` and `interactMode`) — verify: no explicit "only one may be on" guard.
- **Weight mode:** on trigger-enter, publish `WeightRequestEvent` on the other object; the other object's `Weight` component fills it in from the ScriptableObject `ObjectsWeight`. Accumulate. When ≥ `weightThreshold`, invoke `onActivate` and `Enable()` all `Togglable`s.
- **Toggle mode:** each enter flips the state.
- **Interact mode:** track player presence; while inside, `Input.GetKeyDown(interactKey)` flips state.
- `startActivated` sets initial state.
- `canDeactivate` (weight-mode only): if false, once activated stays activated.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/Weight/WeightTrigger.cs` — 185 lines. The trigger.
  - `Assets/_Game/Features/Weight/Weight.cs` — 34 lines. Publishes weight on request.
  - `Assets/_Game/Features/Weight/ObjectsWeight.cs` — `ScriptableObject` mapping enum `Type { Player, MovableCube }` → float weight.
- **Prefab:** `Assets/_Game/Features/Weight/Prefabs/WeightPlatform.prefab` — root: `BoxCollider2D, WeightTrigger, WeightTriggerView`. View children: `ScalePivot > ScaleInTween/ScaleOutTween/Skin`.
- **Depends on:** `Weight` component on the pressing object, `EventBus.WeightRequestEvent`, the `Togglable` list (see below).

**Level-designer configuration:**
- `toggleMode`, `interactMode`, `interactKey` (uses legacy `Input`), `weightThreshold`, `canDeactivate`, `startActivated`, `togglables` (list), `onActivate`/`onDeactivate` UnityEvents.

**Complexity budget:** ⚠️ Suspicious.
- Three modes stuffed in one script with two exclusive bools instead of an `enum { Weight, Toggle, Interact }`. The bool combination `toggleMode=true, interactMode=true` is unhandled.
- Interact mode uses legacy `Input.GetKeyDown` while the rest of the game uses the New Input System.
- The `Togglable` abstract class exists in Core but has only one concrete gameplay implementation (`LightProjectorView` in the UI feature). In practice `onActivate`/`onDeactivate` UnityEvents cover the same job — `togglables` list is duplicate wiring. Consider deleting the `Togglable` class and having designers wire everything via UnityEvent.

---

## Push Strength

**Player-facing:** How much weight the player can push. Progression stat.

**Rules:**
- `CanPush(objectWeight)` returns `objectWeight <= maxPushableWeight`.
- `GetPushSpeed(objectWeight)` lerps push-speed multiplier down to 50% at max weight, 100% at 0 weight.

**Implementation:**
- **Scripts:** `Assets/_Game/Features/Weight/PushStrength.cs` (57 lines).
- **Prefab:** on the Player root.
- **Depends on:** `Weight` on the target.

**Level-designer configuration:**
- `maxPushableWeight`, `pushSpeedMultiplier`.

**Complexity budget:** ✅ Fits — trivial component.

---

## Pushable Object

**Player-facing:** Moveable crates. Push them by walking into them. Crouch-drag lets you pull them out of corners.

**Rules:**
- `PushableObject : PlayerMain` — the cube inherits from `PlayerMain`. Its own physics/animation uses the same state machine (only Idle/Walk/Jump/Land/WallSlide states are constructed; dash/crouch/wall states are commented out).
- On player trigger, the cube stores `_lastPlayer`. In `LateUpdate`, if the player is grounded and pressing in the correct direction, the cube copies the player's input into its own `ManualInputManager.input_Walk`, sets `PlayerData.Walk.MaxSpeed` to 1.2× the player's actual X-velocity for perfect sync, and drives its own PlayerMain state machine.
- **Push-only if standing, push-or-drag if crouching.** Direction check: `isSameDir` (player's input matches cube's direction) *or* `isStopping` *or* `isCrouching`.
- Reads `PushStrength.CanPush(weight)` from the player; if too weak, zeros own input.
- Sets `player.PushObjectSlowdown` and `player.IsPushingObject` so player states can slow themselves.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/PushableObject/PushableObject.cs` — 132 lines. The cube.
  - `Assets/_Game/Features/PushableObject/ManualInputManager.cs` — 13 lines. Empty subclass of `PlayerInputManager` that disables input polling by overriding `OnEnable` to do nothing.
- **Prefabs**
  - `Cube.prefab` — a static cube (no `PushableObject` script, no `Rigidbody2D` at root); just `BoxCollider2D, Animator, CapsuleCollider2D, ManualInputManager, SpriteRenderer`. Verify — this is probably legacy or purely visual.
  - `MovableCube.prefab` — root: `Rigidbody2D, Weight, PushableObject, BoxCollider2D, Animator, CapsuleCollider2D, ManualInputManager, SpriteRenderer`. Child `Skin > Ground/Square/Ground`.
  - `HiddableMovableCube.prefab` — as MovableCube plus `ObjectHider + LightDetector` (disappears in light).
  - `LightMovableCube.prefab` — as MovableCube plus a nested `Point Light` hierarchy (identical to StationaryLight's inner Spot Light tree) — the cube is itself a light source.
- **Depends on:** `PlayerMain` (via inheritance), `PlayerInputManager`, `Weight`, `PushStrength` on player.

**Level-designer configuration:**
- Inherits all of `PlayerData` (a full player-tuning inspector on every cube). `Weight.type` picks the `ObjectsWeight` entry.

**Complexity budget:** ⚠️ Intentional black box.
- The developer confirmed `PushableObject : PlayerMain` is a deliberate hack: the cube reuses the player's custom-physics state machine to achieve the exact same "feel" when moving. Refactoring to a composition model would require re-implementing the whole custom controller, which is not worth it.
- **Rule for future edits:** do not touch `PushableObject.cs` unless changing player physics; treat as an opaque unit that mirrors the player.
- Sub-items still worth revisiting later: `Cube.prefab` (empty variant?) and `LightMovableCube`'s duplicated Point-Light subtree (the base subtree is now cleaner post-refactor — see "Light System" changelog).

---

## Projectile + ProjectileSpawner

**Player-facing:** Stationary hazards that periodically spew projectiles.

**Rules:**
- `ProjectileSpawner` waits `rate` seconds, then fires `OnStartCharge(rate)`, waits `rate` seconds again, fires `OnCharged()` and `Instantiate`s the projectile.
- New projectile is reparented under `SceneRoot.Instance.transform`.
- `Projectile.FixedUpdate` recalculates direction from `_transform` (the target) every frame and `MovePosition`s toward it — so it's homing, not ballistic, despite being called "Projectile".
- On non-trigger overlap, destroys itself.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/Projectile/Projectile.cs` — 30 lines.
  - `Assets/_Game/Features/Projectile/ProjectileSpawner.cs` — 45 lines.
- **Prefabs:** in `Assets/_Game/General/TilemapPrefabs/ProjectileSpawner.prefab` (verify — no prefab under `Features/Projectile/Prefabs/`).
- **Depends on:** `SceneRoot.Instance`, a `directionPoint` Transform, a projectile prefab with a `Projectile` component.

**Level-designer configuration:**
- Spawner: `rate`, `projectilePrefab`, `directionPoint`, `speed`, `speedRotation`.

**Complexity budget:** ⚠️ Suspicious.
- `Projectile.FixedUpdate` reads `_transform.position` (the shooter's target reference) every frame and does `direction = target - self` — this is *homing*, not a shot. If that is intended (bees chasing the player), fine. If a straight projectile was intended, this is a bug and a simpler `rb.linearVelocity = direction * speed` at spawn would work.
- `Projectile` and `EnemyProjectile` are two entirely separate files. See global observations.

---

## Enemy (Slime)

**Player-facing:** A slime that wanders, detects the player at range, chases, and charges a multi-directional projectile blast that fires one bullet toward the player and the rest in a ring.

**Rules:**
- Wander state: random direction change every `WanderDirectionChangeInterval` seconds. Detection scan every FixedUpdate: `Physics2D.OverlapCircleAll` in `DetectionRange` on `PlayerLayerMask`.
- On detect → Aggro. Move toward player at `AggroSpeed`.
- If distance ≤ `AttackRange` and `AttackCooldownTimer` ≤ 0, enter Attack state.
- Attack: freeze rotation, stop movement, wait `ChargeTime` seconds (view sees `OnChargeStart/Stop/Attack`), then fire `ProjectileCount` projectiles in a full ring; the first bullet always aims at the player.
- Movement supports wall/ceiling walking via `EnemyMovementNew` — surface detection via `OverlapCircleAll`, rotates the slime to align with surface normal, gravity toward nearest surface. If `CanWalkOnWalls` is off, uses `MoveGroundOnly` (standard `linearVelocity.x = speed`).

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/Enemy/SlimeEnemy.cs` — 161 lines. MonoBehaviour root, state-machine wiring, gizmos.
  - `Assets/_Game/Features/Enemy/EnemyData.cs` — 103 lines. `ScriptableObject` with nested Physics/Movement/AI/Attack classes.
  - `Assets/_Game/Features/Enemy/EnemyMovement.cs` — 562 lines. **Dead code** — never instantiated (grep confirms only `EnemyMovementNew` is `new`'d in `SlimeEnemy.Awake`).
  - `Assets/_Game/Features/Enemy/EnemyMovementNew.cs` — 430 lines. The actual movement.
  - `Assets/_Game/Features/Enemy/EnemyInputManager.cs` — 136 lines. AI-driven "input" (detect player, decide walk/attack), added at Awake if missing.
  - `Assets/_Game/Features/Enemy/EnemyStateMachine.cs` — 21 lines. Same shape as PlayerStateMachine.
  - `Assets/_Game/Features/Enemy/EnemyState.cs` — 96 lines. Base state with ground/wall checks.
  - `Assets/_Game/Features/Enemy/States/SlimeWanderState.cs` (38 lines), `SlimeAggroState.cs` (43), `SlimeAttackState.cs` (163) — the three concrete states.
- **Prefab:** `Assets/_Game/Features/Enemy/Prefabs/SlimeEnemy.prefab` — root: `Rigidbody2D, CapsuleCollider2D, SlimeEnemy, SlimeView, JumpPad`. Children: `ScalePivot > ChargeTween, AttackTween, Skin (SpriteRenderer + ShadowCaster2D), Core (Light2D)`.
- **Depends on:** `EnemyData` asset (`SlimeData.asset`), the `EnemyProjectile.prefab`, `Player` layer for detection, `JumpPad` on root (bouncing on the slime uses the shared JumpPad script).

**Level-designer configuration:** `EnemyData` scriptable object exposes ~25 tunable fields: ground/wall masks, wanderSpeed, aggroSpeed, gravityScale, canWalkOnWalls, canWalkOnCeiling, detectionRange, attackRange, playerLayerMask, wanderDirectionChangeInterval, projectilePrefab, projectileCount, projectileSpeed, projectileSpeedRotation, attackCooldown, chargeTime.

**Complexity budget:** ❌ Over-engineered.
- **562 lines of dead `EnemyMovement.cs`** (see grep). Delete it. This is the single easiest cleanup in the codebase.
- The `EnemyMovementNew` file has 8 `Debug.Log` and 15+ `Debug.DrawRay`/`DrawCircle` calls in `FixedUpdate`. Fine for dev, cut for ship.
- `SlimeEnemy` has 4 `Debug.Log` calls in `OnCollisionEnter/Stay/Exit` — spams the log. Cut.
- Enemy has a JumpPad on root — meaning stomping the slime bounces the player. This is the shared JumpPad code. Fine but worth documenting as a design pattern rather than being a surprise.
- `EnemyData` mirrors `PlayerData`'s structure (nested `[NonEditable]` runtime scratch mixed with config) — same over-serialisation smell.
- `EnemyInputManager` is added at Awake if missing — since the only prefab already has it in the hierarchy (verified via prefab dump — actually no, the dump shows `SlimeEnemy` root does not have EnemyInputManager, it's added at runtime). The AI runs in the Input manager, not in the states — states just read `input_Walk` and `input_Attack`. This split is unusual for a single-enemy game; a normal AI controller would decide + act in one place.

---

## Enemy Projectile

**Player-facing:** The slime's ring-of-bullets projectiles: arc under gravity, rotate to face travel direction, deal damage on contact.

**Rules:**
- `Initialize(direction, speed, rotSpeed)` sets `linearVelocity = direction * speed`, then gravity takes over.
- `gravityScale` (default 2) makes it fall in a cannon-like arc.
- `FixedUpdate` lerps rotation to match current velocity direction.
- Destroys on any non-trigger collider or trigger collision, or after `lifetime` seconds.

**Implementation:**
- **Scripts:** `Assets/_Game/Features/Enemy/EnemyProjectile.cs` (74 lines).
- **Prefab:** `Assets/_Game/Features/Enemy/Prefabs/EnemyProjectile.prefab` — root: `DamageTrigger, Rigidbody2D, BoxCollider2D, SpriteRenderer, EnemyProjectile`. Children: `DirectionPoint`, `Freeform Light 2D`.
- **Depends on:** `Rigidbody2D` on same GameObject.

**Level-designer configuration:**
- `gravityScale`, `lifetime`, `rotationLerpSpeed`, `rotationOffset`.

**Complexity budget:** ⚠️ Suspicious.
- Two completely different Projectile scripts (`Projectile` and `EnemyProjectile`) with overlapping intent: both have `Rigidbody2D`, both self-destruct on contact, both track direction. `Projectile` is homing, `EnemyProjectile` is ballistic. If both are needed, name them `HomingProjectile` and `BallisticProjectile`; if not, merge.

---

## Abilities (progression)

**Player-facing:** As the player reaches specific levels, they permanently unlock abilities (Dash, Double-Jump, Push Strength upgrade, Health upgrade, Teleport, Heal in Light, Damage in Dark).

**Rules:**
- `LevelAbility` = base `ScriptableObject`. Each ability is a data asset (`Dash.asset`, `DoubleJump.asset`, etc.) with an `unlockAtLevel` (`SceneName`), an `unlockBehavior` (UnlockAtLevel / AlwaysUnlocked / TemporaryUnlock), and a `disableInNearDeath` bool.
- `AbilityManager` (on Player root) at `Start`:
  - Activates all `AlwaysUnlocked` abilities.
  - Reads current scene, `UnlockAbilitiesUpToLevel(current)` — unlocks everything at or before this level.
- `UnlockAbility` puts the ability in `_activeAbilities` and calls its `.Activate(player)`. If behaviour is UnlockAtLevel or AlwaysUnlocked, also `_permanentAbilities` so it can't be removed.
- Near-death handling: `PlayerHealthSystem` calls `AbilityManager.DisableAllAbilities()` at the near-death threshold; only abilities with `disableInNearDeath=true` are deactivated; `EnableAllAbilities` reactivates them.
- Each ability type overrides `Activate(GameObject)` / `Deactivate(GameObject)` differently: some flip a data flag (`DashAbility` toggles `PlayerData.Dash.IsDashEnabled`), some add a component (`TeleportAbility` adds `PlayerTeleportSystem`, `DamageInDarkAbility` adds `LightDetector` + `DamageInDarkSystem`), some add a jump to the jump list (`DoubleJumpAbility`), some raise max HP (`HealthUpgradeAbility`).

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/Abilities/AbilityManager.cs` — 145 lines.
  - `Assets/_Game/Features/Abilities/LevelOrder.cs` — 45 lines. Scene order lookup.
  - `Assets/_Game/Features/Abilities/AbilitiesSystem/LevelAbilities.cs` — the "level-to-abilities" ScriptableObject.
  - `Assets/_Game/Features/Abilities/AbilitiesSystem/LevelAbility.cs` — base class.
  - Concrete abilities: `TeleportAbility.cs`, `DashAbility.cs`, `DoubleJumpAbility.cs`, `HealthUpgradeAbility.cs`, `DamageInDarkAbility.cs`, `HealInLightAbility.cs`, `PushStrengthAbility.cs`.
- **Assets:** `Assets/_Game/Features/Abilities/Data/*.asset` (one per ability + `LevelAbilities.asset` + `LevelOrder.asset`).
- **Depends on:** `Game.LevelAbilities`, `SceneLoader.GetCurrentLevel()`, `HealthSystem`, `PlayerMain`, `PlayerData`, `PlayerTeleportSystem`.

**Level-designer configuration:**
- Per-ability asset: `unlockAtLevel`, `unlockBehavior`, `disableInNearDeath`, and ability-specific fields (e.g. `HealthUpgradeAbility.healthIncrease`, `DoubleJumpAbility.jumpInfo`, effect data on Damage/Heal abilities).
- `LevelAbilities.asset` maps levels to lists of abilities.
- `LevelOrder.asset` orders the scenes.

**Complexity budget:** ✅ Fits — a clean progression system. One-line quibble: `DoubleJumpAbility.Deactivate` "removes the last jump added" — if two DoubleJump abilities are unlocked and one is deactivated, it may remove the wrong one. Verify or store the added entry.

---

## Moving / Swinging Platforms

**Player-facing:** Platforms that move on a path or swing on a pivot.

**Rules:**
- `Platform` (`Assets/_Game/Features/Platform/Platform.cs`, 76 lines) — moves the rigidbody along a `List<Transform> waypoints`, ping-pongs. In `Awake`, reparents waypoints to platform's parent so they stay in world space when the platform moves.
- `SwingingPlatform` (`Assets/_Game/Features/Platform/SwingingPlatform.cs`, 32 lines) — spins the rigidbody's `angularVelocity` between `-maxAngle` and `+maxAngle`.
- `MovedPlatform` (`Assets/_Game/Features/Environment/MovedPlatform.cs`, 95 lines) — an older, `Rigidbody`-not-`Rigidbody2D` moving-platform with `PositivBorderOffset`/`NegativeBorderOffset` bounds. Comments in Cyrillic. **Not referenced in any level scene** (verified via grep on `FirstDesignedLevel.unity`). Likely dead.

**Implementation:**
- **Scripts** as above.
- **Prefab:** `Assets/_Game/Features/Environment/Prefabs/MovingPlatform.prefab` — root: `BoxCollider2D, SpriteRenderer, Platform, Rigidbody2D` + two `MovingPlatform_Waypoint_*` transform children.
- **Depends on:** `Rigidbody2D` on same GameObject.

**Level-designer configuration:**
- `Platform`: `waypoints`, `speed`, `waypointThreshold`.
- `SwingingPlatform`: `maxAngle`, `speed`.

**Complexity budget:** ⚠️ Suspicious.
- `MovedPlatform` (95 lines, uses 3D `Rigidbody`, Cyrillic comments) appears to be legacy. Verify — if unreferenced, delete.
- Verify whether `SwingingPlatform` and `Platform` are both used — grep shows `SwingingPlatform` is only in `FirstDesignedLevel.unity` and its own file.

---

## One-Way Platform, Ground, Wall, Spike, Mirror

**Player-facing:** Static level building blocks.

- **Ground / Wall** — plain `BoxCollider2D + SpriteRenderer`. No scripts. ✅ Fits.
- **OneWayPlatform** — `BoxCollider2D + SpriteRenderer + PlatformEffector2D`. Behaviour is 100% Unity built-in. ✅ Fits.
- **Spike** — root: `DamageTrigger, SpriteRenderer, PolygonCollider2D, JumpPad`. Child: `Freeform Light 2D + Light2DGlobalListener`. Interesting: the spike is also a jump pad (bouncing off spikes without dying? verify — usually you'd only expect DamageTrigger). Verify intent.
- **Mirror** (`Assets/_Game/Features/Environment/Mirror.cs`, 13 lines) — has a public `Reflect()` that just does `ReflectableLight.SetActive(true)`. Only used inside `TestLevels/Mirror and LIghtRay.unity`. Not integrated into any real level. Verify — likely a prototype.
- **HidenObjectTemplate** — root: `BoxCollider2D`. Child `Model` with `MeshFilter, MeshRenderer, BoxCollider (x2), ObjectHider`. The `ObjectHider` on Model needs a `LightDetector` sibling (RequireComponent) but the dump shows no `LightDetector` on the Model child — verify. Suspect this template is broken or unused.

**Complexity budget:** ✅ Mostly fits. Spike's JumpPad and Mirror deserve verification.

---

## Hide Platforms (light-visibility VFX)

**Player-facing:** Blocks that only exist when lit (or only exist when dark, depending on config). Level-designer visible.

**Rules:**
- `ObjectHider` toggles the GameObject between its original layer and layer `"Hidden"`, and enables/disables a list of `Renderer`s. Reacts to `LightChangeEvent` per-GameObject.
- `SimpleFadeHider`, `DissolveObjectHider` — richer VFX-driven versions with alpha or dissolve-shader tweens and delayed physics disable so the player doesn't fall through mid-fade.
- `ModularWallDissolve.cs` and `SpritePhysicsSync.cs` are **empty files** (0 lines) — remnants of a stripped feature. Delete.
- `SimpleBlockToSpriteSync` — 221 lines; verify what it does.

**Implementation:**
- **Scripts**
  - `Assets/_Game/Features/VFX/ObjectHider.cs` — 55 lines.
  - `Assets/_Game/Features/VFX/DissolveObjectHider.cs` — 207 lines.
  - `Assets/_Game/Features/VFX/SimpleFadeHider.cs` — 220 lines.
  - `Assets/_Game/Features/VFX/SimpleBlockToSpriteSync.cs` — 221 lines.
  - `Assets/_Game/Features/VFX/SpriteAlignToGround.cs`, `SpriteBendToGround.cs` — sprite-warping helpers (90, 407 lines).
  - `Assets/_Game/Features/VFX/EditModeSpriteRenderer.cs`, `SimpleEditModeSprite.cs` — edit-mode-only preview sprites (276, 144 lines).
  - `Assets/_Game/Features/VFX/DestroyOnAnimationEnd.cs` — 41 lines.
- **Prefabs**
  - `HidePlatforms.prefab` — root: `SimpleBlockToSpriteSync`. Six `HidePlatform (N)` children each with `ObjectHider, BoxCollider2D, LightDetector, SpriteRenderer` (tag `HideObject`, layer `HiddenGround`). Every child has its own `LightDetector` — one per platform tile.
  - `SkinedHidePlatform.prefab` — same but 12 child tiles.
- **Depends on:** `LightDetector` on each child, `EventBus`, layers `Hidden` and `HiddenGround`.

**Complexity budget:** ⚠️ Suspicious.
- Twelve `LightDetector`s on a single wall (one per tile) means 12 subscriptions per frame. If the tiles are all lit by the same source, one detector on the parent + shared state would be much cheaper.
- Three implementations of "hide me on light change" (`ObjectHider`, `SimpleFadeHider`, `DissolveObjectHider`) with different quality/perf trade-offs is fine as a designer choice, but the interface should be uniform — currently each has its own field names for renderers. Consider one base `LightHider` + strategy for fade curve.
- Delete zero-byte `ModularWallDissolve.cs` and `SpritePhysicsSync.cs`.

---

## Parallax + Distance fade

**Player-facing:** Background sprites drift with camera (parallax) and fade in/out based on distance to the player.

**Rules:**
- `ParallaxEffect`: on `LateUpdate`, offsets sprite by `cameraDelta * parallaxMultiplier`. Optional activation-distance clamp and horizontal/vertical infinite scroll (tile-repeat).
- `DistanceAlphaFade` and `DistanceAnimationControl`: find player by `playerTag`, tween alpha or animator param based on distance.

**Implementation:**
- `Assets/_Game/Features/Environment/ParallaxEffect.cs` (115 lines).
- `Assets/_Game/Features/Environment/DistanceAlphaFade.cs`, `DistanceAnimationControl.cs`.

**Complexity budget:** ✅ Fits — self-contained, no cross-mechanic entanglement.

---

## Light Ray (growing beam)

**Player-facing:** A `Light2D` shape that extends over time until it hits a wall.

**Rules:**
- `LightRayScaller`: every Update, tries to grow the shape's X by `speed * dt`. Before growing, raycasts along the parent's `right` up to `maxRayDistance` on `collisionLayerMask`, skipping the ray's own colliders. Allowed growth is clamped to the distance-to-collision minus `raycastOffset`.
- Comments in Cyrillic. Used in `Assets/_Game/Features/Light/Prefabs/LightRay.prefab` (single child `Freeform Light 2D`).

**Complexity budget:** ⚠️ Suspicious — 157 lines with all-Russian comments, only used in test scenes; verify if this is production art or prototype.

---

## Main Menu

- `Assets/_Game/Features/MainMenu/MainMenu.unity` and `MainMenu.prefab`. No scripts in this feature folder — the menu is composed entirely from `Features/UI` and `Features/Tween` (out of scope). ✅ Fits.

---

## Global observations

Patterns worth simplifying globally.

1. **Two teleport systems coexist.** `PlayerTeleportSkill` (aim + light-target raycast, unlocked as an ability) and `SimpleTeleportTrigger` (linked-pair walk-in pads). Both drive the player to Idle state and both reset velocity. Neither knows about the other. Decide which is authoritative — if the game only ships light-teleport, delete `SimpleTeleportTrigger`; if simple-pair teleports are a separate mechanic, at least share a `TeleportPlayer(GameObject, Vector2, bool preserveHVel)` helper so future changes touch one place.

2. **Two enemy movement scripts.** `EnemyMovement.cs` (562 lines) is not instantiated anywhere — only `EnemyMovementNew` is. Delete `EnemyMovement.cs` outright. Also rename `EnemyMovementNew` to `EnemyMovement`.

3. **Two projectile scripts.** `Projectile` (homing) and `EnemyProjectile` (ballistic + gravity). Their responsibilities overlap but the code doesn't. Either merge, or rename to `HomingProjectile` / `BallisticProjectile` so intent is obvious.

4. **Two jump pads.** `JumpPad.cs` is used in prefabs; `JumpPadAdvanced.cs` (413 lines) is used nowhere. Delete `JumpPadAdvanced.cs`.

5. **Three parallel "light-reactive" systems.** `DamageInDarkSystem`, `HealInLightSystem`, `DangerLightSystem` are ~60-line files that differ only in "on-light action" vs "on-dark action". Collapse into one component with an enum or two `EffectData` slots.

6. **Runtime scratch mixed with config in *Data classes.** `PlayerData` and `EnemyData` mix `[SerializeField, NonEditable]` runtime state (`isGrounded`, `wallDirection`, `chargeTimer`, `dashCooldownTimer`, ...) with actual designer config (`maxSpeed`, `curves`, `layer masks`). This bloats the inspector and the scene YAML. Split runtime scratch off into non-`[Serializable]` classes.

7. **`PushableObject : PlayerMain`.** A crate should not carry a 12-state player controller. This inflates every crate prefab with a `PlayerData` block and constructs several state instances at runtime. Replace with a small `CubeFollower` (~30 lines) that reads player input and drives a `Rigidbody2D`.

8. **Hardcoded `"Player"` tag** is compared in `SimpleTeleportTrigger`, `AbilityManager`, `DissolveObjectHider`, `SimpleFadeHider`, `DistanceAlphaFade`, `DistanceAnimationControl`. A single `Globals.Player.GameObject` (or a scene-registered `PlayerRef` singleton) would centralise this and avoid `FindGameObjectWithTag` scans.

9. **`Togglable` abstract class with only one gameplay implementation** (`LightProjectorView`). The `togglables` list on `WeightTrigger` mostly overlaps with the `onActivate/onDeactivate` UnityEvents. Delete `Togglable` and wire everything via UnityEvent, or delete the UnityEvents and use only the list — but not both.

10. **Two parallel event channels on `LightDetector`** — `UnityEvent<bool>` (`onChangeState` + `onChangeStateInverse`) fire in parallel with the per-GameObject `LightChangeEvent` on `EventBus`. Same information, twice. Pick one.

11. **Empty script files.** `Assets/_Game/Features/VFX/ModularWallDissolve.cs` and `SpritePhysicsSync.cs` are 0 bytes. Delete.

12. **Debug logging in hot paths.** `SlimeEnemy` logs on every `OnCollisionEnter/Stay/Exit`. `JumpPad` logs on every launch. `EnemyMovementNew` scatters `Debug.DrawRay` in `FixedUpdate`. Wrap in `#if UNITY_EDITOR` or a `debugLog` bool.

13. **Deep prefab hierarchies with redundant Rigidbody2Ds.** Every light prefab has *three* `Rigidbody2D` components across nested children. Verify — likely only one (the outer light platform-mover) needs to be dynamic; the others could be Static or deleted.

14. **`Cube.prefab` vs `MovableCube.prefab`.** `Cube.prefab` has `ManualInputManager` and `Animator` but no `PushableObject` or `Rigidbody2D` — verify whether it's used, or if `MovableCube.prefab` supersedes it.

15. **`Mirror.cs` and `MovedPlatform.cs`.** Both appear only in test scenes / unreferenced. Confirm and delete if legacy.

---

## Session refactor changelog

Concrete changes landed during the audit session that produced this doc. Read this before re-reading the sections above — some of what the sections describe has been simplified.

### Deleted (dead code / redundant abstractions)
- `Assets/_Game/Features/Enemy/EnemyMovement.cs` — 562 lines, never instantiated. `EnemyMovementNew` is the only enemy-movement path.
- `Assets/_Game/Features/JumpPad/JumpPadAdvanced.cs` — 413 lines, no prefab referenced it.
- `Assets/_Game/Features/VFX/ModularWallDissolve.cs`, `SpritePhysicsSync.cs` — empty 0-byte files.
- `Assets/_Game/Features/Health/DamageInDarkSystem.cs`, `HealInLightSystem.cs`, `DangerLightSystem.cs` — three ~60-line files with the same shape. Replaced by `LightReactiveEffect.cs` (one component; `onLight` + `onDark` slots).
- `Assets/_Game/Features/SceneTransition/ConeTrigger.prefab` — 3D `MeshCollider + Rigidbody`, incompatible with the 2D `Trigger` script. Only referenced as a disabled child inside `Spot Light.prefab` (also removed).
- `Assets/_Game/Features/Teleport/TeleportDestination.cs` — merged into `Trigger.cs`. Landing point + offset are now fields on `Trigger`. One script per light-zone GameObject.

### Consolidated
- `DamageInDarkAbility` and `HealInLightAbility` now target the shared `LightReactiveEffect` component rather than their own subclasses. Both abilities can coexist on one player without stacking components.
- `LightDetector` no longer publishes a parallel `UnityEvent<bool>` (`onChangeState` + `onChangeStateInverse`) — those bindings were empty in every prefab that referenced them. All light-reactive systems now go through `EventBus.Subscribe<LightChangeEvent>(gameObject, ...)` only.
- `LightDetector.LightBlockCheck` no longer takes a `Rigidbody2D lightRigidbody` parameter (unused; only present in a commented-out `Debug.Log`). Corresponding `Trigger.lightRigidbody` SerializeField and its `OnValidate` auto-wire are gone.
- `Player.GameObject` (in `LightGame.Globals`) is now the single source for "who is the player". `AbilityManager`, `DistanceAlphaFade`, `DistanceAnimationControl` route through it instead of `FindGameObjectWithTag("Player")`. `SimpleTeleportTrigger.requiredTag` (SerializeField) is gone — it now checks `GetComponent<PlayerMain>()`.
- `Checkpoint.playerTag` and `WeightTrigger.playerTag` SerializeFields removed. Both check `GetComponent<PlayerMain>()` instead — no per-instance tag knob a designer never actually varies.
- `LightDetector.lightBlockingLayers` is now a `[SerializeField] LayerMask` with a tooltip (was `LayerMask.GetMask("LightSource", "Ground")` in code). Player prefab has the mask preset preserving the original two layers.

### Prefab-level simplifications
- Light prefabs — `Rigidbody2D` count dropped: was 3 per instance (`Spot Light > Point`, `Spot Light > TriangleCollider`, and root via `Platform`), now 1 (only the root's, which the `Platform` waypoint mover actually needs). Point Light + Spot Light base prefabs now have 0 Rigidbody2Ds.
- `Cube.prefab` — old teleport-target nested subtree (`Point Light > TriangleCollider > Trigger + TeleportDestination`) removed. Cubes are no longer teleport targets. If the mechanic returns, add a `Trigger` component directly to the cube root.
- `Triagle Colider` typo fixed to `TriangleCollider` in `Spot Light.prefab` and `Point Light.prefab` (variants inherit).
- `WeightPlaform.prefab` → `WeightPlatform.prefab`. Root GameObject + 4 scene-override instance names patched.
- `Jumpad.prefab` — `JumpPad` script + `BoxCollider2D` moved from the `Model` child to the root. Level designer selecting the prefab now sees all gameplay knobs immediately; `Model` is pure visual.

### Scripts hardened
- `Projectile.OnTriggerEnter2D` — coroutine (`IEnumerator` + `WaitForFixedUpdate`) reduced to a straight `void` with immediate `Destroy`.
- `Trigger.cs` — `OnValidate` auto-wires `collider2D` + `targetLightPoint`. `Reset()` auto-sets tag `"Teleport"`. Selected gizmo draws the landing point.
- Tooltips added to `DamageTrigger`, `WeightTrigger`, `Checkpoint`, `Trigger` fields.

---

## Cross-cutting infrastructure — over-abstraction audit

The developer flagged the cross-cutting section as "a little complex" and pointed to the "Respawn is handled elsewhere" transition as hard to follow. Findings from a fresh read after the changes above.

### `ILightable` interface — dead abstraction
- Defined in `Assets/_Game/Core/Interfaces/ILightable.cs`. Declared by 6 classes (`SimpleFadeHider`, `ObjectHider`, `DissolveObjectHider`, `LightReactiveEffect`, `PlayerTeleportSystem`, `LevelChangeTrigger`).
- **Never used polymorphically anywhere** — no method parameter, no field, no cast is typed as `ILightable`.
- Every implementer separately subscribes to `EventBus.Subscribe<LightChangeEvent>` in `OnEnable`. The `OnInLightChange(bool)` method the interface requires isn't the subscription callback — it's an extra public method most implementers hand-write to satisfy the interface, then never call. `LevelChangeTrigger.OnInLightChange(bool)` has the comment "kept for ILightable interface compatibility" and does nothing.
- **Recommendation:** delete `ILightable`. Remove the interface declaration from all 6 classes. Delete stub `OnInLightChange(bool)` methods that only exist to satisfy the interface. The subscription pattern (`EventBus.Subscribe<LightChangeEvent>`) is the real contract.

### `Togglable` abstract class — one-implementation abstraction
- Defined in `Assets/_Game/Core/ITogglable.cs`. Exactly one concrete subclass: `LightProjectorView`.
- `WeightTrigger` has both `List<Togglable> togglables` (drag-drop from inspector) AND `UnityEvent onActivate/onDeactivate`. Two parallel channels do the same job.
- **Recommendation:** delete `Togglable` abstract, expose `Enable()`/`Disable()` as plain public methods on `LightProjectorView`. Remove `togglables` list from `WeightTrigger` — designers can wire the same call through `UnityEvent onActivate` (right-click → `LightProjectorView.Enable`). One channel, no abstraction ladder.

### `MonoBehaviourWithData<T>` — thin but honest
- 15-line generic base: one `[SerializeField] T data` + a `Data` property.
- Used by `LightReactiveEffect` (indirectly via effects), `PlayerTeleportSystem`. Marginal savings but no misdirection — keeping is fine.

### `MonoBehaviourEffect<T>` — pulls its weight
- Handles tick timing, rate, duration, IsInfinity for the two over-time effects. Two subclasses each ~85 lines; the base carries the shared timer. Fits.

### Death → Respawn — the "handled elsewhere" transition
The GDD called this out and the developer asked to trace it. The path today:

1. `HealthSystem.TakeDamage` → HP ≤ 0 → calls virtual `Die()`.
2. `PlayerHealthSystem.Die()` (override) — zeroes velocity, publishes `PlayerDiedEvent` globally.
3. `PlayerDeathView` — a script in `Features/UI/Views/` — subscribes to `PlayerDiedEvent`. In `HandleDeathCoroutine` it: fades lights to black → waits → **calls `Checkpoint.Active.RespawnPlayer(player)`** → fades back.
4. `Checkpoint.RespawnPlayer` teleports + resets velocity + full-heals.

The concern is real: **the respawn call sits inside a class named `PlayerDeathView`, which is under `UI/Views/`**. A view file should not own gameplay state transitions. Debugging "why did the player respawn at the wrong place?" starts in Health, jumps to a UI file, jumps to Checkpoint. Three files, two feature folders, one architectural layer violation.

**Recommendation (not applied yet — architectural call):**
- Split `PlayerDeathView` into:
  - `PlayerDeathVisualView` (in `Features/UI/Views/`) — subscribes to `PlayerDiedEvent`, fades the screen. Nothing else.
  - `PlayerRespawnHandler` (in `Features/Checkpoint/` or `Globals/`) — subscribes to `PlayerDiedEvent`, waits the delay, calls `Checkpoint.Active.RespawnPlayer(player)`.
- Both live on the Player prefab (or the Respawn handler on a scene root). Each has one responsibility.

Alternative: give `Checkpoint` itself a static `PlayerDiedEvent` subscription (a global "respawn coordinator" attached to whatever checkpoint is currently `Active`). Slightly more elegant, ties the respawn logic to the Checkpoint feature where it belongs.

### `Trigger` component — the still-heavy one
Renamed / repurposed several times. It now holds: light detection, level-change scene load config, teleport landing config. Three headers, three responsibilities. This is defensible because they always coexist on the same light-zone GameObject and share the same `Collider2D`/`Rigidbody2D` requirement. But if the "level-change trigger" mechanic evolves (say, becoming a boss room seal or a save point), consider extracting `TriggerLevelChangeOnEnter` as a sibling component that reads `Trigger`'s data. For now, ✅ Fits.

### `Trigger` naming
`Trigger.cs` in `Features/SceneTransition/` is now a general-purpose light-zone script — not really a "scene transition" component anymore. Consider renaming to `LightSource` and moving to `Features/Light/`. Would also move `LevelChangeTrigger` (the player-side listener) into that folder.

### `EventBus` — the right amount of abstraction
- Two forms (global and per-GameObject) are both load-bearing. Feature scripts use the per-GameObject form to route damage/heal/light events to the specific target instance, and the global form for scene-wide broadcasts (`PlayerDiedEvent`, `RequestLevelChangeEvent`).
- No fewer patterns work here; more patterns would be over-engineering. Keep as-is.

### Summary of over-abstraction to eliminate
| Abstraction | State | Recommendation |
|-------------|-------|----------------|
| `ILightable` | ✅ Deleted this session | Interface removed; `LightChangeEvent` class now lives alone in `Assets/_Game/Core/Interfaces/LightChangeEvent.cs`. All 6 implementers reduced to `MonoBehaviour`; `LevelChangeTrigger.OnInLightChange(bool)` stub deleted. |
| `Togglable` abstract | Kept (developer preference) | The developer prefers the drag-list UX over UnityEvents. Only `LightProjectorView` implements it today. |
| `WeightTrigger.onActivate/onDeactivate` UnityEvent | ✅ Replaced with C# `Action` events this session | `WeightTriggerView` now subscribes via `+= / -=` to `OnActivated / OnDeactivated`. Matches the JumpPad→JumpPadView pattern. |
| `LightDetector.UnityEvent<bool> onChangeState` | ✅ Already removed this session | — |
| `Trigger.lightRigidbody` param | ✅ Already removed this session | — |
| Three `*InLight/InDark/DangerLight` systems | ✅ Already merged into `LightReactiveEffect` | — |
| Six `"Player"` tag comparisons | ✅ Consolidated to `Player.GameObject` this session | — |
| `Trigger` in `SceneTransition/` folder | ✅ Renamed + moved this session | Now `LightSource` in `Features/Light/LightSource.cs`. 3 callers updated. |
| Death → respawn crossing UI layer | Not addressed | Split `PlayerDeathView` per proposal above (still pending). |

---

## Model / ViewModel / View — should we adopt it?

An independent audit read both Godot reference projects (`deep-tripping` and `card-game-nakama`) and cross-checked against LightGame.

### What the reference projects actually do
- **`deep-tripping` has NO ViewModel layer.** Model + View only. `BattleCharacter` is a `Resource` with `property_changed`, `CharacterView` binds via `set_data()` and re-renders.
- **`card-game-nakama` uses full Model/VM/View — but only in 2 places**: `Card`/`CardVM`/`CardView` and `Player`/`PlayerVM`/`EnemyInfoView`. Both are network-synced entities with server-authoritative state + client prediction. The VM tier is where "predicted" fields (like `is_dragging`, an optimistically-set `playable`) live so the view has one uniform surface.
- **The VM tier is a networking / reconciliation solution**, not a general architectural pattern. The base classes (`Model`, `ViewModel`, `ViewBinder`) live in `client/features/core/`.

### Where LightGame ALREADY does Model→View (implicitly)
- `JumpPad` + `JumpPadView` — Model exposes typed `Action` events (`OnPlayerLand`, `OnPlayerBounce`, `OnCooldownComplete`), View subscribes in `OnEnable` and drives tweens/particles/sprite color.
- `HealthSystem` + `HealthView` — same shape (`OnHeal`, `OnTakeDamage`, `OnNearDeathStateChanged`).
- `WeightTrigger` + `WeightTriggerView` — same shape (as of this session, now using `Action` events).
- `SlimeEnemy` + `SlimeView`, `LightProjector` + `LightProjectorView`, `ProjectileSpawner` + `ProjectileSpawnerView` — all the same.

**Every `*View.cs` under `Features/UI/Views/` follows this convention.** LightGame is already doing Model→View — just with typed `Action`s instead of a single string-based `property_changed(name, value)` signal. **For a single-player game, typed `Action`s are strictly better** (compile-time enforcement, no boxing, no stringly-typed property names).

### Where the split is missing (worth fixing — but this is NOT a MVVM problem)
- `PlayerData` (~380 lines) — mixes designer-config knobs with `[SerializeField, NonEditable]` runtime scratch (`isGrounded`, `currentSlopeAngle`, `dashCooldownTimer`, `contacts`). This is the "Model + Runtime" split that `mechanics.md` line 83 already calls out.
- `HealthView` has private lists (`_pendingHealSectors`, `_pendingDamageSectors`) that are view-only state. That could live in a tiny helper class the view holds — but calling it a "ViewModel" and giving it a base class is overkill.

### Recommendation (from the audit): **Skip formal MVVM; delete the empty base classes**
- `Assets/_Game/Core/Model.cs`, `ViewModel.cs`, `View.cs` — created earlier during the Godot-style refactor but almost nothing uses them. They import the string-property-name idiom from card-game-nakama's networking pattern into a single-player game where it's a downgrade.
- Keeping them tempts future code to force well-designed pairs (`JumpPad` + `JumpPadView`) into a worse shape (string-based `PropertyChanged("Cooldown", 0.5f)` instead of typed `OnCooldownComplete` events).
- **Delete `Model.cs`, `ViewModel.cs`, `View.cs`.** Continue using typed `Action` events — that's the established pattern and it's better than the string-based one.

### Focused wins worth pursuing (excluding `PlayerData` — black box)
Ranked by impact:

1. **Extract `HealthView`'s pending-sector bookkeeping into a plain helper class** the view holds. Not a ViewModel base class — just a `HealthPendingSectors` C# class. Removes the LINQ soup from the view.
2. **Port `ViewBinder.sync/place` as a static C# utility.** The one piece of card-game-nakama that has no equivalent in LightGame and is genuinely useful for any dynamic UI list (ability grid, save slots, level select). One static class, no base-class dependency. Optional — only worth doing when you build such a list.

**Skipped:** the "Split `PlayerData` into `PlayerConfig` + `PlayerRuntime`" recommendation, because `PlayerData` is third-party open-source code that we treat as a black box (see cross-cutting section).

Neither of the two wins above requires MVVM base classes.

### MVVM base classes — deleted this session
- `Assets/_Game/Core/Model.cs`, `ViewModel.cs`, `View.cs` — created earlier during the Godot-style refactor, unused (all `Model`/`ViewModel`/`View` references in the project pointed to the third-party `Assets/Plugins/MVVM/` library, not these). Deleted. Existing Model→View pairs (`JumpPad`+`JumpPadView`, `HealthSystem`+`HealthView`, `WeightTrigger`+`WeightTriggerView`, `SlimeEnemy`+`SlimeView`, etc.) continue using typed C# `Action` events — the established pattern.

### Third-party MVVM plugin — deleted this session
- `Assets/Plugins/MVVM/` (234 files, 1.4 MB, 3 asmdefs) removed. It was in use by 6 files in `Features/Selectable/` but nothing else — 0 prefab dependencies, 0 scene dependencies, 0 scriptable-object dependencies across the entire project (verified via `AssetDatabase.GetDependencies`).
- The 6 Selectable binders that referenced the plugin (`ButtonStateBinder`, `SelectableStateBinder`, `ToggleStateBinder`, `IsAnyTogglesOnBinder`) were also dead (no prefab used them) — deleted.
- `ISelectable` rewritten: `ReactiveProperty<SelectionState>` → `event Action<SelectionState> SelectionStateChanged` + `SelectionState CurrentState` getter. All 8 `SelectableStateXxx` components (Color, Scale, Sprite, Sound, TextMaterial, Sorting, UnityEvent, SeparateUnityEvent) rewritten to subscribe via `+=/-=` in `OnEnable`/`OnDisable` instead of `Subscribe(...).AddTo(this)`. `ButtonExtended` and `ToggleExtended` fire the event from `DoStateTransition` (skipping when unchanged).
- `[SortingLayer]` attribute copied from the plugin to `Assets/_Game/Core/SortingLayerAttribute.cs` (~10 lines).
- `com.cysharp.r3` package removed from `Packages/manifest.json` (unused after MVVM plugin was gone). `R3.Unity` and `MVVM` references removed from `LightGame.asmdef`.
- Dead `using R3;` imports removed from `CameraView.cs` and `PlayerHealthSystem.cs`.
- `ViewBinder.sync` ported from card-game-nakama (`client/features/core/view_binder.gd`) to `Assets/_Game/Core/ViewBinder.cs` — ~90 lines. Reconciles a list of models to child views in a container by ID. Use for future dynamic UI lists (level select, save slots, ability picker).
- **Result:** menu UI infrastructure is now ~800 lines of feature code + 1 static utility, down from ~35,000 lines (234 files) of plugin. Both `MainMenu.prefab` and `Button.prefab` instantiate cleanly with 0 missing script references.

### Additional unused plugins deleted this session

Confirmed 0 refs across all 111 prefabs, 42 scenes, and 29 ScriptableObjects before deleting.

**Local folders (`Assets/`):**
- `Assets/Plugins/ModularSettings/` — settings-menu template plugin (volume sliders, MSAA dropdown, etc.), unused
- `Assets/Plugins/Neonalig/` — SceneToolbar editor tool, unused
- `Assets/com.unity.uiextensions/` — Unity UI Extensions widgets, unused

**Package manifest entries:**
- `com.svermeulen.extenject` — Zenject DI framework
- `com.unity.visualscripting` — Bolt/Visual Scripting
- `com.unity.timeline` — Cinematic timeline editor
- `com.unity.ai.navigation` — NavMesh (2D platformer doesn't use it)
- `com.unity.multiplayer.center` — Multiplayer SDK entry point
- `com.github-glitchenzo.nugetforunity` — NuGet package manager
- `com.coffee.ui-effect` — UI shader effects

Asset count dropped from ~17,600 to 13,911 (≈3,700 assets, ~30 MB freed). Compiled clean, MainMenu / Button / HealthSector prefabs instantiate with 0 missing script references.

**Pre-existing issue not caused by these deletions:** `Assets/_Game/Features/UI/Prefabs/Views/Hint.prefab` has 3 dangling script references from the original `Assets/Light and controller/` folder that was renamed during the Godot-style refactor months ago (`HintView.cs` etc.). Fix is out of scope here — either re-create the missing scripts or remove the dangling MonoBehaviour slots.

### `LightDetector.LightBlockCheck` bug — fixed this session

**Symptom:** On FirstDesignedLevel, interacting with a weight platform enabled a light, but the hide-platform in that light did NOT switch to the `Hidden` layer / let the player through.

**Cause 1 (raycast mask):** During the earlier UnityEvent-removal pass on `LightDetector`, the hardcoded raycast mask `LayerMask.GetMask("LightSource", "Ground")` was replaced with a `[SerializeField] LayerMask lightBlockingLayers = ~0`. With "all layers" enabled, the raycast from the platform toward the light hit the platform's OWN collider first (distance 0, tag `HideObject`), and since that's the first non-PassLight hit, `LightBlockCheck` returned false → the light never registered on the detector.

**Cause 2 (Start vs OnEnable + GetContacts vs Overlap):** `LightSource.Start()` ran the initial-overlap sweep only once per component lifetime. Lights that were disabled at scene start (via `WeightTrigger.Start()` with `startActivated=false`) and re-enabled later via toggle never re-swept — and Unity does NOT reliably fire `OnTriggerEnter2D` when a trigger collider is enabled while already overlapping another collider. Additionally, the sweep used `Collider2D.GetContacts()`, which only returns pairs where at least one side has a `Rigidbody2D`. `HidePlatform` is static (no Rigidbody), so it was invisible to `GetContacts`. `Collider2D.Overlap()` works for static colliders too.

**Cause 3 (missing OnDisable cleanup):** Because disabled MonoBehaviours don't receive `OnTriggerExit`, turning a light OFF left stale entries in every `LightDetector.lightSprings` — the platform would stay hidden.

**Fix:**
- `Assets/_Game/Features/Light/LightDetector.cs` — reverted to the old hardcoded mask, cached as a lazily-initialized static (can't call `LayerMask.GetMask` from a field initializer in Unity — throws `UnityException` before Awake).
- `Assets/_Game/Features/Light/LightSource.cs` — replaced `Start()` with `OnEnable()` so the initial-overlap sweep runs on every enable; swapped `GetContacts` for `Overlap` (works for static colliders); added `OnDisable()` that removes the light from all detectors it had registered with (tracked via a private `HashSet<LightDetector>`).

**Lesson for future refactors:** Anything that changes a physics-query mask can silently break a mechanic in a way that compiles clean and produces zero runtime errors. And any component that participates in trigger-based state via `Start()` needs to be re-entrant (`OnEnable` / `OnDisable`) if it can be toggled. Verify light-reactive mechanics on `FirstDesignedLevel` after touching `LightDetector` / `LightSource` / anything with a `ContactFilter2D`.
