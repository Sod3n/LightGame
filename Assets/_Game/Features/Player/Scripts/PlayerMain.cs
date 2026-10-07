using System;
using UnityEngine;

// Nested inside the GeneralLevel prefab; without this a Scene view click selects the whole level rig.
[SelectionBase]
public class PlayerMain : MonoBehaviour
{
    public PlayerStateMachine _stateMachine; // State Machine declaration where we change current state
    [NonEditable, Space(5)] public AnimName CurrentState; // Variable to display the current state in the Unity inspector for debugging purposes.
    public MainState IdleState, WalkState, JumpState, LandState, DashState, CrouchIdleState, CrouchWalkState, WallGrabState, WallClimbState, WallJumpState, WallSlideState, DirectionalJumpState ; // State declarations
    public PlayerOneShotState AttackState, HitState, DeathState, LandImpactState; // One-shot presentation states (triggered from gameplay / touchdown, not movement input)
    public enum AnimName { Idle, Walk, Jump, ExtraJump1, ExtraJump2, Land, Dash, CrouchIdle, CrouchWalk, WallGrab, WallClimb, WallJump, WallSlide, DirectionalJump, Attack, Hit, Death, LandImpact } // Enum declaration of state names as animator parameters

    [NonSerialized] public Animator Animator; // The Animator is used to control the player's animations based on their current state.
    [NonSerialized] public Rigidbody2D Rigidbody2D; // The Rigidbody2D is used to control movement based on velocity vector.
    [NonSerialized] public PlayerInputManager InputManager; // The PlayerInputManager handles all user input and sends it to the state machine.
    [NonSerialized] public CapsuleCollider2D CapsuleCollider2D; // CapsuleCollider2D is used to handle slopes and define the ground check position in the base state class: "State.cs".

    public PlayerData PlayerData; // All player movement and action data is stored in the PlayerData object.
    
    // Push/drag state - set by PushableObject when player is interacting with it
    [NonSerialized] public float PushObjectSlowdown = 1f; // Speed multiplier when pushing/dragging objects (1 = no slowdown)
    [NonSerialized] public bool IsPushingObject = false; // Whether player is currently pushing/dragging an object

    private void Awake()
    {
        Debug.Log($"[TeleportDebug] PlayerMain.Awake: instanceID={GetInstanceID()}, position={transform.position}, scene='{gameObject.scene.name}', frame={Time.frameCount}");

        // Declaration of necessary components:
        // Animator for controlling character animations,
        // Rigidbody2D for physics simulation,
        // InputManager for handling player input,
        // CapsuleCollider2D for slope detection and ground checking.
        Animator = GetComponent<Animator>();
        Rigidbody2D = GetComponent<Rigidbody2D>();
        InputManager = GetComponent<PlayerInputManager>();
        CapsuleCollider2D = GetComponent<CapsuleCollider2D>();

        // In this section, we assign all states
        _stateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, _stateMachine, AnimName.Idle, PlayerData);
        WalkState = new PlayerWalkState(this, _stateMachine, AnimName.Walk, PlayerData);
        JumpState = new PlayerJumpState(this, _stateMachine, AnimName.Jump, PlayerData);
        LandState = new PlayerLandState(this, _stateMachine, AnimName.Land, PlayerData);
        DashState = new PlayerDashState(this, _stateMachine, AnimName.Dash, PlayerData);
        CrouchIdleState = new PlayerCrouchIdleState(this, _stateMachine, AnimName.CrouchIdle, PlayerData);
        CrouchWalkState = new PlayerCrouchWalkState(this, _stateMachine, AnimName.CrouchWalk, PlayerData);
        WallGrabState = new PlayerWallGrabState(this, _stateMachine, AnimName.WallGrab, PlayerData);
        WallClimbState = new PlayerWallClimbState(this, _stateMachine, AnimName.WallClimb, PlayerData);
        WallJumpState = new PlayerWallJumpState(this, _stateMachine, AnimName.WallJump, PlayerData);
        WallSlideState = new PlayerWallSlideState(this, _stateMachine, AnimName.WallSlide, PlayerData);
        DirectionalJumpState = new PlayerDirectionalJumpState(this, _stateMachine, AnimName.DirectionalJump, PlayerData);

