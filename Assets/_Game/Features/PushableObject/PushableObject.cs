using System;
using Light_and_controller.Scripts.Components;
using Light_and_controller.Scripts.Components.PushableObject;
using Light_and_controller.Scripts.Systems;
using UnityEngine;

public class PushableObject : PlayerMain
{
    private void Awake()
    {
        // Declaration of necessary components:
        // Animator for controlling character animations,
        // Rigidbody2D for physics simulation,
        // InputManager for handling player input,
        // CapsuleCollider2D for slope detection and ground checking.
        Animator = GetComponent<Animator>();
        Rigidbody2D = GetComponent<Rigidbody2D>();
        InputManager = GetComponent<ManualInputManager>();
        CapsuleCollider2D = GetComponent<CapsuleCollider2D>();

        // In this section, we assign all states
        _stateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, _stateMachine, AnimName.Idle, PlayerData);
        WalkState = new PlayerWalkState(this, _stateMachine, AnimName.Walk, PlayerData);
        JumpState = new PlayerJumpState(this, _stateMachine, AnimName.Jump, PlayerData);
        LandState = new PlayerLandState(this, _stateMachine, AnimName.Land, PlayerData);
        // DashState = new PlayerDashState(this, _stateMachine, AnimName.Dash, PlayerData);
        // CrouchIdleState = new PlayerCrouchIdleState(this, _stateMachine, AnimName.CrouchIdle, PlayerData);
        // CrouchWalkState = new PlayerCrouchWalkState(this, _stateMachine, AnimName.CrouchWalk, PlayerData);
        // WallGrabState = new PlayerWallGrabState(this, _stateMachine, AnimName.WallGrab, PlayerData);
        // WallClimbState = new PlayerWallClimbState(this, _stateMachine, AnimName.WallClimb, PlayerData);
        // WallJumpState = new PlayerWallJumpState(this, _stateMachine, AnimName.WallJump, PlayerData);
        WallSlideState = new PlayerWallSlideState(this, _stateMachine, AnimName.WallSlide, PlayerData);

        baseMaxSpeed = PlayerData.Walk.MaxSpeed;
        
        // Get object weight from Weight component
        var weightComponent = GetComponent<Weight>();
        if (weightComponent != null)
        {
            _objectWeight = weightComponent.Get();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} is missing Weight component. Using default weight of 1.");
            _objectWeight = 1f;
        }
        
        // Safety check for weight
        if (_objectWeight <= 0f || float.IsNaN(_objectWeight))
        {
            Debug.LogWarning($"{gameObject.name} has invalid weight: {_objectWeight}. Using default weight of 1.");
            _objectWeight = 1f;
        }
    }

    private PlayerMain _lastPlayer;
    private float baseMaxSpeed;
    private float _objectWeight;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!other.TryGetComponent(out PlayerMain newEntry)) return;
        _lastPlayer = newEntry;
    }

    private void LateUpdate()
    {
        if(!_lastPlayer) return;
        if(!_lastPlayer.PlayerData.Physics.IsGrounded) return;
        
        var playerDir = Mathf.Sign(_lastPlayer.InputManager.Input_Walk);
        var dir = Mathf.Sign((transform.position - _lastPlayer.transform.position).x);
        var isSameDir = Mathf.Approximately(playerDir, dir);
        var isStopping = Mathf.Approximately(_lastPlayer.InputManager.Input_Walk, 0);
        var isCrouching = _lastPlayer.CurrentState == AnimName.CrouchWalk;
        
        // When crxouching, player can drag the cube in any direction (to get it out of corners)
        // When standing, player can only push (same direction)
        if(!isCrouching && !isSameDir && !isStopping) return;
        
        // Check if player has push strength component and can push this object
        var pushStrength = _lastPlayer.GetComponent<PushStrength>();
        if (pushStrength != null && !pushStrength.CanPush(_objectWeight))
        {
            // Player is too weak to push/drag this object
            InputManager.input_Walk = 0;
            return;
        }
        
        // Calculate push speed based on weight and push strength
        float pushSpeedMultiplier = 1f;
        if (pushStrength != null)
        {
            pushSpeedMultiplier = pushStrength.GetPushSpeed(_objectWeight);
            
            // Safety check for invalid values
            if (float.IsNaN(pushSpeedMultiplier) || float.IsInfinity(pushSpeedMultiplier))
            {
                pushSpeedMultiplier = 1f;
                Debug.LogWarning($"Invalid push speed multiplier calculated for {gameObject.name}. Using default value.");
            }
        }
        
        // Set push state on player for states to query
        _lastPlayer.PushObjectSlowdown = pushSpeedMultiplier;
        _lastPlayer.IsPushingObject = true;
        
        // Cube copies player's actual velocity for perfect sync
        float playerXVelocity = Mathf.Abs(_lastPlayer.Rigidbody2D.linearVelocity.x);
        PlayerData.Walk.MaxSpeed = playerXVelocity * 1.2f;
    
        InputManager.input_Walk = _lastPlayer.InputManager.Input_Walk;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(!_lastPlayer || other.gameObject != _lastPlayer.gameObject) return;
        InputManager.input_Walk = 0;
        
        // Reset push state on player
        _lastPlayer.PushObjectSlowdown = 1f;
        _lastPlayer.IsPushingObject = false;
        _lastPlayer = null;
    }

    private void OnDisable()
    {
        InputManager.input_Walk = 0;
        _stateMachine.ChangeState(IdleState);
    }
}