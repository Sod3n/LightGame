# Troubleshooting Guide - Slime Enemy

## Common Issues & Solutions

### 1. Enemy Stuck in Corner / Rotating Endlessly

**Problem**: Enemy gets stuck in corners and spins continuously.

**Solution**: 
- The system now includes corner detection that prevents rotation when multiple surfaces are detected
- Rotation only occurs on gentle slopes (< 45°)
- Enemy resets to upright position when in corners

**Additional fixes if still occurring**:
- Increase the corner detection distance in code (currently 0.6f)
- Disable wall walking: Set `Can Walk On Walls` to false in EnemyData
- Use simpler collider shapes (circle instead of capsule)
- Check Rigidbody2D settings:
  - Collision Detection: Continuous
  - Interpolate: Interpolate (for smoother movement)

### 2. Enemy Doesn't Move

**Causes & Solutions**:
- **Not grounded**: Place enemy on a surface with correct layer
- **No input**: Check EnemyInputManager is attached and initialized
- **Frozen**: Verify Rigidbody2D > Body Type is "Dynamic"
- **Wrong layers**: Ensure Ground Layer Mask includes the floor layer
- **Zero speed**: Check Movement > Wander Speed is > 0

### 3. Enemy Falls Through Floor

**Solutions**:
- Set Rigidbody2D > Collision Detection to "Continuous"
- Increase collider size slightly
- Check layer collision matrix (Edit > Project Settings > Physics 2D)
- Ensure floor has a collider

### 4. Enemy Doesn't Detect Player

**Solutions**:
- Verify player is on correct layer
- Set AI > Player Layer Mask to include player layer
- Increase AI > Detection Range
- Check player has a collider (for OverlapCircle detection)

### 5. Projectiles Don't Spawn

**Solutions**:
- Assign Projectile Prefab in Attack settings
- Check projectile prefab has required components:
  - Rigidbody2D
  - Collider
  - EnemyProjectile script (or Projectile script)
- Look for errors in Console
- Verify Attack Cooldown has expired

### 6. Enemy Moves Too Fast/Slow

**Quick Adjustments**:
- Wander Speed: 1-3 (slow wandering)
- Aggro Speed: 3-5 (chase speed)
- If still too fast, reduce Rigidbody2D > Mass

### 7. Enemy Jitters or Shakes

**Solutions**:
- Set Rigidbody2D > Interpolate to "Interpolate"
- Reduce rotation lerp speed (currently 5f in AlignToSurface)
- Increase Rigidbody2D > Linear Drag to 0.5-1
- Check for conflicting forces (multiple colliders, etc.)

### 8. Wall Walking Not Working

**Current Behavior**: 
Wall/ceiling walking is simplified - enemy now only rotates on gentle slopes (< 45°) to prevent corner issues.

**If you need full wall walking**:
1. Modify `AlignToSurface()` in both states
2. Remove the 45° angle check
3. Add more sophisticated corner handling
4. Consider using raycasts in all directions

**Alternative**: Disable wall walking entirely:
- Set `Can Walk On Walls` to false
- Set Rigidbody2D > Freeze Rotation to true

### 9. Attack State Issues

**Enemy doesn't attack**:
- Check Attack Cooldown Timer (visible in inspector during play)
- Verify player is within Attack Range
- Ensure Attack > Charge Time isn't too long

**Projectiles fire in wrong direction**:
- Check projectile prefab rotation
- Verify EnemyProjectile.Initialize is being called
- First projectile should aim at player

### 10. Performance Issues

**If game slows down**:
- Limit number of enemies (< 10 recommended)
- Reduce projectile count (Attack > Projectile Count)
- Increase projectile lifetime (destroys faster)
- Use object pooling for projectiles
- Reduce detection range

## Debug Tips

### Enable Debug Visualization
Select enemy in hierarchy during play mode to see:
- Yellow sphere: Detection range
- Red sphere: Attack range
- Green line: Ground check ray

### Check State in Inspector
During play mode, expand the enemy's state machine to see:
- Current state
- Timer values
- Input values

### Console Warnings
Watch for these warnings:
- "Projectile prefab not assigned!" - Need to set prefab
- Missing component errors - Add required components

### Test in Isolation
1. Create simple test scene
2. Add flat ground
3. Add one enemy
4. Add player
5. Test each behavior separately

## Recommended Settings for Stable Behavior

### For Simple Ground Enemy (No Wall Walking)
```
Movement:
- Wander Speed: 2
- Aggro Speed: 3
- Gravity Scale: 1
- Can Walk On Walls: ✗
- Can Walk On Ceiling: ✗

Rigidbody2D:
- Freeze Rotation: ✓
- Collision Detection: Continuous
- Interpolate: Interpolate
```

### For Wall-Walking Enemy
```
Movement:
- Wander Speed: 2
- Aggro Speed: 3
- Gravity Scale: 0.5
- Can Walk On Walls: ✓
- Can Walk On Ceiling: ✓

Rigidbody2D:
- Freeze Rotation: ✗
- Collision Detection: Continuous
- Interpolate: Interpolate
- Linear Drag: 0.5
```

## Still Having Issues?

1. Check Unity Console for errors
2. Verify all components are attached
3. Test with default settings first
4. Simplify setup (disable wall walking, reduce projectile count)
5. Check layer collision matrix
6. Ensure all required layers exist
