using System;
using UnityEngine;
using Assets.Scripts.Core.Managers;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : MonoBehaviour
    {
        #region SerializeFields
        [Header("Player Settings")]
        [SerializeField]
        [Range(1f, 5)]
        private float _moveSpeed;
        [SerializeField]
        private float _jumpForce;
        [SerializeField]
        private SpriteRenderer _charSprite;
        [SerializeField]
        private bool _isFlipx;
        
        [Header("Ground")]
        [SerializeField]
        private Transform _groundCheck;
        [SerializeField]
        private LayerMask _groundLayer;
        #endregion

        [HideInInspector] public float LastLinearVelocityX { get; set; }

        #region Actions
        public static event Action OnDeath;
        public static event Action OnPickupCoin;
        #endregion

        private bool _canMove;
        private Rigidbody2D _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        void Start()
        {
            _canMove = true;
            _charSprite.flipX = _isFlipx;
        }

        void Update()
        {
            if (!_canMove) return;
            HandleJump();
        }

        void FixedUpdate()
        {
            if (!_canMove) return;
            HandleMove();
        }

        public bool CanMove() => _canMove;

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Bouncable"))
            {
                SetMove();
            }
            else if (other.gameObject.CompareTag("DeathZone"))
            {
                OnDeath?.Invoke();
            }
            else if (other.gameObject.CompareTag("Pickable"))
            {
                OnPickupCoin?.Invoke();
            }
        }

        public void SetMove()
        {
            _isFlipx = !_isFlipx;
            if (_charSprite != null)
                _charSprite.flipX = _isFlipx;
        }

        public void EnableMove(bool value)
        {
            _canMove = value;
        }

        public Rigidbody2D GetRigidbody()
        {
            return _rb;
        }

        public bool IsFlipX() => _isFlipx;

        public bool IsGrounded()
        {
            return Physics2D.OverlapCapsule(_groundCheck.position, new Vector2(0.7f, 0.1f), CapsuleDirection2D.Horizontal, 0, _groundLayer);
        }

        public void HandleMove()
        {
            // version 3
            float direction = _isFlipx ? -1f : 1f;
            float targetX = direction * _moveSpeed;

            // smooth transition ke kecepatan target
            float newX = Mathf.Lerp(_rb.linearVelocityX, targetX, 0.1f);
            _rb.linearVelocity = new Vector2(newX, _rb.linearVelocityY);
            
        }

        private void HandleJump()
        {

            // Check if jump input is pressed and player is grounded
            if (GameInput.Instance.IsJumpPressed() && IsGrounded())
            {
                float lastLinearVelocityX = LastLinearVelocityX = _rb.linearVelocityX;
                GetComponent<PlayerDash>()?.CancelDash();
                // Apply jump force
                // _rb.AddForce(new Vector2(lastLinearVelocityX, _jumpForce), ForceMode2D.Impulse);
                _rb.linearVelocity = new Vector2(_rb.linearVelocityX, _jumpForce);
            }
        }
    }
}
