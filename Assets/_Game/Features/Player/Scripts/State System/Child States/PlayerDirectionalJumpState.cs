using UnityEngine;

public class PlayerDirectionalJumpState : MainState
{
    private Vector2 launchDirection;
    private float launchForce;
    private float jumpDuration;
    private bool preservePerpendicularVelocity;
    private PlayerData.JumpVariables.JumpInfo jumpInfo;
    private int jumpIndex;
    
    public PlayerDirectionalJumpState(PlayerMain player, PlayerStateMachine stateMachine, PlayerMain.AnimName animEnum, PlayerData playerData) : base(player, stateMachine, animEnum, playerData)
    {
    }

    public void SetLaunchParameters(Vector2 direction, float force, float duration, bool preservePerpendicular, int jumpIdx)
    {
        launchDirection = direction.normalized;
        launchForce = force;
        jumpDuration = duration;
        preservePerpendicularVelocity = preservePerpendicular;
        jumpIndex = jumpIdx;
        Debug.Log($"[DirectionalJump] SetLaunchParameters - Direction: {launchDirection}, Force: {force}, Duration: {duration}, JumpIndex: {jumpIdx}");
    }

    public override void Enter()
    {
        base.Enter();
        
        Debug.Log($"[DirectionalJump] Enter - Starting directional jump");
        
        // Get jump info for curve-based velocity
        jumpInfo = playerData.Jump.Jumps[jumpIndex - 1];
        
        // Note: DirectionalJump animation parameter needs to be added to Animator if you want animations
        // player.Animator.SetBool(_animEnum.ToString(), true);
        
        // Enable gravity for natural arc
        rigidbody2D.gravityScale = 1f;
        
        playerData.Jump.JumpBufferTimer = 0;
        playerData.Jump.NewJump = false;
    }

    public override void Update()
    {
        base.Update();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        
        // Calculate velocity using jump curve for dynamic movement
        Vector2 newVelocity = Vector2.zero;
        
        if (localTime <= jumpDuration)
        {
            // Use the jump velocity curve for dynamic acceleration
            float curveValue = jumpInfo.JumpVelocityCurve.Evaluate(localTime / jumpDuration);
            float speed = curveValue * launchForce / jumpDuration;
            
            newVelocity = launchDirection * speed;
            
            // Preserve perpendicular velocity if enabled
            if (preservePerpendicularVelocity)
            {
                Vector2 perpendicular = new Vector2(-launchDirection.y, launchDirection.x);
                float perpendicularSpeed = Vector2.Dot(rigidbody2D.linearVelocity, perpendicular);
                newVelocity += perpendicular * perpendicularSpeed;
            }
        }
        
        rigidbody2D.linearVelocity = newVelocity;
        rigidbody2D.linearVelocity += playerData.Physics.Platform.DampedVelocity;
    }

    public override void Exit()
    {
        base.Exit();
        // Note: DirectionalJump animation parameter needs to be added to Animator if you want animations
        // player.Animator.SetBool(_animEnum.ToString(), false);
        Debug.Log($"[DirectionalJump] Exit - Leaving directional jump state after {localTime}s");
    }

    public override void PhysicsCheck()
    {
        base.PhysicsCheck();
    }

    public override void SwitchStateLogic()
    {
        base.SwitchStateLogic();

        if (localTime >= jumpDuration || (playerData.Physics.IsOnHeadBump && playerData.Physics.CanBumpHead))
        {
            Debug.Log($"[DirectionalJump] Transitioning to LandState - Time: {localTime}/{jumpDuration}, HeadBump: {playerData.Physics.IsOnHeadBump}");
            stateMachine.ChangeState(player.LandState);
        }
        else if (inputManager.Input_Dash && playerData.Dash.DashCooldownTimer <= 0f)
        {
            stateMachine.ChangeState(player.DashState);
        }
        else if (playerData.Physics.IsNextToWall)
        {
            if (inputManager.Input_WallGrab)
            {
                stateMachine.ChangeState(player.WallGrabState);
            }
            else if (localTime > playerData.Jump.IgnoreWallSlideTime && inputManager.Input_Walk != 0 && Mathf.Sign(inputManager.Input_Walk) == Mathf.Sign(playerData.Physics.WallDirection))
            {
                stateMachine.ChangeState(player.WallSlideState);
            }
        }
    }
}