        // One-shot presentation states. They play their clip once (watching the Animator
        // for completion) and then hand control back to Idle.
        AttackState = new PlayerOneShotState(this, _stateMachine, AnimName.Attack, PlayerData, "PlayerAttack",
            pinInPlace: true);
        HitState = new PlayerOneShotState(this, _stateMachine, AnimName.Hit, PlayerData, "PlayerHit",
            pinInPlace: true, locksStateMachine: true);
        DeathState = new PlayerOneShotState(this, _stateMachine, AnimName.Death, PlayerData, "PlayerDeath",
            pinInPlace: true, locksStateMachine: true, returnToIdleOnFinish: false);

        // Brief landing-impact "stun" the fall (Land) state exits into on touchdown, before Idle/Walk.
        // pinInPlace freezes the player so the whole clip reads as a proper landing (kept short via the
        // PlayerLandImpact state Speed). Not locked, so it doesn't wipe the jump buffer on finish.
        LandImpactState = new PlayerOneShotState(this, _stateMachine, AnimName.LandImpact, PlayerData, "PlayerLandImpact",
            pinInPlace: true);
    }

    // --- One-shot animation triggers (call these from gameplay code) ---

    /// <summary>Play the attack animation once, then return to Idle. Ignored while dead.</summary>
    public void PlayAttack()
    {
        if (CurrentState == AnimName.Death) return;
        _stateMachine.ChangeState(AttackState);
    }

    /// <summary>Play the hit/hurt reaction once, then return to Idle. Ignored while dead.</summary>
    public void PlayHit()
    {
        if (CurrentState == AnimName.Death) return;
        _stateMachine.ChangeState(HitState);
    }

    /// <summary>
    /// Play the death animation, then invoke <paramref name="onComplete"/> once it finishes
    /// (used to gate respawn/teleport until the animation has played). Ignored if already dead.
    /// </summary>
    public void PlayDeath(System.Action onComplete = null)
    {
        if (CurrentState == AnimName.Death) return;
        DeathState.OnComplete = onComplete;
        _stateMachine.ChangeState(DeathState, force: true); // death overrides any current (even locked) state
    }

    /// <summary>
    /// Reset the player after a respawn: release the death/one-shot lock and return to Idle.
    /// Called by the health system when the player is revived (healed from a dead state).
    /// </summary>
    public void OnRespawn()
    {
        ClearPendingMovementIntent();
        _stateMachine.Unlock();
        _stateMachine.ChangeState(IdleState, force: true);
    }

    /// <summary>
    /// Wipe any queued jump/buffer state. A hazard's jump pad may have set NewJump (etc.)
    /// right before a locking one-shot blocked its state change, and that stale intent
    /// would otherwise fire a spurious jump the moment movement control resumes.
    /// </summary>
    public void ClearPendingMovementIntent()
    {
        PlayerData.Jump.NewJump = false;
        PlayerData.Jump.NextJumpInt = 1;
        PlayerData.Jump.JumpBufferTimer = 0f;
        PlayerData.Jump.CoyoteTimeTimer = 0f;
        PlayerData.Walls.WallJump.JumpBufferTimer = 0f;
        PlayerData.Walls.WallJump.CoyoteTimeTimer = 0f;
    }

    private void Start()
    {
        _stateMachine.Initialize(IdleState); //Here, we assign the starting state

        // The Derivative function is used to calculate the derivative of the animation curves,
        // which is necessary to obtain the velocity curve for the corresponding movement.
        // Here, we assign the derivative of each curve to its corresponding variable.
        // Basically, we get derivative of height curves to obtain velocity curves.
        PlayerData.Jump.Jumps[0].JumpVelocityCurve = PlayerData.Jump.Jumps[0].JumpHeightCurve.Derivative();
        foreach (var jump in PlayerData.Jump.Jumps)
        {
            jump.JumpVelocityCurve = jump.JumpHeightCurve.Derivative();
        }
        PlayerData.Land.LandVelocityCurve = PlayerData.Land.LandHeightCurve.Derivative();
        PlayerData.Dash.DashYVelocityCurve = PlayerData.Dash.DashHeightCurve.Derivative();
        PlayerData.Walls.WallJump.JumpVelocityCurve = PlayerData.Walls.WallJump.JumpHeightCurve.Derivative();
    }
    private void Update()
    {
        _stateMachine.CurrentState.Update(); // Update method of current state at runtime
    }

    private void FixedUpdate()
    {
        _stateMachine.CurrentState.FixedUpdate(); // FixedUpdate method of current state at runtime
    }
}
