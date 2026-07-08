using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy
{
    /// <summary>
    /// AI-driven input manager for enemy behavior
    /// Simulates player input based on AI decisions
    /// </summary>
    public class EnemyInputManager : MonoBehaviour
    {
        private SlimeEnemy enemy;
        private EnemyData enemyData;
        
        [SerializeField, NonEditable] public float input_Walk;
        [SerializeField, NonEditable] private bool input_Attack;
        
        public float Input_Walk => input_Walk;
        public bool Input_Attack => input_Attack;
        
        public void Initialize(SlimeEnemy enemy, EnemyData enemyData)
        {
            this.enemy = enemy;
            this.enemyData = enemyData;
        }
        
        private void FixedUpdate()
        {
            if (enemy == null || enemyData == null) return;
            
            UpdateAI();
        }
        
        private void UpdateAI()
        {
            // Detect player
            DetectPlayer();
            
            // Update timers
            UpdateTimers();
            
            // Decide movement and actions
            if (enemyData.AI.IsAggro && enemyData.AI.TargetPlayer != null)
            {
                HandleAggroState();
            }
            else
            {
                HandleWanderState();
            }
        }
        
        private void DetectPlayer()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                enemy.transform.position, 
                enemyData.AI.DetectionRange, 
                enemyData.AI.PlayerLayerMask
            );
            
            if (hits.Length > 0)
            {
                enemyData.AI.TargetPlayer = hits[0].transform;
                enemyData.AI.IsAggro = true;
            }
            else
            {
                enemyData.AI.TargetPlayer = null;
                enemyData.AI.IsAggro = false;
            }
        }
        
        private void HandleAggroState()
        {
            if (enemyData.AI.TargetPlayer == null) return;
            
            // Move towards player
            float directionToPlayer = Mathf.Sign(enemyData.AI.TargetPlayer.position.x - enemy.transform.position.x);
            input_Walk = directionToPlayer;
            
            // Check if in attack range
            float distanceToPlayer = Vector2.Distance(enemy.transform.position, enemyData.AI.TargetPlayer.position);
            if (distanceToPlayer <= enemyData.AI.AttackRange && enemyData.Attack.AttackCooldownTimer <= 0f)
            {
                input_Attack = true;
            }
            else
            {
                input_Attack = false;
            }
        }
        
        private void HandleWanderState()
        {
            // Random wandering behavior
            enemyData.AI.WanderTimer -= Time.fixedDeltaTime;
            
            if (enemyData.AI.WanderTimer <= 0f)
            {
                // Change direction randomly
                float randomChoice = Random.Range(0f, 1f);
                if (randomChoice < 0.33f)
                {
                    input_Walk = -1f; // Move left
                }
                else if (randomChoice < 0.66f)
                {
                    input_Walk = 1f; // Move right
                }
                else
                {
                    input_Walk = 0f; // Stop
                }
                
                enemyData.AI.WanderTimer = enemyData.AI.WanderDirectionChangeInterval;
            }
            
            // Don't attack while wandering
            input_Attack = false;
        }
        
        private void UpdateTimers()
        {
            enemyData.Attack.AttackCooldownTimer = Mathf.Max(0f, enemyData.Attack.AttackCooldownTimer - Time.fixedDeltaTime);
            
            if (enemyData.Attack.IsCharging)
            {
                enemyData.Attack.ChargeTimer += Time.fixedDeltaTime;
            }
        }
        
        public void ResetAttackCooldown()
        {
            enemyData.Attack.AttackCooldownTimer = enemyData.Attack.AttackCooldown;
        }
    }
}
