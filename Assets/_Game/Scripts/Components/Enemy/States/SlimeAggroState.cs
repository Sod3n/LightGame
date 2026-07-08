using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy.States
{
    public class SlimeAggroState : EnemyState
    {
        public SlimeAggroState(SlimeEnemy enemy, EnemyStateMachine stateMachine, EnemyData enemyData) 
            : base(enemy, stateMachine, enemyData)
        {
        }
        
        public override void Enter()
        {
            base.Enter();
            // Don't set gravityScale here - it's configured in SlimeEnemy.Start() based on CanWalkOnWalls
        }
        
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            
            // Use shared movement system
            float speed = enemyData.Movement.AggroSpeed * inputManager.Input_Walk;
            enemy.Movement.Move(speed);
        }
        
        protected override void SwitchStateLogic()
        {
            base.SwitchStateLogic();
            
            // Switch to attack state if attack input is triggered
            if (inputManager.Input_Attack && enemyData.Attack.AttackCooldownTimer <= 0f)
            {
                stateMachine.ChangeState(enemy.AttackState);
            }
            // Switch back to wander if player is lost
            else if (!enemyData.AI.IsAggro)
            {
                stateMachine.ChangeState(enemy.WanderState);
            }
        }
    }
}
