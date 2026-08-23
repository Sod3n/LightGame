using System;
using UnityEngine;

/// <summary>
/// Jump pad component that triggers a jump when the player lands on it.
/// Works with the state-based player controller system by initiating jump state transitions.
/// </summary>
public class JumpPad : MonoBehaviour
{
    [Header("Jump Pad Settings")]
    [SerializeField, Tooltip("Which jump to trigger (1 = first jump, 2 = double jump, 3 = triple jump)")]
    [Range(1, 3)]
    private int jumpIndex = 1;
    
    [SerializeField, Tooltip("Use collision normal as launch direction instead of fixed direction")]
    private bool useCollisionNormal = false;
    
    [SerializeField, Tooltip("Direction to launch the player (will be normalized). Ignored if useCollisionNormal is true")]
    private Vector2 launchDirection = Vector2.up;
    
    [SerializeField, Tooltip("Force multiplier for the launch (1.0 = normal jump height)")]
    [Range(0.5f, 10f)]
    private float forceMultiplier = 1.5f;
    
    [SerializeField, Tooltip("Whether to preserve player's velocity in perpendicular direction")]
    private bool preservePerpendicularVelocity = true;
    
    [SerializeField, Tooltip("Minimum angle (in degrees) between collision normal and pad's up direction to activate. 0 = any angle, 90 = must hit from specific side")]
    [Range(0f, 90f)]
    private float activationAngle = 60f;
    
    [SerializeField, Tooltip("Cooldown time before the same player can use this pad again")]
    private float cooldownTime = 0.5f;

    [SerializeField, Tooltip("Time interval to check for stuck objects on the jump pad")]
    private float stuckCheckInterval = 0.2f;

    [SerializeField, Tooltip("Minimum velocity threshold to consider an object as stuck")]
    private float stuckVelocityThreshold = 0.5f;

    [Header("Layer Settings")]
    [SerializeField, Tooltip("Layer mask for objects that can use the jump pad")]
    private LayerMask targetLayers = -1;
    
    [Header("Events")]
    [Tooltip("Called when a player lands on the jump pad (before bounce)")]
    public Action<PlayerMain> OnPlayerLand;
    
    [Tooltip("Called when the jump force is applied to the player")]
    public Action<PlayerMain> OnPlayerBounce;
    
    [Tooltip("Called when the cooldown period completes")]
    public Action OnCooldownComplete;
    
    [Tooltip("Called for visual/audio feedback (same as OnPlayerBounce but without player parameter)")]
    public Action OnBounce;
    
    // Internal state
    private float lastBounceTime = -999f;
    private PlayerMain lastPlayer;
    private bool cooldownCompleted = true;
    private float lastStuckCheckTime = 0f;
    private PlayerMain currentCollidingPlayer;
    private Rigidbody2D currentCollidingRigidbody;
    private Vector2 lastCollisionNormal;
    
    /// <summary>
    /// Returns true if the jump pad is ready to be used (not on cooldown)
    /// </summary>
    public bool IsActive => cooldownCompleted;
    
    private void OnValidate()
    {
        // Ensure we have a collider and it's NOT set as a trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && col.isTrigger)
        {
            col.isTrigger = false;
            Debug.LogWarning($"JumpPad on {gameObject.name}: Collider2D was set as trigger. Auto-corrected to non-trigger.");
        }
    }
    
