using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class StateMachine<T> where T : MonoBehaviour
    {
        private BaseState<T> currentState;

        public void ChangeState(BaseState<T> newState, T owner)
        {
            currentState?.ExitState(owner);

            currentState = newState;

            currentState?.EnterState(owner);
        }

        public void Update(T owner)
        {
            currentState?.UpdateState(owner);
        }
    }   
}
