using UnityEngine;

namespace Light_and_controller.Scripts.Components.Enemy
{
    public class EnemyState
    {
        protected SlimeEnemy enemy;
        protected EnemyStateMachine stateMachine;
        protected Rigidbody2D rigidbody2D;
        protected EnemyData enemyData;
        protected EnemyInputManager inputManager;
        protected float localTime;
        
        public EnemyState(SlimeEnemy enemy, EnemyStateMachine stateMachine, EnemyData enemyData)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
            this.enemyData = enemyData;
            this.inputManager = enemy.InputManager;
        }
        
        public virtual void Enter()
        {
            rigidbody2D = enemy.Rigidbody2D;
            localTime = 0f;
        }
        
        public virtual void Update()
        {
            localTime += Time.deltaTime;
            SwitchStateLogic();
        }
        
        public virtual void FixedUpdate()
        {
            PhysicsCheck();
        }
        
        public virtual void Exit()
        {
            localTime = 0f;
        }
        
        protected virtual void PhysicsCheck()
        {
            // Ground check
            CheckGround();
            
            // Wall check
            CheckWall();
            
            // Set facing direction
            if (inputManager.Input_Walk != 0)
            {
                enemyData.Physics.FacingDirection = (int)Mathf.Sign(inputManager.Input_Walk);
            }
        }
        
        protected void CheckGround()
        {
            Vector2 checkPosition = (Vector2)enemy.transform.position + enemyData.Physics.GroundCheckPosition;
            RaycastHit2D hit = Physics2D.Raycast(checkPosition, -enemy.transform.up, 0.5f, enemyData.Physics.GroundLayerMask);
            enemyData.Physics.IsGrounded = hit.collider != null;
            
            if (hit.collider != null)
            {
                enemyData.Physics.WalkSpeedDirection = Vector2.Perpendicular(hit.normal);
            }
        }
        
        protected void CheckWall()
        {
            Vector2 checkPosition = enemy.transform.position;
            RaycastHit2D hitRight = Physics2D.Raycast(checkPosition, enemy.transform.right, 0.5f, enemyData.Physics.WallLayerMask);
            RaycastHit2D hitLeft = Physics2D.Raycast(checkPosition, -enemy.transform.right, 0.5f, enemyData.Physics.WallLayerMask);
            
            if (hitRight.collider != null)
            {
                enemyData.Physics.IsNextToWall = true;
                enemyData.Physics.WallDirection = 1;
            }
            else if (hitLeft.collider != null)
            {
                enemyData.Physics.IsNextToWall = true;
                enemyData.Physics.WallDirection = -1;
            }
            else
            {
                enemyData.Physics.IsNextToWall = false;
                enemyData.Physics.WallDirection = 0;
            }
        }
        
        protected virtual void SwitchStateLogic() { }
    }
}
