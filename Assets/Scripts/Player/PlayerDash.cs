using System;
using System.Collections;
using Assets.Scripts.Core.Managers;
using UnityEngine;
using GabrielBigardi.SpriteAnimator;

namespace Assets.Scripts.Player 
{
    public class PlayerDash : MonoBehaviour, IPlayerAbility
    {
        [Header("Dash Settings")]
        [SerializeField] protected float _dashDuration = 0.2f;
        [SerializeField] protected float _dashSpeed = 20f;
        [SerializeField] protected float _cooldownTime;

        private PlayerMove _playerMove;
        private Rigidbody2D _rb;
        private bool _isDashing;
        private bool _isCooldown;
        private float _dashTimer;
        private Vector2 _dashDirection;
        private float _lastLinearVelocityX;
        private PlayerStateManager _stateManager;
        private PlayerHealth _playerHealth;
        private AfterImageEffect _afterImageEffect;

        [Header("Animator")]
        [SerializeField] private SpriteAnimator _spriteAnimator;

        #region Events
        public event Action OnEndDash;
        #endregion

        #region IPlayerAbility Implementation
        public bool IsActive { get; private set; }
        
        public bool CanActivate()
        {
            return _stateManager == null || 
                   (_stateManager.CurrentState != PlayerState.Preview &&
                    _stateManager.CurrentState != PlayerState.Climbing && 
                    _stateManager.CurrentState != PlayerState.Dead &&
                    !_isDashing && !_isCooldown);
        }
        
        public void Activate()
        {
            if (CanActivate())
            {
                StartDash();
            }
        }
        
        public void Deactivate()
        {
            if (_isDashing)
            {
                CancelDash();
            }
        }
        
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
        #endregion

        protected virtual void Start()
        {
            _playerMove = GetComponent<PlayerMove>();
            _rb = _playerMove.GetComponent<Rigidbody2D>();
            _stateManager = GetComponent<PlayerStateManager>();
            _playerHealth = GetComponent<PlayerHealth>();
            _afterImageEffect = GetComponent<AfterImageEffect>();
            IsActive = true;
            
            // Subscribe to health events to cancel dash when hit
            if (_playerHealth != null)
            {
                PlayerHealth.OnInvulnerabilityStart += OnPlayerHit;
            }
        }
        
        protected virtual void OnDestroy()
        {
            // Unsubscribe from health events
            PlayerHealth.OnInvulnerabilityStart -= OnPlayerHit;
        }
        
        /// <summary>
        /// Called when player takes damage - cancels dash to allow knockback
        /// </summary>
        private void OnPlayerHit()
        {
            if (_isDashing)
            {
                // Cancel dash WITHOUT resetting velocity - let knockback apply
                // Only play Move animation if not climbing
                if (_stateManager == null || _stateManager.CurrentState != PlayerState.Climbing)
                {
                    _spriteAnimator.Play("Move");
                }
                _isDashing = false;
                OnEndDash?.Invoke();
                // Don't call CancelDash() because it resets velocity!
            }
        }

        protected virtual void Update()
        {
            if (!_playerMove.CanMove()) return;

            if (!_isDashing && GameInput.Instance.IsDashPressed() && !_isCooldown)
            {
                StartDash();
            }

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                {
                    EndDash();
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (_isDashing)
            {
                // Freeze Y velocity for flying effect
                _rb.linearVelocity = new Vector2(_dashDirection.x * _dashSpeed, 0f);
            }
        }

        protected virtual void StartDash()
        {
            _lastLinearVelocityX = _rb.linearVelocityX;
            _isDashing = true;
            _isCooldown = true;
            _spriteAnimator.Play("StartSkill").SetOnComplete(() => _spriteAnimator.Play("OnSkill"));
            StartCoroutine(StartCooldownDash(_cooldownTime));
            _dashTimer = _dashDuration;
            _dashDirection = _playerMove.IsFlipX() ? Vector2.left : Vector2.right;
            
            // Start afterimage effect
            if (_afterImageEffect != null)
            {
                _afterImageEffect.StartAfterimage();
            }
        }

        public void CancelDash()
        {
            if (_isDashing)
            {
                // Only play Move animation if not climbing
                if (_stateManager == null || _stateManager.CurrentState != PlayerState.Climbing)
                {
                    _spriteAnimator.Play("Move");
                }
                _isDashing = false;
                _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0);
                OnEndDash?.Invoke();
            }
        }

        protected virtual void EndDash()
        {
            // Only play EndSkill and transition to Move if not climbing
            if (_stateManager != null && _stateManager.CurrentState == PlayerState.Climbing)
            {
                // Just stop dashing, let climb animation continue
                _isDashing = false;
                _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0);
                OnEndDash?.Invoke();
            }
            else
            {
                // Normal end dash sequence
                _spriteAnimator.Play("EndSkill").SetOnComplete(() => _spriteAnimator.Play("Move"));
                _isDashing = false;
                _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0);
                OnEndDash?.Invoke();
            }
        }

        protected virtual IEnumerator StartCooldownDash(float cooldownTime)
        {
            if (_isCooldown)
            {
                float elapsed = 0f;
                while (elapsed < cooldownTime)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                _isCooldown = false;
            }
        }
    }
}
