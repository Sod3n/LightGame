using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy.States
{
    public class SlimeAttackState : EnemyState
    {
        private bool hasAttacked;
        
        public SlimeAttackState(SlimeEnemy enemy, EnemyStateMachine stateMachine, EnemyData enemyData) 
            : base(enemy, stateMachine, enemyData)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            hasAttacked = false;
            enemyData.Attack.IsCharging = true;
            enemyData.Attack.ChargeTimer = 0f;
            
            // Stop movement during attack
            enemy.Movement.Stop();
            
            // Freeze rotation during attack
            enemy.Rigidbody2D.freezeRotation = true;
            Debug.Log($"[AttackState] Enter - Rotation frozen at {enemy.transform.rotation.eulerAngles.z} degrees");
            
            // Notify view that charge has started
            ((SlimeEnemy)enemy).InvokeChargeStart(enemyData.Attack.ChargeTime);
        }
        
        public override void Update()
        {
            base.Update();
            
            // Charge up the attack
            if (enemyData.Attack.IsCharging && enemyData.Attack.ChargeTimer >= enemyData.Attack.ChargeTime)
            {
                FireProjectiles();
                hasAttacked = true;
                enemyData.Attack.IsCharging = false;
                
                // Notify view that charge has stopped and attack is fired
                ((SlimeEnemy)enemy).InvokeChargeStop();
                ((SlimeEnemy)enemy).InvokeAttack();
            }
        }
        
        private void FireProjectiles()
        {
            if (enemyData.Attack.ProjectilePrefab == null)
            {
                Debug.LogWarning("Projectile prefab not assigned!");
                return;
            }
            
            int projectileCount = enemyData.Attack.ProjectileCount;
            float angleStep = 360f / projectileCount;
            
            // Direction to player for one projectile
            Vector2 directionToPlayer = Vector2.zero;
            if (enemyData.AI.TargetPlayer != null)
            {
                directionToPlayer = (enemyData.AI.TargetPlayer.position - enemy.transform.position).normalized;
            }
            
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = i * angleStep;
                Vector2 direction;
                
                // One projectile always points to player
                if (i == 0 && enemyData.AI.TargetPlayer != null)
                {
                    direction = directionToPlayer;
                }
                else
                {
                    // Calculate direction based on angle
                    float radians = angle * Mathf.Deg2Rad;
                    direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                }
                
                // Instantiate projectile
                GameObject projectileObj = Object.Instantiate(
                    enemyData.Attack.ProjectilePrefab, 
                    enemy.transform.position, 
                    Quaternion.identity
                );
                
                // Set parent to scene root if available
                if (SceneRoot.Instance != null)
                {
                    projectileObj.transform.SetParent(SceneRoot.Instance.transform, true);
                }
                
                // Set up projectile - try EnemyProjectile first, then Projectile, then Rigidbody2D
                var enemyProjectile = projectileObj.GetComponent<EnemyProjectile>();
                if (enemyProjectile != null)
                {
                    enemyProjectile.Initialize(direction, enemyData.Attack.ProjectileSpeed, enemyData.Attack.ProjectileSpeedRotation);
                }
                else
                {
                    var projectile = projectileObj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        projectile.Direction = direction;
                        projectile.Speed = enemyData.Attack.ProjectileSpeed;
                        projectile.SpeedRotation = enemyData.Attack.ProjectileSpeedRotation;
                    }
                    else
                    {
                        // If using Rigidbody2D directly
                        var rb = projectileObj.GetComponent<Rigidbody2D>();
                        if (rb != null)
                        {
                            rb.linearVelocity = direction * enemyData.Attack.ProjectileSpeed;
                        }
                    }
                }
            }
            
            // Reset attack cooldown
            inputManager.ResetAttackCooldown();
        }
        
        protected override void SwitchStateLogic()
        {
            base.SwitchStateLogic();
            
            // Return to appropriate state after attack
            if (hasAttacked)
            {
                if (enemyData.AI.IsAggro)
                {
                    stateMachine.ChangeState(enemy.AggroState);
                }
                else
                {
                    stateMachine.ChangeState(enemy.WanderState);
                }
            }
        }
        
        public override void Exit()
        {
            base.Exit();
            enemyData.Attack.IsCharging = false;
            enemyData.Attack.ChargeTimer = 0f;
            
            // Unfreeze rotation when leaving attack state (for wall walking)
            if (enemyData.Movement.CanWalkOnWalls)
            {
                enemy.Rigidbody2D.freezeRotation = false;
            }
            Debug.Log($"[AttackState] Exit - Rotation unfrozen");
            
            // Ensure charge is stopped when exiting (in case it was interrupted)
            ((SlimeEnemy)enemy).InvokeChargeStop();
        }
    }
}
