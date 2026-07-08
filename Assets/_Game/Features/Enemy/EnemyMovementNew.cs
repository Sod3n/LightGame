using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy
{
    /// <summary>
    /// Handles all movement logic for enemies including wall/ceiling walking
    /// </summary>
    public class EnemyMovementNew
    {
        // Physics constants - adjust as needed
        private const float CIRCLE_CHECK_RADIUS_MULTIPLIER = 2f; // Multiplier for enemy size
        private const float RAYCAST_ALIGNMENT_DISTANCE = 2f;
        private const float ROTATION_SPEED = 8f;
        private const float GRAVITY_FORCE = 20f;
        
        private readonly SlimeEnemy enemy;
        private readonly EnemyData enemyData;
        private readonly Rigidbody2D rigidbody2D;
        
        // Movement state
        private Vector2 currentMoveDirection = Vector2.right;
        private Vector2 currentGroundDirection = Vector2.down;
        
        // Ground tracking
        private bool hasGround = false;
        private Collider2D currentGroundCollider = null;
        private Vector2 groundNormal = Vector2.up;
        
        public EnemyMovementNew(SlimeEnemy enemy, EnemyData enemyData, Rigidbody2D rigidbody2D)
        {
            this.enemy = enemy;
            this.enemyData = enemyData;
            this.rigidbody2D = rigidbody2D;
        }
        
        #region Main Movement Functions
        
        /// <summary>
        /// Main movement function called from FixedUpdate
        /// </summary>
        public void Move(float speed)
        {
            if (enemyData.Movement.CanWalkOnWalls)
            {
                MoveOnSurface(speed);
            }
            else
            {
                MoveGroundOnly(speed);
            }
        }
        
        /// <summary>
        /// Simple ground-only movement
        /// </summary>
        private void MoveGroundOnly(float speed)
        {
            if (!enemyData.Physics.IsGrounded) return;
            
            rigidbody2D.linearVelocity = new Vector2(
                currentMoveDirection.x * speed, 
                rigidbody2D.linearVelocity.y
            );
            
            // Keep upright
            enemy.transform.rotation = Quaternion.Lerp(
                enemy.transform.rotation, 
                Quaternion.identity, 
                Time.fixedDeltaTime * ROTATION_SPEED
            );
        }
        
        /// <summary>
        /// Advanced surface walking (walls/ceiling)
        /// </summary>
        private void MoveOnSurface(float speed)
        {
            Vector2 position = enemy.transform.position;
            
            // Step 1: Check if we still have ground contact
            if (hasGround)
            {
                AlignToCurrentGround(position);
            }
            
            // Step 2: If no ground, check 4 circles and pick best floor
            if (!hasGround)
            {
                CheckCirclesAndPickFloor(position);
            }
            
            // Step 3: If still no ground, apply gravity
            if (!hasGround)
            {
                ApplyGravity();
            }
            else
            {
                // Apply movement along the surface
                ApplyMovementAlongSurface(speed);
            }
        }
        
        #endregion
        
        #region Collision Events
        
        /// <summary>
        /// Called when enemy enters a collision
        /// </summary>
        public void OnCollisionEnter(Collision2D collision)
        {
            // Check if this is a valid ground surface
            if (IsValidGround(collision))
            {
                SetNewGround(collision);
                CalculateNewMoveDirection(collision);
            }
        }
        
        /// <summary>
        /// Called while enemy stays in collision
        /// </summary>
        public void OnCollisionStay(Collision2D collision)
        {
            // TODO: Handle ongoing collision
            // Example: Track current surface contact
        }
        
        /// <summary>
        /// Called when enemy exits a collision
        /// </summary>
        public void OnCollisionExit(Collision2D collision)
        {
            // Check if we lost our current ground
            if (collision.collider == currentGroundCollider)
            {
                ClearGround();
            }
        }
        
        #endregion
        
        #region Circle Detection & Ground Checking
        
        /// <summary>
        /// Check for surfaces to the left or right (excluding current surface)
        /// If found, move to that direction. If not, rotate back.
        /// </summary>
        private void CheckCirclesAndPickFloor(Vector2 position)
        {
            // Get enemy size from collider
            float enemyRadius = enemy.GetComponent<Collider2D>()?.bounds.extents.magnitude ?? 0.5f;
            float checkRadius = enemyRadius * CIRCLE_CHECK_RADIUS_MULTIPLIER;
            
            // Find all nearby surfaces
            Collider2D[] nearbySurfaces = Physics2D.OverlapCircleAll(
                position, checkRadius, enemyData.Physics.GroundLayerMask
            );
            
            // Filter out current ground and find best surface
            Collider2D bestSurface = null;
            float bestScore = float.MinValue;
            
            foreach (var surface in nearbySurfaces)
            {
                // Skip current ground
                if (surface == currentGroundCollider) continue;
                
                Vector2 toSurface = (Vector2)surface.transform.position - position;
                
                // Check if surface is to left or right of movement
                float horizontalAlignment = Vector2.Dot(toSurface.normalized, currentMoveDirection);
                
                if (Mathf.Abs(horizontalAlignment) > 0.3f && horizontalAlignment > bestScore)
                {
                    bestScore = horizontalAlignment;
                    bestSurface = surface;
                }
            }
            
            if (bestSurface != null)
            {
                // Found a surface - move toward it
                Vector2 toSurface = bestSurface.ClosestPoint(position) - position;
                currentGroundDirection = toSurface.normalized;
                hasGround = true;
            }
            else
            {
                // No surface found - rotate back
                FlipDirection();
            }
        }
        
        /// <summary>
        /// Raycast to current ground direction to align move direction
        /// </summary>
        private void AlignToCurrentGround(Vector2 position)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                position, 
                currentGroundDirection, 
                RAYCAST_ALIGNMENT_DISTANCE, 
                enemyData.Physics.GroundLayerMask
            );
            
            if (hit.collider != null)
            {
                // Update ground normal
                groundNormal = hit.normal;
                
                // Adjust move direction to be tangent to surface
                currentMoveDirection = Vector2.Perpendicular(groundNormal);
                
                // Ensure we're moving in the correct direction (not backwards)
                if (Vector2.Dot(currentMoveDirection, enemy.transform.right) < 0)
                {
                    currentMoveDirection = -currentMoveDirection;
                }
            }
            else
            {
                // Lost ground contact
                hasGround = false;
            }
        }
        
        /// <summary>
        /// Check if collision is a valid ground surface
        /// </summary>
        private bool IsValidGround(Collision2D collision)
        {
            // TODO: Check if collision is on ground layer
            // TODO: Check contact normal angle if needed
            return ((1 << collision.gameObject.layer) & enemyData.Physics.GroundLayerMask) != 0;
        }
        
        #endregion
        
        #region Ground State Management
        
        /// <summary>
        /// Set new ground from collision
        /// </summary>
        private void SetNewGround(Collision2D collision)
        {
            hasGround = true;
            currentGroundCollider = collision.collider;
            
            // Get contact normal from first contact point
            if (collision.contactCount > 0)
            {
                groundNormal = collision.GetContact(0).normal;
                currentGroundDirection = -groundNormal;
            }
        }
        
        /// <summary>
        /// Calculate new move direction based on collision
        /// </summary>
        private void CalculateNewMoveDirection(Collision2D collision)
        {
            if (collision.contactCount == 0) return;
            
            Vector2 contactNormal = collision.GetContact(0).normal;
            
            // Calculate tangent (perpendicular to normal)
            Vector2 tangent = Vector2.Perpendicular(contactNormal);
            
            // Choose tangent direction that matches our current movement
            if (Vector2.Dot(tangent, currentMoveDirection) < 0)
            {
                tangent = -tangent;
            }
            
            currentMoveDirection = tangent.normalized;
        }
        
        /// <summary>
        /// Clear ground state when leaving surface
        /// </summary>
        private void ClearGround()
        {
            hasGround = false;
            currentGroundCollider = null;
            currentGroundDirection = Vector2.down;
        }
        
        #endregion
        
        #region Movement Application
        
        /// <summary>
        /// Apply movement velocity along the current surface
        /// </summary>
        private void ApplyMovementAlongSurface(float speed)
        {
            // Calculate velocity along surface
            Vector2 velocity = currentMoveDirection * speed;
            rigidbody2D.linearVelocity = velocity;
            
            // Rotate to align with ground normal
            float targetAngle = Mathf.Atan2(groundNormal.y, groundNormal.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            
            enemy.transform.rotation = Quaternion.Lerp(
                enemy.transform.rotation,
                targetRotation,
                Time.fixedDeltaTime * ROTATION_SPEED
            );
        }
        
        /// <summary>
        /// Apply gravity when no ground is present
        /// </summary>
        private void ApplyGravity()
        {
            // Apply downward gravity
            Vector2 gravity = Vector2.down * GRAVITY_FORCE;
            rigidbody2D.AddForce(gravity, ForceMode2D.Force);
            
            // Gradually reduce horizontal velocity
            Vector2 velocity = rigidbody2D.linearVelocity;
            velocity.x *= 0.95f;
            rigidbody2D.linearVelocity = velocity;
        }
        
        #endregion
        
        #region Helper Functions
        
        /// <summary>
        /// Set the movement direction
        /// </summary>
        public void SetDirection(Vector2 direction)
        {
            currentMoveDirection = direction.normalized;
        }
        
        /// <summary>
        /// Get current movement direction
        /// </summary>
        public Vector2 GetDirection()
        {
            return currentMoveDirection;
        }
        
        /// <summary>
        /// Get current ground direction
        /// </summary>
        public Vector2 GetGroundDirection()
        {
            return currentGroundDirection;
        }
        
        /// <summary>
        /// Check if enemy is currently grounded
        /// </summary>
        public bool IsGrounded()
        {
            return hasGround;
        }
        
        /// <summary>
        /// Flip the movement direction
        /// </summary>
        public void FlipDirection()
        {
            currentMoveDirection = -currentMoveDirection;
        }
        
        /// <summary>
        /// Stop all movement
        /// </summary>
        public void Stop()
        {
            rigidbody2D.linearVelocity = Vector2.zero;
        }
        
        #endregion
        
        #region Debug Visualization
        
        /// <summary>
        /// Draw debug gizmos for movement visualization
        /// </summary>
        public void DrawDebugGizmos()
        {
            Vector2 position = enemy.transform.position;
            
            // Draw movement direction (green)
            Debug.DrawRay(position, currentMoveDirection * 1f, Color.green);
            
            // Draw ground direction (blue)
            Debug.DrawRay(position, currentGroundDirection * 1f, Color.blue);
            
            // Draw ground normal (red)
            if (hasGround)
            {
                Debug.DrawRay(position, groundNormal * 0.5f, Color.red);
            }
            
            // Draw circle check radius (yellow)
            float enemyRadius = enemy.GetComponent<Collider2D>()?.bounds.extents.magnitude ?? 0.5f;
            float checkRadius = enemyRadius * CIRCLE_CHECK_RADIUS_MULTIPLIER;
            DrawCircle(position, checkRadius, Color.yellow);
        }
        
        private void DrawCircle(Vector2 center, float radius, Color color, int segments = 20)
        {
            float angleStep = 360f / segments;
            Vector2 prevPoint = center + new Vector2(radius, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 newPoint = center + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
                Debug.DrawLine(prevPoint, newPoint, color);
                prevPoint = newPoint;
            }
        }
        
        #endregion
    }
}
