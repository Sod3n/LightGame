using System;
using UnityEngine;

// A generic, non-looping "overlay" state (Attack / Hit / Death) that plays its clip
// once and then hands control back to Idle. It reuses the exact same bool-driven
// animator convention as every movement state (Enter sets the bool true, Exit sets
// it false), so Entry routes into it and it self-exits when the bool clears.
//
// Movement physics are intentionally NOT run while a one-shot plays, so it cannot be
// interrupted by jump-buffer / state-switch logic mid-animation. It is a presentation
// state; gameplay code triggers it via PlayerMain.PlayAttack/PlayHit/PlayDeath.
public class PlayerOneShotState : MainState
{
    private readonly string _animatorStateName;      // e.g. "PlayerAttack" - the Animator state to watch for completion
    private readonly float _fallbackTimeout;         // safety: force return even if the Animator never reports completion
    private readonly bool _pinInPlace;               // zero velocity every step - fully freezes in place (also cancels gravity)
    private readonly bool _locksStateMachine;        // block external state changes (jump pads, knockback) while this plays
    private readonly bool _returnToIdleOnFinish;     // false for Death: stay locked/frozen until an external respawn resets us

    private bool _enteredClip; // becomes true once the Animator has actually entered our state
    private bool _finished;    // guards the completion handler so it runs exactly once

    // Optional callback fired once when the clip finishes (or the fallback timeout hits).
    // Set per-invocation, cleared automatically. Used to gate death respawn on the animation.
    public Action OnComplete;

    public PlayerOneShotState(PlayerMain player, PlayerStateMachine stateMachine, PlayerMain.AnimName animEnum, PlayerData playerData,
        string animatorStateName, bool pinInPlace = false, bool locksStateMachine = false,
        bool returnToIdleOnFinish = true, float fallbackTimeout = 3f)
        : base(player, stateMachine, animEnum, playerData)
    {
        _animatorStateName = animatorStateName;
        _pinInPlace = pinInPlace;
        _locksStateMachine = locksStateMachine;
        _returnToIdleOnFinish = returnToIdleOnFinish;
        _fallbackTimeout = fallbackTimeout;
    }

    public override void Enter()
    {
        base.Enter();
        _enteredClip = false;
        _finished = false;

        // Take exclusive control so external systems can't cancel this one-shot.
        if (_locksStateMachine)
            stateMachine.Lock();

        // Kill any carried-over motion so the one-shot plays in place.
        if (_pinInPlace)
            player.Rigidbody2D.linearVelocity = Vector2.zero;
    }

    public override void SwitchStateLogic()
    {
        var info = player.Animator.GetCurrentAnimatorStateInfo(0);

        if (info.IsName(_animatorStateName))
        {
            _enteredClip = true;
            // Non-looping clip: normalizedTime reaches (and clamps at) 1 when it finishes.
            if (info.normalizedTime >= 1f)
            {
                Finish();
                return;
            }
        }

        // Safety net: if the Animator never enters the expected state (mis-wired param, etc.)
        // don't leave the player stuck here forever.
        if (!_enteredClip && localTime >= _fallbackTimeout)
            Finish();
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;

        // Fire and clear the callback first, so listeners (e.g. the death respawn/fade)
        // only run once the animation has actually played through.
        var callback = OnComplete;
        OnComplete = null;
        callback?.Invoke();

        if (_returnToIdleOnFinish)
        {
            if (_locksStateMachine)
            {
                // Drop any jump intent a hazard queued while we held the lock, so we don't
                // pop straight into a jump the instant movement control resumes.
                player.ClearPendingMovementIntent();
                stateMachine.Unlock();
            }
            // Going straight to Walk avoids a one-frame idle pose when a direction is held.
            var walking = inputManager.Input_Walk != 0 && !playerData.Physics.Slope.StayStill;
            stateMachine.ChangeState(walking ? player.WalkState : player.IdleState, force: true);
        }
        // Otherwise (Death) we stay here, locked and frozen, until PlayerMain.OnRespawn releases us.
    }

    // Pinned states stay frozen every physics step - this also cancels gravity and defeats
    // external forces (jump-pad launches, knockback) for the duration of the one-shot.
    public override void FixedUpdate()
    {
        if (_pinInPlace)
            player.Rigidbody2D.linearVelocity = Vector2.zero;
    }
}
