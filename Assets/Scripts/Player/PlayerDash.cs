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
            IsActive = true;
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
                _rb.linearVelocity = _dashDirection * _dashSpeed;
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
        }

        public void CancelDash()
        {
            if (_isDashing)
            {
                _isDashing = false;
                _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0);
                OnEndDash?.Invoke();
            }
        }

        protected virtual void EndDash()
        {
            _spriteAnimator.Play("EndSkill").SetOnComplete(() => _spriteAnimator.Play("Move"));
            _isDashing = false;
            _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0);
            OnEndDash?.Invoke();
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
