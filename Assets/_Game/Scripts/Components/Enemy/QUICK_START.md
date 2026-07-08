# Quick Start Guide - Slime Enemy

## Minimal Setup (5 minutes)

### Step 1: Create Enemy Data
1. Right-click in Project → `Create > Enemy > Enemy Data`
2. Name it "SlimeData"
3. Set these minimum values:
   - Physics > Ground Layer Mask: Select your ground layers
   - Physics > Player Layer Mask: Select player layer
   - Attack > Projectile Prefab: (create in step 2)

### Step 2: Create Projectile Prefab
1. Create empty GameObject
2. Add Sprite Renderer (any sprite)
3. Add Rigidbody2D (Dynamic)
4. Add CircleCollider2D
5. Add `EnemyProjectile` component
6. Save as prefab, drag to SlimeData's Projectile Prefab field

### Step 3: Create Enemy
1. Create GameObject in scene
2. Add Sprite Renderer (slime sprite)
3. Add Rigidbody2D:
   - Dynamic
   - Gravity Scale: 0 (for wall walking) or 1 (normal)
   - Uncheck Freeze Rotation
4. Add CapsuleCollider2D
5. Add `SlimeEnemy` component
6. Drag SlimeData to Enemy Data field

### Step 4: Test
- Place enemy near ground
- Add player to scene
- Play!

## Default Behavior
- Wanders randomly
- Detects player within 10 units
- Attacks when player within 8 units
- Fires 8 projectiles in circle (1 aimed at player)
- 2 second cooldown between attacks

## Quick Tweaks

**Make it faster:**
- SlimeData > Movement > Aggro Speed: 5

**More aggressive:**
- SlimeData > AI > Detection Range: 15
- SlimeData > Attack > Attack Cooldown: 1

**More projectiles:**
- SlimeData > Attack > Projectile Count: 12

**Disable wall walking:**
- SlimeData > Movement > Can Walk On Walls: Uncheck
- Enemy Rigidbody2D > Freeze Rotation: Check

Done! See README.md for full documentation.
