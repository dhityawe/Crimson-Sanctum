using UnityEngine;
using System;

namespace Assets.Scripts.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private PlayerMove _move;
        [SerializeField] private PlayerDash _dash;
        [SerializeField] private PlayerClimb _climb;
        [SerializeField] private PlayerStateManager _stateManager;
        [SerializeField] private PlayerCollisionHandler _collisionHandler;
        [SerializeField] private PlayerHealth _health;

        private IPlayerAbility[] _abilities;
        
        void Start()
        {
            InitializeComponents();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void InitializeComponents()
        {
            // Get components if not assigned
            if (_move == null) _move = GetComponent<PlayerMove>();
            if (_dash == null) _dash = GetComponent<PlayerDash>();
            if (_climb == null) _climb = GetComponent<PlayerClimb>();
            if (_stateManager == null) _stateManager = GetComponent<PlayerStateManager>();
            if (_collisionHandler == null) _collisionHandler = GetComponent<PlayerCollisionHandler>();
            if (_health == null) _health = GetComponent<PlayerHealth>();

            // Initialize abilities array
            _abilities = new IPlayerAbility[] { _move, _dash, _climb };
        }

        private void SubscribeToEvents()
        {
            if (_stateManager != null)
            {
                _stateManager.OnStateChanged += HandleStateChange;
            }

            if (_collisionHandler != null)
            {
                _collisionHandler.OnCollisionEnter += HandleCollision;
                _collisionHandler.OnTriggerEnter += HandleTrigger;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_stateManager != null)
            {
                _stateManager.OnStateChanged -= HandleStateChange;
            }

            if (_collisionHandler != null)
            {
                _collisionHandler.OnCollisionEnter -= HandleCollision;
                _collisionHandler.OnTriggerEnter -= HandleTrigger;
            }
        }

        private void HandleStateChange(PlayerState newState)
        {
            switch (newState)
            {
                case PlayerState.Preview:
                    SetAbilityEnabled(_move, false);
                    SetAbilityEnabled(_dash, false);
                    SetAbilityEnabled(_climb, false);
                    break;
                case PlayerState.Climbing:
                    SetAbilityEnabled(_move, false);
                    SetAbilityEnabled(_dash, false);
                    SetAbilityEnabled(_climb, true);
                    break;
                case PlayerState.Dead:
                    SetAbilityEnabled(_move, false);
                    SetAbilityEnabled(_dash, false);
                    SetAbilityEnabled(_climb, false);
                    break;
                case PlayerState.Dashing:
                    SetAbilityEnabled(_move, false);
                    SetAbilityEnabled(_dash, true);
                    SetAbilityEnabled(_climb, false);
                    break;
                case PlayerState.Idle:
                case PlayerState.Moving:
                case PlayerState.Jumping:
                    SetAbilityEnabled(_move, true);
                    SetAbilityEnabled(_dash, true);
                    SetAbilityEnabled(_climb, true);
                    break;
            }
        }

        private void SetAbilityEnabled(IPlayerAbility ability, bool enabled)
        {
            ability?.SetEnabled(enabled);
        }

        private void HandleCollision(Collision2D collision)
        {
            // Central collision handling can be added here if needed
        }

        private void HandleTrigger(Collider2D other)
        {
            // Central trigger handling can be added here if needed
        }

    }    
}
