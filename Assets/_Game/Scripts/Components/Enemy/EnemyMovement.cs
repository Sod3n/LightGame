using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy
{
    /// <summary>
    /// Handles all movement logic for enemies including wall/ceiling walking
    /// </summary>
    public class EnemyMovement
    {
        // Physics constants
        private const float GROUND_CHECK_DISTANCE = 1f;
        private const float CORNER_SEARCH_RADIUS = 2.0f;  // Increased from 1.6f
        private const float CORNER_CAST_DISTANCE = 1.5f;  // Increased from 1.0f
        private const float OVERLAP_DETECTION_RADIUS = 1.8f;
        private const float SURFACE_ATTRACTION_FORCE = 40f;
        private const float MAX_SURFACE_DISTANCE = 1.2f;
        private const float ROTATION_DISTANCE_THRESHOLD = 0.5f;  // Only rotate when this close to surface
        private const float SURFACE_GRAVITY = 30f;
        private const float CORNER_GRAVITY = 50f;
        private const float GRACE_GRAVITY = 15f;
        private const float FALL_GRAVITY = 40f;
        private const float ROTATION_SPEED = 8f;
        private const float VELOCITY_LERP = 15f;
        private const float HORIZONTAL_THRESHOLD = 0.7f;
        private const float HORIZONTAL_BIAS = 0.5f;
        private const float OBSTACLE_THRESHOLD = 0.7f;
        private const float CORNER_GRACE_PERIOD = 0.2f;
        private const int MAX_CORNER_SEARCH_ATTEMPTS = 3;
        
        private readonly SlimeEnemy enemy;
        private readonly EnemyData enemyData;
        private readonly Rigidbody2D rigidbody2D;
        
        // Movement state
        private Vector2 currentDirection = Vector2.right;
        private bool shouldUpdateRotation = true;
        
        // Ground tracking
        private bool hadGroundLastFrame = false;
        private Collider2D lastGroundCollider = null;
        private ContactPoint2D? lastGroundContact;
        
        // Timing
        private float lostGroundTime = -1f;
        private int cornerSearchAttempts = 0;
        
        public EnemyMovement(SlimeEnemy enemy, EnemyData enemyData, Rigidbody2D rigidbody2D)
        {
            this.enemy = enemy;
            this.enemyData = enemyData;
            this.rigidbody2D = rigidbody2D;
        }
        
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
        
        private void MoveGroundOnly(float speed)
        {
            if (!enemyData.Physics.IsGrounded) return;
            
            rigidbody2D.linearVelocity = new Vector2(speed, rigidbody2D.linearVelocity.y);
            enemy.transform.rotation = Quaternion.Lerp(
                enemy.transform.rotation, 
                Quaternion.identity, 
                Time.fixedDeltaTime * 10f
            );
        }
        
        private void MoveOnSurface(float speed)
        {
            Vector2 position = enemy.transform.position;
            
            // Primary approach: Use overlap detection to find nearby surfaces
            SurfaceInfo surfaceInfo = FindNearbySurface(position);
            
            if (surfaceInfo.HasSurface)
            {
                HandleSurfaceMovement(surfaceInfo, position);
            }
            else
            {
                // Fallback: Try traditional ground detection
                RaycastHit2D groundHit = FindGroundSurface(position);
                
                if (groundHit.collider != null)
                {
                    HandleGroundedMovement(groundHit, position);
                }
                else if (hadGroundLastFrame || cornerSearchAttempts > 0)
                {
                    HandleCornerDetection(position);
                }
                else
                {
                    HandleAirborneMovement();
                }
            }
            
            ApplyMovementVelocity(speed, position);
        }
        
        private struct SurfaceInfo
        {
            public bool HasSurface;
            public Vector2 SurfacePoint;
            public Vector2 SurfaceNormal;
            public Collider2D SurfaceCollider;
            public float Distance;
        }
        
        private SurfaceInfo FindNearbySurface(Vector2 position)
        {
            // Find all nearby surfaces using overlap
            Collider2D[] nearbySurfaces = Physics2D.OverlapCircleAll(
                position, OVERLAP_DETECTION_RADIUS, enemyData.Physics.GroundLayerMask);
            
            if (nearbySurfaces.Length == 0)
            {
                return new SurfaceInfo { HasSurface = false };
            }
            
            // Find the best surface point using movement-aware scoring
            Vector2 bestPoint = Vector2.zero;
            Collider2D bestCollider = null;
            Vector2 bestNormal = Vector2.zero;
            float bestScore = float.MinValue;
            
            // Calculate "down" direction relative to current movement
            Vector2 relativeDown = new Vector2(currentDirection.y, -currentDirection.x);
            
            foreach (var surface in nearbySurfaces)
            {
                Vector2 surfacePoint = surface.ClosestPoint(position);
                float distance = Vector2.Distance(position, surfacePoint);
                
                // Skip if too far
                if (distance > MAX_SURFACE_DISTANCE)
                    continue;
                
                // Get surface normal
                Vector2 toSurface = (surfacePoint - position).normalized;
                RaycastHit2D normalCheck = Physics2D.Raycast(
                    position, toSurface, distance + 0.1f, enemyData.Physics.GroundLayerMask);
                Vector2 surfaceNormal = normalCheck.collider != null ? normalCheck.normal : -toSurface;
                
                // Score this surface based on multiple factors
                float score = 0f;
                
                // 1. Prefer surfaces below us (relative to movement direction)
                float downAlignment = Vector2.Dot(toSurface, relativeDown);
                score += downAlignment * 3f;
                
                // 2. Prefer closer surfaces
                float distanceScore = 1f - (distance / MAX_SURFACE_DISTANCE);
                score += distanceScore * 2f;
                
                // 3. Prefer surfaces we're moving toward (not away from)
                float movementAlignment = Vector2.Dot(currentDirection, Vector2.Perpendicular(surfaceNormal));
                score += Mathf.Abs(movementAlignment) * 1.5f;
                
                // 4. Penalize surfaces behind us
                float forwardAlignment = Vector2.Dot(toSurface, currentDirection);
                if (forwardAlignment < -0.3f)
                    score -= 2f;
                
                // Debug visualization for scoring
                Color debugColor = score > 0 ? Color.Lerp(Color.yellow, Color.green, score / 5f) : Color.red;
                Debug.DrawLine(position, surfacePoint, debugColor, 0f);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = surfacePoint;
                    bestCollider = surface;
                    bestNormal = surfaceNormal;
                }
            }
            
            // No valid surface found
            if (bestCollider == null)
            {
                return new SurfaceInfo { HasSurface = false };
            }
            
            float finalDistance = Vector2.Distance(position, bestPoint);
            
            // Debug visualization
            Debug.DrawLine(position, bestPoint, Color.cyan, 0f);
            Debug.DrawRay(bestPoint, bestNormal * 0.5f, Color.magenta);
            DrawCircle(position, OVERLAP_DETECTION_RADIUS, Color.yellow, 0f, 12);
            
            return new SurfaceInfo
            {
                HasSurface = true,
                SurfacePoint = bestPoint,
                SurfaceNormal = bestNormal,
                SurfaceCollider = bestCollider,
                Distance = finalDistance
            };
        }
        
        private void HandleSurfaceMovement(SurfaceInfo surfaceInfo, Vector2 position)
        {
            Vector2 surfaceNormal = surfaceInfo.SurfaceNormal;
            
            // Apply attraction force toward surface (stronger when further away)
            float attractionMultiplier = Mathf.Clamp01(surfaceInfo.Distance / MAX_SURFACE_DISTANCE);
            rigidbody2D.AddForce(-surfaceNormal * SURFACE_ATTRACTION_FORCE * (1f + attractionMultiplier), ForceMode2D.Force);
            
            // Calculate rotation influence based on distance
            // Use a curve that's strong in middle range but weaker at extremes
            float normalizedDistance = Mathf.Clamp01(surfaceInfo.Distance / ROTATION_DISTANCE_THRESHOLD);
            float rotationInfluence = 1f - normalizedDistance;
            
            // Reduce rotation when TOO close (prevents over-rotation at corners)
            if (surfaceInfo.Distance < 0.15f)
            {
                rotationInfluence *= 0.5f;
            }
            
            // Align direction to surface (with distance-based blending)
            Vector2 targetDirection = CalculateSurfaceDirection(surfaceNormal);
            currentDirection = Vector2.Lerp(currentDirection, targetDirection, rotationInfluence * Time.fixedDeltaTime * ROTATION_SPEED).normalized;
            
            // Align rotation to surface (only when close enough)
            if (shouldUpdateRotation && rotationInfluence > 0.1f)
            {
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
                float rotationSpeed = ROTATION_SPEED * rotationInfluence;
                enemy.transform.rotation = Quaternion.Slerp(
                    enemy.transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed
                );
            }
            
            // Anti-stuck mechanism: if velocity is very low, add a forward boost
            if (rigidbody2D.linearVelocity.magnitude < 0.5f && surfaceInfo.Distance < 0.3f)
            {
                rigidbody2D.AddForce(currentDirection * 15f, ForceMode2D.Force);
                Debug.DrawRay(position, currentDirection * 0.8f, Color.red);
            }
            
            Debug.DrawRay(surfaceInfo.SurfacePoint, surfaceNormal * 0.5f, Color.green);
            Debug.DrawRay(position, currentDirection, Color.cyan);
            Debug.DrawRay(position, Vector2.up * rotationInfluence * 0.5f, Color.white);  // Show rotation influence
            
            lastGroundCollider = surfaceInfo.SurfaceCollider;
            hadGroundLastFrame = true;
            lostGroundTime = -1f;
            cornerSearchAttempts = 0;
        }
        
        private RaycastHit2D FindGroundSurface(Vector2 position)
        {
            Vector2 downwardDir = new Vector2(currentDirection.y, -currentDirection.x);
            RaycastHit2D hit = Physics2D.Raycast(position, downwardDir, GROUND_CHECK_DISTANCE, enemyData.Physics.GroundLayerMask);
            
            if (hit.collider == null)
            {
                downwardDir = -downwardDir;
                hit = Physics2D.Raycast(position, downwardDir, GROUND_CHECK_DISTANCE, enemyData.Physics.GroundLayerMask);
            }
            
            Debug.DrawRay(position, downwardDir * GROUND_CHECK_DISTANCE, hit.collider != null ? Color.green : Color.yellow);
            return hit;
        }
        
        private void HandleGroundedMovement(RaycastHit2D groundHit, Vector2 position)
        {
            Vector2 surfaceNormal = groundHit.normal;
            
            // Apply gravity toward surface
            rigidbody2D.AddForce(-surfaceNormal * SURFACE_GRAVITY, ForceMode2D.Force);
            
            // Align direction to surface
            currentDirection = CalculateSurfaceDirection(surfaceNormal);
            
            // Align rotation to surface
            if (shouldUpdateRotation)
            {
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
                enemy.transform.rotation = Quaternion.Slerp(
                    enemy.transform.rotation, targetRotation, Time.fixedDeltaTime * ROTATION_SPEED
                );
            }
            
            Debug.DrawRay(groundHit.point, surfaceNormal * 0.5f, Color.green);
            Debug.DrawRay(position, currentDirection, Color.cyan);
            
            lastGroundCollider = groundHit.collider;
            hadGroundLastFrame = true;
            lostGroundTime = -1f;
            cornerSearchAttempts = 0;
        }
        
        private Vector2 CalculateSurfaceDirection(Vector2 surfaceNormal)
        {
            Vector2 dir1 = Vector2.Perpendicular(surfaceNormal);
            Vector2 dir2 = -dir1;
            
            float dot1 = Vector2.Dot(dir1, currentDirection);
            float dot2 = Vector2.Dot(dir2, currentDirection);
            
            // Add horizontal bias for floor/ceiling
            if (Mathf.Abs(surfaceNormal.y) > HORIZONTAL_THRESHOLD && Mathf.Abs(currentDirection.x) > 0.3f)
            {
                dot1 += dir1.x * Mathf.Sign(currentDirection.x) * HORIZONTAL_BIAS;
                dot2 += dir2.x * Mathf.Sign(currentDirection.x) * HORIZONTAL_BIAS;
            }
            
            Vector2 alignedDirection = (dot1 > dot2) ? dir1 : dir2;
            return Vector2.Lerp(currentDirection, alignedDirection, Time.fixedDeltaTime * ROTATION_SPEED).normalized;
        }
        
        private void HandleCornerDetection(Vector2 position)
        {
            RaycastHit2D cornerHit = FindCornerSurface(position);
            
            if (cornerHit.collider != null)
            {
                // Successfully found corner surface
                currentDirection = CalculateSurfaceDirection(cornerHit.normal);
                rigidbody2D.AddForce(-cornerHit.normal * CORNER_GRAVITY, ForceMode2D.Force);
                Debug.DrawRay(cornerHit.point, cornerHit.normal * 0.5f, Color.green, 0.5f);
                
                // Reset search attempts
                cornerSearchAttempts = 0;
                hadGroundLastFrame = false;
                lostGroundTime = Time.time;
            }
            else
            {
                // Increment search attempts
                if (hadGroundLastFrame)
                {
                    cornerSearchAttempts = 1;
                    hadGroundLastFrame = false;
                    lostGroundTime = Time.time;
                }
                else
                {
                    cornerSearchAttempts++;
                }
                
                // Only give up after multiple attempts
                if (cornerSearchAttempts >= MAX_CORNER_SEARCH_ATTEMPTS)
                {
                    // Cliff detected - turn around
                    currentDirection = -currentDirection;
                    rigidbody2D.AddForce(Vector2.down * SURFACE_GRAVITY + currentDirection * 20f, ForceMode2D.Force);
                    Debug.DrawRay(position, currentDirection, Color.red, 1f);
                    cornerSearchAttempts = 0;
                }
                else
                {
                    // Keep searching, apply gentle gravity to stay near surface
                    rigidbody2D.AddForce(Vector2.down * GRACE_GRAVITY, ForceMode2D.Force);
                    Debug.DrawRay(position, Vector2.down * 0.5f, Color.yellow, 0.5f);
                }
            }
        }
        
        private RaycastHit2D FindCornerSurface(Vector2 position)
        {
            // Try current direction first
            RaycastHit2D hit = FindBestSurfaceInDirection(position, currentDirection);
            if (hit.collider != null) return hit;
            
            // Try perpendicular and downward directions
            Vector2[] searchDirections = {
                new Vector2(currentDirection.y, -currentDirection.x),
                new Vector2(-currentDirection.y, currentDirection.x),
                Vector2.down
            };
            
            foreach (var dir in searchDirections)
            {
                hit = FindBestSurfaceInDirection(position, dir);
                if (hit.collider != null) return hit;
            }
            
            return default;
        }
        
        private RaycastHit2D FindBestSurfaceInDirection(Vector2 position, Vector2 direction)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(
                position, CORNER_SEARCH_RADIUS, direction, CORNER_CAST_DISTANCE, enemyData.Physics.GroundLayerMask
            );
            
            RaycastHit2D bestHit = default;
            float bestScore = -1f;
            
            foreach (var hit in hits)
            {
                if (hit.collider == lastGroundCollider) continue;
                
                // Prefer surfaces perpendicular to movement
                float dot = Mathf.Abs(Vector2.Dot(hit.normal, currentDirection));
                float score = 1f - dot;
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestHit = hit;
                }
            }
            
            DrawCircleCast(position, CORNER_SEARCH_RADIUS, direction, CORNER_CAST_DISTANCE,
                bestHit.collider != null ? Color.green : Color.yellow);
            
            return bestHit;
        }
        
        private void HandleAirborneMovement()
        {
            float timeSinceLostGround = Time.time - lostGroundTime;
            
            if (timeSinceLostGround < CORNER_GRACE_PERIOD)
            {
                rigidbody2D.AddForce(Vector2.down * GRACE_GRAVITY, ForceMode2D.Force);
            }
            else
            {
                rigidbody2D.AddForce(Vector2.down * FALL_GRAVITY, ForceMode2D.Force);
                
                if (shouldUpdateRotation)
                {
                    enemy.transform.rotation = Quaternion.Slerp(
                        enemy.transform.rotation, Quaternion.identity, Time.fixedDeltaTime * 5f
                    );
                }
            }
            
            if (lastGroundContact.HasValue)
            {
                Debug.DrawRay(lastGroundContact.Value.point, lastGroundContact.Value.normal * 0.5f, Color.cyan);
            }
        }
        
        private void ApplyMovementVelocity(float speed, Vector2 position)
        {
            Vector2 targetVelocity = currentDirection * Mathf.Abs(speed);
            rigidbody2D.linearVelocity = Vector2.Lerp(
                rigidbody2D.linearVelocity, targetVelocity, Time.fixedDeltaTime * VELOCITY_LERP
            );
            
            Debug.DrawRay(position, targetVelocity * 0.3f, Color.blue);
        }
        
        public void OnCollisionEnter(Collision2D collision)
        {
            if (collision.contactCount == 0) return;
            
            ContactPoint2D contact = collision.GetContact(0);
            
            if (IsObstacle(contact.normal))
            {
                HandleObstacleCollision(contact);
            }
            else
            {
                lastGroundContact = contact;
            }
        }
        
        private bool IsObstacle(Vector2 contactNormal)
        {
            return Mathf.Abs(Vector2.Dot(contactNormal, currentDirection)) > OBSTACLE_THRESHOLD;
        }
        
        private void HandleObstacleCollision(ContactPoint2D contact)
        {
            Vector2 perpendicular1 = Vector2.Perpendicular(contact.normal);
            Vector2 perpendicular2 = -perpendicular1;
            
            RaycastHit2D check1 = Physics2D.Raycast(contact.point, perpendicular1, 0.5f, enemyData.Physics.GroundLayerMask);
            RaycastHit2D check2 = Physics2D.Raycast(contact.point, perpendicular2, 0.5f, enemyData.Physics.GroundLayerMask);
            
            float score1 = Vector2.Dot(perpendicular1, currentDirection);
            float score2 = Vector2.Dot(perpendicular2, currentDirection);
            
            if (check1.collider != null) score1 -= 10f;
            if (check2.collider != null) score2 -= 10f;
            
            // Prefer upward movement when both directions are free
            if (check1.collider == null && check2.collider == null && Mathf.Abs(score1 - score2) < 0.1f)
            {
                score1 -= Vector2.Dot(perpendicular1, Vector2.down) * 0.3f;
                score2 -= Vector2.Dot(perpendicular2, Vector2.down) * 0.3f;
            }
            
            currentDirection = (score1 > score2 ? perpendicular1 : perpendicular2).normalized;
            
            Debug.DrawRay(contact.point, contact.normal * 0.5f, Color.yellow, 1f);
            Debug.DrawRay(contact.point, currentDirection * 0.8f, Color.magenta, 1f);
        }
        
        public void OnCollisionStay(Collision2D collision)
        {
            if (collision.contactCount == 0) return;
            
            ContactPoint2D contact = collision.GetContact(0);
            float dotWithMovement = Vector2.Dot(contact.normal, currentDirection);
            
            if (dotWithMovement > 0.5f || Mathf.Abs(dotWithMovement) < 0.3f)
            {
                lastGroundContact = contact;
            }
        }
        
        public void OnCollisionExit(Collision2D collision)
        {
            lastGroundContact = null;
        }
        
        public void Stop()
        {
            rigidbody2D.linearVelocity = Vector2.zero;
        }
        
        public void SetRotationEnabled(bool enabled)
        {
            shouldUpdateRotation = enabled;
        }
        
        private void DrawCircleCast(Vector2 origin, float radius, Vector2 direction, float distance, Color color)
        {
            float duration = 2f;
            
            DrawCircle(origin, radius, color, duration);
            DrawCircle(origin + direction * distance, radius, color, duration);
            
            Vector2 perpendicular = Vector2.Perpendicular(direction.normalized) * radius;
            Debug.DrawLine(origin + perpendicular, origin + direction * distance + perpendicular, color, duration);
            Debug.DrawLine(origin - perpendicular, origin + direction * distance - perpendicular, color, duration);
            Debug.DrawRay(origin, direction * distance, color, duration);
        }
        
        private void DrawCircle(Vector2 center, float radius, Color color, float duration = 0f, int segments = 16)
        {
            float angleStep = 360f / segments;
            Vector2 prevPoint = center + new Vector2(radius, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Debug.DrawLine(prevPoint, newPoint, color, duration);
                prevPoint = newPoint;
            }
        }
    }
}
