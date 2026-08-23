# Slime Enemy System

A complete enemy AI system based on the player controller architecture, featuring wall/ceiling walking and multi-directional projectile attacks.

## Features

- **Wandering Behavior**: Slime wanders slowly in random directions
- **Wall/Ceiling Walking**: Can stick to and walk on any surface (walls, ceilings, floors)
- **Player Detection**: Automatically detects and chases the player within range
- **Projectile Attacks**: Fires projectiles in all directions with cannon-like physics
  - One projectile always aims at the player
  - Configurable number of projectiles (default: 8 in a circle)
  - Strong gravity effect for arc trajectory

## Setup Instructions

### 1. Create Enemy Data Asset

1. Right-click in Project window
2. Select `Create > Enemy > Enemy Data`
3. Name it (e.g., "SlimeEnemyData")
4. Configure the settings:

#### Physics Settings
- **Ground Layer Mask**: Set to your ground/wall layers
- **Wall Layer Mask**: Set to wall layers
- **Player Layer Mask**: Set to player layer for detection

#### Movement Settings
- **Wander Speed**: 2 (slow wandering)
- **Aggro Speed**: 3 (faster when chasing)
- **Gravity Scale**: 1 (or 0 for no gravity)
- **Can Walk On Walls**: ✓ (enabled)
- **Can Walk On Ceiling**: ✓ (enabled)

#### AI Settings
- **Detection Range**: 10 (units to detect player)
- **Attack Range**: 8 (units to start attacking)
- **Wander Direction Change Interval**: 3 (seconds between direction changes)

#### Attack Settings
- **Projectile Prefab**: Assign your projectile prefab (see step 2)
- **Projectile Count**: 8 (number of projectiles per attack)
- **Projectile Speed**: 5
- **Projectile Speed Rotation**: 180 (rotation speed)
- **Attack Cooldown**: 2 (seconds between attacks)
- **Charge Time**: 1 (seconds to charge before firing)

### 2. Create Projectile Prefab

#### Option A: Using EnemyProjectile (Recommended for cannon physics)
1. Create a new GameObject
2. Add a Sprite Renderer (assign your projectile sprite)
3. Add a Rigidbody2D:
   - Body Type: Dynamic
   - Gravity Scale: 2 (will be overridden by script)
   - Collision Detection: Continuous
4. Add a CircleCollider2D or other collider
5. Add the `EnemyProjectile` component
   - Set Gravity Scale: 2 (for strong arc)
   - Set Lifetime: 5 seconds
   - Set Rotation Speed: 180
6. Save as prefab

#### Option B: Using existing Projectile component
1. Use your existing projectile prefab
2. Make sure it has Rigidbody2D and a collider

### 3. Create Slime Enemy GameObject

1. Create a new GameObject in your scene
2. Name it "SlimeEnemy"
3. Add required components:
   - **Sprite Renderer**: Assign slime sprite
   - **Rigidbody2D**:
     - Body Type: Dynamic
     - Gravity Scale: 1 (or 0 if you want it to stick to surfaces)
     - Freeze Rotation: Unchecked (needs to rotate for wall walking)
     - Collision Detection: Continuous
   - **CapsuleCollider2D**: Adjust size to fit sprite
   - **SlimeEnemy** script (from Enemy folder)

4. In the SlimeEnemy component:
   - Assign your EnemyData asset to the "Enemy Data" field

5. Optional: Add an Animator if you have animations

### 4. Layer Setup

Make sure your layers are configured:
- Player should be on a "Player" layer
- Ground/walls should be on appropriate layers
- Set up layer collision matrix in Edit > Project Settings > Physics 2D

## How It Works

### State Machine
The enemy uses a state machine with three states:

1. **Wander State**: 
   - Moves slowly in random directions
   - Changes direction every few seconds
   - Aligns to surfaces (walls/ceiling)

2. **Aggro State**:
   - Activated when player enters detection range
   - Chases player at higher speed
   - Maintains surface alignment

3. **Attack State**:
   - Activated when player is in attack range
   - Stops movement
   - Charges for configured time
   - Fires projectiles in all directions
   - One projectile always aims at player
   - Returns to Aggro or Wander state after attack

### Surface Walking
The slime automatically:
- Detects surfaces using raycasts
- Rotates to align with surface normal
- Walks along walls and ceilings
- Transitions smoothly between surfaces

### Projectile System
When attacking:
- Spawns configured number of projectiles
- Distributes them evenly in a circle (360° / count)
- First projectile always aims at player position
- Each projectile has cannon-like physics with gravity
- Projectiles auto-destroy on collision or after lifetime

## Customization

### Adjust AI Behavior
Edit the `EnemyInputManager.cs`:
- Modify `HandleWanderState()` for different wander patterns
- Modify `HandleAggroState()` for chase behavior
- Adjust detection logic in `DetectPlayer()`

### Change Movement
Edit state classes in `States/` folder:
- `SlimeWanderState.cs`: Wander movement
- `SlimeAggroState.cs`: Chase movement
- `SlimeAttackState.cs`: Attack behavior

### Modify Projectile Pattern
Edit `SlimeAttackState.cs` > `FireProjectiles()`:
- Change angle calculation for different patterns
- Add spread/randomness
- Implement different firing modes

## Debugging

The enemy draws debug gizmos when selected:
- **Yellow sphere**: Detection range
- **Red sphere**: Attack range
- **Green line**: Ground check ray

Enable Gizmos in Scene view to see these.

## Performance Tips

1. Limit number of enemies in scene
2. Use object pooling for projectiles
3. Adjust detection range based on needs
4. Consider using NavMesh for complex pathfinding (optional)

## Troubleshooting

**Enemy doesn't move:**
- Check Rigidbody2D is set to Dynamic
- Verify Ground Layer Mask is set correctly
- Ensure enemy is touching a surface

**Enemy doesn't detect player:**
- Check Player Layer Mask in Enemy Data
- Verify player is on correct layer
- Increase detection range

**Projectiles don't spawn:**
- Assign Projectile Prefab in Enemy Data
- Check projectile prefab has required components
- Look for errors in console

**Enemy falls through floor:**
- Check collision layers
- Verify Ground Layer Mask includes floor layer
- Adjust collider sizes

## Architecture

Based on the player controller system:
- Uses state machine pattern
- Modular state classes
- ScriptableObject for data
- Input manager for AI decisions
- Physics-based movement
