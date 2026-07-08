using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Advanced jump pad component with directional control, velocity curves, and event callbacks.
/// Integrates seamlessly with the state-based player controller system.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class JumpPadAdvanced : MonoBehaviour
{
    [System.Serializable]
    public enum JumpPadMode
    {
        Vertical,           // Standard upward bounce
        Directional,        // Bounce in a specific direction
        VelocityBased,      // Bounce strength based on incoming velocity
        Curve               // Use animation curve for custom velocity profile
    }
    
    [Header("Jump Pad Mode")]
    [SerializeField] private JumpPadMode mode = JumpPadMode.Vertical;
    
    [Header("Force Settings")]
    [SerializeField, Tooltip("Base jump force")]
    private float jumpForce = 20f;
    
    [SerializeField, Tooltip("Direction for directional mode (will be normalized)")]
    private Vector2 jumpDirection = Vector2.up;
    
    [SerializeField, Tooltip("Horizontal velocity multiplier")]
    [Range(0f, 3f)]
    private float horizontalMultiplier = 1.2f;
    
    [SerializeField, Tooltip("Override velocity completely or add to existing")]
    private bool overrideVelocity = true;
    
    [Header("Velocity-Based Settings")]
    [SerializeField, Tooltip("Minimum incoming speed to activate (for velocity-based mode)")]
    private float minActivationSpeed = 2f;
    
    [SerializeField, Tooltip("Multiplier for incoming velocity")]
    [Range(1f, 5f)]
    private float velocityMultiplier = 1.5f;
    
    [Header("Curve-Based Settings")]
    [SerializeField, Tooltip("Custom velocity curve over time")]
    private AnimationCurve velocityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [SerializeField, Tooltip("Duration of curve application")]
    private float curveDuration = 0.5f;
    
    [SerializeField, Tooltip("Maximum velocity from curve")]
    private float curveMaxVelocity = 25f;
    
    [Header("Timing")]
    [SerializeField, Tooltip("Cooldown before same player can use again")]
    private float cooldownTime = 0.5f;
    
    [SerializeField, Tooltip("Delay before applying force (for anticipation)")]
    private float activationDelay = 0f;
    
    [Header("State Control")]
    [SerializeField, Tooltip("Force player into jump state")]
    private bool forceJumpState = true;
    
    [SerializeField, Tooltip("Reset player's jump counter")]
    private bool resetJumpCounter = true;
    
    [SerializeField, Tooltip("Allow multiple jumps after bounce")]
    private bool enableMultiJump = false;
    
    [Header("Visual & Audio")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerParameter = "Bounce";
    [SerializeField] private string chargeParameter = "Charge";
    [SerializeField] private ParticleSystem bounceEffect;
    [SerializeField] private ParticleSystem chargeEffect;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bounceSound;
    [SerializeField] private AudioClip chargeSound;
    
    [Header("Layer Settings")]
    [SerializeField] private LayerMask targetLayers = -1;
    
    [Header("Events")]
    [SerializeField] private UnityEvent<PlayerMain> onPlayerBounce;
    [SerializeField] private UnityEvent<PlayerMain> onPlayerLand;
    [SerializeField] private UnityEvent onCooldownComplete;
    
    // Internal state
    private float lastBounceTime = -999f;
    private PlayerMain currentPlayer;
    private bool isCharging = false;
    private float chargeStartTime;
    private Coroutine applyCurveCoroutine;
    
    private void OnValidate()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger && mode != JumpPadMode.Curve)
        {
            col.isTrigger = true;
        }
        
        if (mode == JumpPadMode.Directional && jumpDirection != Vector2.zero)
        {
            jumpDirection = jumpDirection.normalized;
        }
    }
    
    private void Update()
    {
        // Check if cooldown is complete
        if (Time.time - lastBounceTime >= cooldownTime && lastBounceTime > 0)
        {
            onCooldownComplete?.Invoke();
            lastBounceTime = -999f;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!CanActivate())
            return;
        
        if (((1 << collision.gameObject.layer) & targetLayers) == 0)
            return;
        
        PlayerMain player = collision.gameObject.GetComponent<PlayerMain>();
        if (player == null)
            return;
        
        // Check landing angle
        Vector2 contactNormal = collision.contacts[0].normal;
        if (Vector2.Dot(contactNormal, Vector2.down) < 0.3f)
            return;
        
        HandlePlayerContact(player, collision.rigidbody);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!CanActivate())
            return;
        
        if (((1 << other.gameObject.layer) & targetLayers) == 0)
            return;
        
        PlayerMain player = other.GetComponent<PlayerMain>();
        if (player == null)
            return;
        
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null && mode == JumpPadMode.VelocityBased && rb.linearVelocity.y > 0)
            return;
        
        HandlePlayerContact(player, rb);
    }
    
    private bool CanActivate()
    {
        return Time.time - lastBounceTime >= cooldownTime;
    }
    
    private void HandlePlayerContact(PlayerMain player, Rigidbody2D rb)
    {
        if (player == null || rb == null)
            return;
        
        currentPlayer = player;
        onPlayerLand?.Invoke(player);
        
        if (activationDelay > 0)
        {
            isCharging = true;
            chargeStartTime = Time.time;
            PlayChargeEffects();
            Invoke(nameof(ApplyJumpForceDelayed), activationDelay);
        }
        else
        {
            ApplyJumpForce(player, rb);
        }
    }
    
    private void ApplyJumpForceDelayed()
    {
        if (currentPlayer != null)
        {
            Rigidbody2D rb = currentPlayer.GetComponent<Rigidbody2D>();
            ApplyJumpForce(currentPlayer, rb);
        }
        isCharging = false;
    }
    
    private void ApplyJumpForce(PlayerMain player, Rigidbody2D rb)
    {
        if (player == null || rb == null)
            return;
        
        Vector2 newVelocity = CalculateVelocity(rb);
        
        // Apply velocity based on mode
        if (mode == JumpPadMode.Curve)
        {
            // Start coroutine to apply curve over time
            if (applyCurveCoroutine != null)
                StopCoroutine(applyCurveCoroutine);
            applyCurveCoroutine = StartCoroutine(ApplyVelocityCurve(rb, newVelocity));
        }
        else
        {
            rb.linearVelocity = newVelocity;
        }
        
        // Handle player state
        if (forceJumpState || resetJumpCounter)
        {
            if (resetJumpCounter)
            {
                player.PlayerData.Jump.NextJumpInt = 1;
                player.PlayerData.Jump.NewJump = false;
                
                if (enableMultiJump)
                {
                    player.PlayerData.Jump.CoyoteTimeTimer = player.PlayerData.Jump.CoyoteTimeMaxTime;
                }
            }
            
            if (forceJumpState && player.PlayerData.Physics.IsGrounded)
            {
                player._stateMachine.ChangeState(player.JumpState);
            }
        }
        
        lastBounceTime = Time.time;
        PlayBounceEffects();
        onPlayerBounce?.Invoke(player);
    }
    
    private Vector2 CalculateVelocity(Rigidbody2D rb)
    {
        float currentHorizontalVelocity = rb.linearVelocity.x;
        Vector2 newVelocity = Vector2.zero;
        
        switch (mode)
        {
            case JumpPadMode.Vertical:
                newVelocity = new Vector2(
                    currentHorizontalVelocity * horizontalMultiplier,
                    jumpForce
                );
                break;
                
            case JumpPadMode.Directional:
                Vector2 direction = jumpDirection.normalized;
                if (overrideVelocity)
                {
                    newVelocity = direction * jumpForce;
                    newVelocity.x += currentHorizontalVelocity * (horizontalMultiplier - 1f);
                }
                else
                {
                    newVelocity = rb.linearVelocity + direction * jumpForce;
                }
                break;
                
            case JumpPadMode.VelocityBased:
                float incomingSpeed = Mathf.Abs(rb.linearVelocity.y);
                if (incomingSpeed >= minActivationSpeed)
                {
                    float boostedForce = jumpForce + (incomingSpeed * velocityMultiplier);
                    newVelocity = new Vector2(
                        currentHorizontalVelocity * horizontalMultiplier,
                        boostedForce
                    );
                }
                else
                {
                    newVelocity = new Vector2(
                        currentHorizontalVelocity * horizontalMultiplier,
                        jumpForce
                    );
                }
                break;
                
            case JumpPadMode.Curve:
                // Initial velocity for curve mode
                newVelocity = new Vector2(
                    currentHorizontalVelocity * horizontalMultiplier,
                    curveMaxVelocity * velocityCurve.Evaluate(0)
                );
                break;
        }
        
        if (!overrideVelocity && mode != JumpPadMode.Directional)
        {
            newVelocity += rb.linearVelocity;
        }
        
        return newVelocity;
    }
    
    private System.Collections.IEnumerator ApplyVelocityCurve(Rigidbody2D rb, Vector2 initialVelocity)
    {
        float elapsed = 0f;
        float initialHorizontalVelocity = initialVelocity.x;
        
        while (elapsed < curveDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = elapsed / curveDuration;
            float curveValue = velocityCurve.Evaluate(t);
            
            Vector2 velocity = rb.linearVelocity;
            velocity.y = curveValue * curveMaxVelocity;
            velocity.x = initialHorizontalVelocity;
            rb.linearVelocity = velocity;
            
            yield return new WaitForFixedUpdate();
        }
    }
    
    private void PlayChargeEffects()
    {
        if (animator != null && !string.IsNullOrEmpty(chargeParameter))
        {
            animator.SetTrigger(chargeParameter);
        }
        
        if (chargeEffect != null)
        {
            chargeEffect.Play();
        }
        
        if (audioSource != null && chargeSound != null)
        {
            audioSource.PlayOneShot(chargeSound);
        }
    }
    
    private void PlayBounceEffects()
    {
        if (animator != null && !string.IsNullOrEmpty(triggerParameter))
        {
            animator.SetTrigger(triggerParameter);
        }
        
        if (bounceEffect != null)
        {
            bounceEffect.Play();
        }
        
        if (audioSource != null && bounceSound != null)
        {
            audioSource.PlayOneShot(bounceSound);
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 position = transform.position;
        
        switch (mode)
        {
            case JumpPadMode.Vertical:
                DrawArrow(position, Vector3.up * (jumpForce * 0.1f), Color.yellow);
                break;
                
            case JumpPadMode.Directional:
                DrawArrow(position, (Vector3)jumpDirection * (jumpForce * 0.1f), Color.cyan);
                break;
                
            case JumpPadMode.VelocityBased:
                DrawArrow(position, Vector3.up * (jumpForce * 0.1f), Color.green);
                Gizmos.DrawWireSphere(position, minActivationSpeed * 0.1f);
                break;
                
            case JumpPadMode.Curve:
                DrawCurveGizmo(position);
                break;
        }
    }
    
    private void DrawArrow(Vector3 start, Vector3 direction, Color color)
    {
        Gizmos.color = color;
        Vector3 end = start + direction;
        Gizmos.DrawLine(start, end);
        
        Vector3 right = Quaternion.Euler(0, 0, 150) * direction.normalized * 0.3f;
        Vector3 left = Quaternion.Euler(0, 0, -150) * direction.normalized * 0.3f;
        Gizmos.DrawLine(end, end + right);
        Gizmos.DrawLine(end, end + left);
    }
    
    private void DrawCurveGizmo(Vector3 position)
    {
        Gizmos.color = Color.magenta;
        int segments = 20;
        Vector3 lastPoint = position;
        
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float curveValue = velocityCurve.Evaluate(t);
            Vector3 point = position + new Vector3(t * 2f - 1f, curveValue * curveMaxVelocity * 0.1f, 0);
            Gizmos.DrawLine(lastPoint, point);
            lastPoint = point;
        }
    }
}
