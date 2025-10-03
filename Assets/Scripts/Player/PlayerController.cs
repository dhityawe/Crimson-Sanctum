using UnityEngine;
using System;
using GabrielBigardi.SpriteAnimator;

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
        [SerializeField] private SpriteAnimator _spriteAnimator;

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
                    // _spriteAnimator.Play("Climb");
                    PlayAnimation("Climb");
                    SetAbilityEnabled(_move, false);
                    SetAbilityEnabled(_dash, false);
                    SetAbilityEnabled(_climb, true);
                    break;
                case PlayerState.Dead:
                    // _spriteAnimator.Play("Dead");
                    PlayAnimation("Dead");
                    SetAbilityEnabled(_move, false);
                    SetAbilityEnabled(_dash, false);
                    SetAbilityEnabled(_climb, false);
                    break;
                case PlayerState.Dashing:
                    // _spriteAnimator.Play("StartSkill").SetOnComplete(() => _spriteAnimator.Play("OnSkill"));
                    PlayAnimation("StartSkill", "OnSkill");
                    SetAbilityEnabled(_move, false);
                    SetAbilityEnabled(_dash, true);
                    SetAbilityEnabled(_climb, false);
                    break;
                case PlayerState.Idle:
                case PlayerState.Moving:
                    // _spriteAnimator.Play("Move");
                    PlayAnimation("Move");
                    SetAbilityEnabled(_move, true);
                    SetAbilityEnabled(_dash, true);
                    SetAbilityEnabled(_climb, true);
                    break;
                case PlayerState.Jumping:
                    // _spriteAnimator.Play("StartJump").SetOnComplete(() => _spriteAnimator.Play("OnAir"));
                    PlayAnimation("StartJump", "OnAir");
                    SetAbilityEnabled(_move, true);
                    SetAbilityEnabled(_dash, true);
                    SetAbilityEnabled(_climb, true);
                    break;
            }
        }

        public void PlayAnimation(string animationName, string onCompleteAnimation = null)
        {
            if (string.IsNullOrEmpty(onCompleteAnimation))
                _spriteAnimator.Play(animationName);
            else
                _spriteAnimator.Play(animationName).SetOnComplete(() => _spriteAnimator.Play(onCompleteAnimation));

            Debug.Log($"Playing animation: {_spriteAnimator.CurrentAnimation.Name}");
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
