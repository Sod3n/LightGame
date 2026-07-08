using System;
using Light_and_controller.Scripts.Components.Enemy.States;
using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy
{
    /// <summary>
    /// Slime enemy controller based on player controller architecture
    /// Features:
    /// - Wanders slowly in random directions
    /// - Can walk on walls and ceilings
    /// - Detects and chases player when in range
    /// - Fires projectiles in all directions (one always aimed at player)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class SlimeEnemy : MonoBehaviour
    {
        [Header("Components")]
        public EnemyData EnemyData;
        
        // Events for UI/View
        public event Action<float> OnChargeStart; // Passes charge duration
        public event Action OnChargeStop;
        public event Action OnAttack; // Fired when projectiles are launched
        
        /// <summary>
        /// Invoke charge start event (called by states)
        /// </summary>
        /// <param name="chargeDuration">Duration of the charge in seconds</param>
        public void InvokeChargeStart(float chargeDuration) => OnChargeStart?.Invoke(chargeDuration);
        
        /// <summary>
        /// Invoke charge stop event (called by states)
        /// </summary>
        public void InvokeChargeStop() => OnChargeStop?.Invoke();
        
        /// <summary>
        /// Invoke attack event (called by states)
        /// </summary>
        public void InvokeAttack() => OnAttack?.Invoke();
        
        [Header("State Machine")]
        public EnemyStateMachine _stateMachine;
        public EnemyState WanderState;
        public EnemyState AggroState;
        public EnemyState AttackState;
        
        [Header("References")]
        [NonSerialized] public Rigidbody2D Rigidbody2D;
        [NonSerialized] public CapsuleCollider2D CapsuleCollider2D;
        [NonSerialized] public EnemyInputManager InputManager;
        [NonSerialized] public Animator Animator;
        
        [Header("Systems")]
        [NonSerialized] public EnemyMovementNew Movement;
        
        private void Awake()
        {
            // Get components
            Rigidbody2D = GetComponent<Rigidbody2D>();
            CapsuleCollider2D = GetComponent<CapsuleCollider2D>();
            InputManager = GetComponent<EnemyInputManager>();
            Animator = GetComponent<Animator>();
            
            // If no input manager exists, add one
            if (InputManager == null)
            {
                InputManager = gameObject.AddComponent<EnemyInputManager>();
            }
            
            // Initialize input manager
            InputManager.Initialize(this, EnemyData);
            
            // Initialize movement system
            Movement = new EnemyMovementNew(this, EnemyData, Rigidbody2D);
            
            // Initialize state machine
            _stateMachine = new EnemyStateMachine();
            WanderState = new SlimeWanderState(this, _stateMachine, EnemyData);
            AggroState = new SlimeAggroState(this, _stateMachine, EnemyData);
            AttackState = new SlimeAttackState(this, _stateMachine, EnemyData);
            
            // Initialize wander timer
            EnemyData.AI.WanderTimer = EnemyData.AI.WanderDirectionChangeInterval;
        }
        
        private void Start()
        {
            // Start in wander state
            _stateMachine.Initialize(WanderState);
            
            // Set up rigidbody
            if (EnemyData.Movement.CanWalkOnWalls)
            {
                // Disable Unity gravity for wall walking - we'll apply custom gravity
                Rigidbody2D.gravityScale = 0f;
                Rigidbody2D.freezeRotation = false; // Allow rotation
            }
            else
            {
                // Normal gravity for ground-only movement
                Rigidbody2D.gravityScale = EnemyData.Movement.GravityScale;
                Rigidbody2D.freezeRotation = true; // Keep upright
            }
            
            Rigidbody2D.constraints = RigidbodyConstraints2D.None; // No constraints
            Rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            
            // Add some drag to prevent sliding
            Rigidbody2D.linearDamping = 0.5f;
        }
        
        private void Update()
        {
            _stateMachine.CurrentState?.Update();
        }
        
        private void FixedUpdate()
        {
            _stateMachine.CurrentState?.FixedUpdate();
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log($"[SlimeEnemy] OnCollisionEnter2D with {collision.gameObject.name}");
            Movement?.OnCollisionEnter(collision);
        }
        
        private void OnCollisionStay2D(Collision2D collision)
        {
            Debug.Log($"[SlimeEnemy] OnCollisionStay2D with {collision.gameObject.name}");
            Movement?.OnCollisionStay(collision);
        }
        
        private void OnCollisionExit2D(Collision2D collision)
        {
            Debug.Log($"[SlimeEnemy] OnCollisionExit2D with {collision.gameObject.name}");
            Movement?.OnCollisionExit(collision);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (EnemyData == null) return;
            
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, EnemyData.AI.DetectionRange);
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, EnemyData.AI.AttackRange);
            
            // Draw ground check
            Gizmos.color = Color.green;
            Vector2 checkPos = (Vector2)transform.position + EnemyData.Physics.GroundCheckPosition;
            Gizmos.DrawLine(checkPos, checkPos + Vector2.down * 0.5f);
        }
    }
}
