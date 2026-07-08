using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy.States
{
    public class SlimeWanderState : EnemyState
    {
        public SlimeWanderState(SlimeEnemy enemy, EnemyStateMachine stateMachine, EnemyData enemyData) 
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
            float speed = enemyData.Movement.WanderSpeed * inputManager.Input_Walk;
            enemy.Movement.Move(speed);
        }
        
        protected override void SwitchStateLogic()
        {
            base.SwitchStateLogic();
            
            // Switch to aggro state if player detected
            if (enemyData.AI.IsAggro)
            {
                stateMachine.ChangeState(enemy.AggroState);
            }
        }
    }
}
