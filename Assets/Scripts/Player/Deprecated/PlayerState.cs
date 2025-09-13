namespace Assets.Scripts.Player
{
    public abstract class PlayerBaseState
    {
        protected PlayerController player;
        protected PlayerStateMachine stateMachine;

        protected PlayerBaseState(PlayerController player, PlayerStateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void HandleInput() { }
        public virtual void LogicUpdate() { }
        public virtual void PhysicsUpdate() { }
        public virtual void Exit() { }
    }

    public class PlayerStateMachine
    {
        public PlayerBaseState CurrentState { get; private set; }

        public void Initialize(PlayerBaseState startingState)
        {
            CurrentState = startingState;
            CurrentState.Enter();
        }

        public void ChangeState(PlayerBaseState newState)
        {
            CurrentState.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }
    }
}
