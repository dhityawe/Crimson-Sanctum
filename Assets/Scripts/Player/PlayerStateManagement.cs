using System;
using UnityEngine;

namespace Assets.Scripts.Player
{
    public enum PlayerState
    {
        Preview,    // Default state for character preview in selection
        Idle,       // Player can move but no input
        Moving,     // Player moving with input
        Jumping,    // Player jumping
        Dashing,    // Player dashing
        Climbing,   // Player climbing
        Dead        // Player dead
    }
    public class PlayerStateManager : MonoBehaviour
    {
        public PlayerState CurrentState { get; private set; }
        public event Action<PlayerState> OnStateChanged;

        public void ChangeState(PlayerState newState)
        {
            if (CurrentState != newState)
            {
                CurrentState = newState;
                OnStateChanged?.Invoke(newState);
                PlayerEvents.OnStateChange?.Invoke(newState);
            }
        }

        void Start()
        {
            ChangeState(PlayerState.Preview);
        }
    }
}

