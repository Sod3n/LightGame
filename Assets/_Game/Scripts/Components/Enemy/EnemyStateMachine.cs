namespace Light_and_controller.Scripts.Components.Enemy
{
    [System.Serializable]
    public class EnemyStateMachine
    {
        public EnemyState CurrentState { get; private set; }
        
        public void Initialize(EnemyState startingState)
        {
            CurrentState = startingState;
            CurrentState.Enter();
        }
        
        public void ChangeState(EnemyState newState)
        {
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }
    }
}
