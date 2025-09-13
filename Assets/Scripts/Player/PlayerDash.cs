using System;
using System.Collections;
using Assets.Scripts.Core.Managers;
using UnityEngine;

namespace Assets.Scripts.Player 
{
    public class PlayerDash : MonoBehaviour
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

        #region Events
        public event Action OnEndDash;
        #endregion

        protected virtual void Start()
        {
            _playerMove = GetComponent<PlayerMove>();
            _rb = _playerMove.GetComponent<Rigidbody2D>();
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
            // start the animation of dash should be place here
            StartCoroutine(StartCooldownDash(_cooldownTime));
            _dashTimer = _dashDuration;

            _dashDirection = _playerMove.IsFlipX() ? Vector2.left : Vector2.right;
            
        }

        public void CancelDash()
        {
            if (_isDashing)
            {
                _isDashing = false;
                _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0); // reset velocity
                OnEndDash?.Invoke();
            }
        }

        protected virtual void EndDash()
        {
            _isDashing = false;
            _rb.linearVelocity = new Vector2(_lastLinearVelocityX, 0); // reset velocity
            OnEndDash?.Invoke();
        }

        protected virtual IEnumerator StartCooldownDash(float cooldownTime)
        {
            if (_isCooldown)
            {
                Debug.Log("dash is cooldown");
                float elapsed = 0f;
                while (elapsed < cooldownTime)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                _isCooldown = false;
                Debug.Log("you can use dash again");
            }
        }
    }
}
