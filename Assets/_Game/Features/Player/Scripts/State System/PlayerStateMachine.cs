[System.Serializable]
public class PlayerStateMachine
{
    // Declaration of current runtime state
    public MainState CurrentState { get; private set; }

    // While locked, normal (non-forced) state changes are ignored. Used by one-shot
    // presentation states (Hit/Death) so external systems - jump pads, knockback, the
    // movement logic - can't cancel them mid-animation.
    public bool IsLocked { get; private set; }

    // Function to initialize starting state
    public void Initialize(MainState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    // Function to change current state.
    // force=true bypasses the lock (the one-shot states use it to enter/exit themselves,
    // and death uses it to take priority over everything else).
    public void ChangeState(MainState newState, bool force = false)
    {
        if (IsLocked && !force)
            return;

        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void Lock() => IsLocked = true;
    public void Unlock() => IsLocked = false;
}
