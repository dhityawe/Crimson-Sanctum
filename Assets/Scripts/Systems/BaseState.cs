using UnityEngine;

namespace Assets.Scripts.Systems
{
    /// <summary>
    /// Base interface for states used in a state machine.
    /// </summary>
    /// <typeparam name="T">The type of MonoBehaviour that the state is associated with.</typeparam>
    public interface BaseState<T> where T : MonoBehaviour
    {
        void EnterState(T owner);
        void UpdateState(T owner);
        void ExitState(T owner);
    }
}
