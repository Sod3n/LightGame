using System;
using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/Enemy Data")]
    [Serializable]
    public class EnemyData : ScriptableObject
    {
        #region Variable Organization Classes
        
        [Serializable]
        public class PhysicsVariables
        {
            [SerializeField] private LayerMask groundLayerMask;
            [SerializeField] private LayerMask wallLayerMask;
            [SerializeField, NonEditable] private Vector3 groundCheckPosition;
            [SerializeField, NonEditable] private bool isGrounded;
            [SerializeField, NonEditable] private bool isNextToWall;
            [SerializeField, NonEditable] private int wallDirection;
            [SerializeField, NonEditable] private int facingDirection;
            [SerializeField, NonEditable] private Vector2 walkSpeedDirection;
            
            public LayerMask GroundLayerMask => groundLayerMask;
            public LayerMask WallLayerMask => wallLayerMask;
            public Vector2 GroundCheckPosition { get { return groundCheckPosition; } set { groundCheckPosition = value; } }
            public bool IsGrounded { get { return isGrounded; } set { isGrounded = value; } }
            public bool IsNextToWall { get { return isNextToWall; } set { isNextToWall = value; } }
            public int WallDirection { get { return wallDirection; } set { wallDirection = value; } }
            public int FacingDirection { get { return facingDirection; } set { facingDirection = value; } }
            public Vector2 WalkSpeedDirection { get { return walkSpeedDirection; } set { walkSpeedDirection = value; } }
        }
        
        [Serializable]
        public class MovementVariables
        {
            [SerializeField] private float wanderSpeed = 2f;
            [SerializeField] private float aggroSpeed = 3f;
            [SerializeField, Range(0, 1)] private float gravityScale = 1f;
            [SerializeField] private bool canWalkOnWalls = true;
            [SerializeField] private bool canWalkOnCeiling = true;
            
            public float WanderSpeed => wanderSpeed;
            public float AggroSpeed => aggroSpeed;
            public float GravityScale => gravityScale;
            public bool CanWalkOnWalls => canWalkOnWalls;
            public bool CanWalkOnCeiling => canWalkOnCeiling;
        }
        
        [Serializable]
        public class AIVariables
        {
            [SerializeField] private float detectionRange = 10f;
            [SerializeField] private float attackRange = 8f;
            [SerializeField] private LayerMask playerLayerMask;
            [SerializeField] private float wanderDirectionChangeInterval = 3f;
            [SerializeField, NonEditable] private float wanderTimer;
            [SerializeField, NonEditable] private Transform targetPlayer;
            [SerializeField, NonEditable] private bool isAggro;
            
            public float DetectionRange => detectionRange;
            public float AttackRange => attackRange;
            public LayerMask PlayerLayerMask => playerLayerMask;
            public float WanderDirectionChangeInterval => wanderDirectionChangeInterval;
            public float WanderTimer { get { return wanderTimer; } set { wanderTimer = value; } }
            public Transform TargetPlayer { get { return targetPlayer; } set { targetPlayer = value; } }
            public bool IsAggro { get { return isAggro; } set { isAggro = value; } }
        }
        
        [Serializable]
        public class AttackVariables
        {
            [SerializeField] private GameObject projectilePrefab;
            [SerializeField] private int projectileCount = 8;
            [SerializeField] private float projectileSpeed = 5f;
            [SerializeField] private float projectileSpeedRotation = 180f;
            [SerializeField] private float attackCooldown = 2f;
            [SerializeField] private float chargeTime = 1f;
            [SerializeField, NonEditable] private float attackCooldownTimer;
            [SerializeField, NonEditable] private bool isCharging;
            [SerializeField, NonEditable] private float chargeTimer;
            
            public GameObject ProjectilePrefab => projectilePrefab;
            public int ProjectileCount => projectileCount;
            public float ProjectileSpeed => projectileSpeed;
            public float ProjectileSpeedRotation => projectileSpeedRotation;
            public float AttackCooldown => attackCooldown;
            public float ChargeTime => chargeTime;
            public float AttackCooldownTimer { get { return attackCooldownTimer; } set { attackCooldownTimer = value; } }
            public bool IsCharging { get { return isCharging; } set { isCharging = value; } }
            public float ChargeTimer { get { return chargeTimer; } set { chargeTimer = value; } }
        }
        
        #endregion
        
        [Space(7)]
        public PhysicsVariables Physics;
        public MovementVariables Movement;
        public AIVariables AI;
        public AttackVariables Attack;
    }
}