    private void Update()
    {
        // Check if cooldown has completed
        if (!cooldownCompleted && Time.time - lastBounceTime >= cooldownTime)
        {
            cooldownCompleted = true;
            OnCooldownComplete?.Invoke();
        }

        // Periodic check for stuck objects
        if (currentCollidingPlayer != null && currentCollidingRigidbody != null)
        {
            if (Time.time - lastStuckCheckTime >= stuckCheckInterval)
            {
                lastStuckCheckTime = Time.time;
                CheckAndHandleStuckObject();
            }
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the colliding object is on the target layer
        if (((1 << collision.gameObject.layer) & targetLayers) == 0)
            return;
        
        // Try to get the PlayerMain component
        PlayerMain player = collision.gameObject.GetComponent<PlayerMain>();
        if (player == null)
            return;

        // Track the colliding player for stuck detection
        currentCollidingPlayer = player;
        currentCollidingRigidbody = collision.rigidbody;
        lastStuckCheckTime = Time.time;
        
        // Store collision normal for potential use in launch direction
        if (collision.contactCount > 0)
        {
            lastCollisionNormal = collision.contacts[0].normal;
        }

        // Check if enough time has passed since last bounce
        if (Time.time - lastBounceTime < cooldownTime)
            return;

        // Check if player is hitting from the correct direction
        if (activationAngle > 0f)
        {
            Vector2 contactNormal = collision.contacts[0].normal;
            Vector2 padNormal = -launchDirection.normalized; // Opposite of launch direction
            float dotProduct = Vector2.Dot(contactNormal, padNormal);
            float minDot = Mathf.Cos(activationAngle * Mathf.Deg2Rad);
            
            if (dotProduct < minDot)
                return; // Player didn't hit from the correct angle
        }
        
        // Invoke land event
        OnPlayerLand?.Invoke(player);
        
        ApplyJumpForce(player, collision.rigidbody, collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Update the current colliding player reference
        if (((1 << collision.gameObject.layer) & targetLayers) != 0)
        {
            PlayerMain player = collision.gameObject.GetComponent<PlayerMain>();
            if (player != null)
            {
                currentCollidingPlayer = player;
                currentCollidingRigidbody = collision.rigidbody;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Clear the colliding player reference when they leave
        PlayerMain player = collision.gameObject.GetComponent<PlayerMain>();
        if (player != null && player == currentCollidingPlayer)
        {
            currentCollidingPlayer = null;
            currentCollidingRigidbody = null;
        }
    }

    private void CheckAndHandleStuckObject()
    {
        if (currentCollidingPlayer == null || currentCollidingRigidbody == null)
            return;

        // Check if the object is moving slowly (potentially stuck)
        float currentSpeed = currentCollidingRigidbody.linearVelocity.magnitude;

        if (currentSpeed < stuckVelocityThreshold)
        {
            // Object appears to be stuck, apply jump force
            // Invoke land event
            OnPlayerLand?.Invoke(currentCollidingPlayer);

            ApplyJumpForce(currentCollidingPlayer, currentCollidingRigidbody, null);
        }
    }
    
    
    private void ApplyJumpForce(PlayerMain player, Rigidbody2D rb, Collision2D collision)
    {
        if (player == null || rb == null)
            return;
        
        // Validate jump index
        int clampedJumpIndex = Mathf.Clamp(jumpIndex, 1, player.PlayerData.Jump.Jumps.Count);
        if (clampedJumpIndex != jumpIndex)
        {
            Debug.LogWarning($"JumpPad on {gameObject.name}: Invalid jump index {jumpIndex}. Using index {clampedJumpIndex}.");
        }
        
        // Get jump info and calculate launch velocity
        var jumpInfo = player.PlayerData.Jump.Jumps[clampedJumpIndex - 1];
        float baseForce = jumpInfo.MaxHeight * forceMultiplier;
        
        // Determine launch direction based on settings
        Vector2 normalizedDirection;
        if (useCollisionNormal && collision != null && collision.contactCount > 0)
        {
            // Use the collision normal as launch direction (inverted because normal points into surface)
            normalizedDirection = -collision.contacts[0].normal;
            Debug.Log($"[JumpPad] Using collision normal (inverted): {normalizedDirection}");
        }
        else if (useCollisionNormal && lastCollisionNormal != Vector2.zero)
        {
            // Fallback to last stored collision normal (for stuck object handling)
            normalizedDirection = -lastCollisionNormal;
            Debug.Log($"[JumpPad] Using last collision normal (inverted): {normalizedDirection}");
        }
        else
        {
            // Use fixed launch direction
            normalizedDirection = launchDirection.normalized;
            Debug.Log($"[JumpPad] Using fixed launch direction: {normalizedDirection}");
        }
        
        // Use DirectionalJumpState for angled jumps, normal JumpState for upward jumps
        if (useCollisionNormal || launchDirection.normalized != Vector2.up)
        {
            // Use directional jump state for custom directions
            Debug.Log($"[JumpPad] Using DirectionalJumpState - Direction: {normalizedDirection}, Force: {baseForce}");
            var directionalJumpState = player.DirectionalJumpState as PlayerDirectionalJumpState;
            if (directionalJumpState != null)
            {
                directionalJumpState.SetLaunchParameters(normalizedDirection, baseForce, jumpInfo.JumpTime, preservePerpendicularVelocity, clampedJumpIndex);
                player._stateMachine.ChangeState(player.DirectionalJumpState);
            }
            else
            {
                Debug.LogError($"[JumpPad] DirectionalJumpState is null!");
            }
        }
        else
        {
            // Use normal jump state for standard upward jumps
            Debug.Log($"[JumpPad] Using normal JumpState - Force multiplier: {forceMultiplier}");
            player.PlayerData.Jump.NextJumpInt = clampedJumpIndex;
            player.PlayerData.Jump.NewJump = true;
            player.PlayerData.Jump.CoyoteTimeTimer = player.PlayerData.Jump.CoyoteTimeMaxTime;
            player._stateMachine.ChangeState(player.JumpState);
            
            var jumpState = player.JumpState as PlayerJumpState;
            if (jumpState != null)
            {
                jumpState.VelocityMultiplier = forceMultiplier;
            }
        }
        
        // Update cooldown
        lastBounceTime = Time.time;
        lastPlayer = player;
        cooldownCompleted = false;
        
        // Invoke bounce events
        OnPlayerBounce?.Invoke(player);
        OnBounce?.Invoke();
    }
    
    private void OnDrawGizmos()
    {
        // Draw a visual indicator in the editor
        Gizmos.color = cooldownCompleted ? Color.yellow : Color.gray;
        Vector3 position = transform.position;
        
        // Draw arrow showing launch direction
        Vector3 direction = useCollisionNormal ? Vector3.up : launchDirection.normalized;
        float visualLength = forceMultiplier * 2f;
        Vector3 endPoint = position + (Vector3)(direction * visualLength);
        
        Gizmos.DrawLine(position, endPoint);
        
        // Draw arrowhead
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * 0.2f;
        Gizmos.DrawLine(endPoint, endPoint - (Vector3)(direction * 0.3f) + perpendicular);
        Gizmos.DrawLine(endPoint, endPoint - (Vector3)(direction * 0.3f) - perpendicular);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw more detailed gizmo when selected
        Gizmos.color = Color.green;
        Vector3 position = transform.position;
        
        // Draw force visualization
        Vector3 direction = useCollisionNormal ? Vector3.up : launchDirection.normalized;
        float visualLength = forceMultiplier * 2f;
        Vector3 endPoint = position + (Vector3)(direction * visualLength);
        Gizmos.DrawWireSphere(endPoint, 0.3f);
        
        // Draw activation cone if angle restriction is set
        if (activationAngle > 0f)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Vector3 padNormal = -(Vector3)direction;
            
            // Draw cone representing valid collision angles
            int segments = 16;
            float angleStep = 360f / segments;
            Vector3 perpendicular = new Vector3(-padNormal.y, padNormal.x, 0);
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = angleStep * i;
                float angle2 = angleStep * (i + 1);
                
                Vector3 dir1 = Quaternion.AngleAxis(angle1, Vector3.forward) * perpendicular;
                Vector3 dir2 = Quaternion.AngleAxis(angle2, Vector3.forward) * perpendicular;
                
                Vector3 coneDir1 = (padNormal + dir1 * Mathf.Tan(activationAngle * Mathf.Deg2Rad)).normalized;
                Vector3 coneDir2 = (padNormal + dir2 * Mathf.Tan(activationAngle * Mathf.Deg2Rad)).normalized;
                
                Gizmos.DrawLine(position, position + coneDir1 * 1.5f);
                Gizmos.DrawLine(position + coneDir1 * 1.5f, position + coneDir2 * 1.5f);
            }
        }
    }
}
