using System;
using UnityEngine;
using Assets.Scripts.Core.Managers;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : MonoBehaviour
    {
        #region SerializeFields
        [SerializeField]
        [Range(1f, 5)]
        private float _moveSpeed;
        [SerializeField]
        private float _jumpForce;
        [SerializeField]
        private bool _isGrounded;
        [SerializeField]
        private SpriteRenderer _charSprite;
        [SerializeField]
        private bool _isFlipx;
        [SerializeField]
        private bool _canMove;
        [Header("Ground")]
        [SerializeField]
        private Transform _groundCheck;
        [SerializeField]
        private LayerMask _groundLayer;
        #endregion

        #region Actions
        public static event Action OnDeath;
        public static event Action OnPickupCoin;
        #endregion

        
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

        }

        void FixedUpdate()
        {
            if (!_canMove) return;
            HandleMove();
            HandleJump();
        }

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

        private bool IsGrounded()
        {
            return Physics2D.OverlapCapsule(_groundCheck.position, new Vector2(0.7f, 0.1f), CapsuleDirection2D.Horizontal, 0, _groundLayer);
        }

        public void HandleMove()
        {
            // version 1
            // Vector2 direction = _isFlipx ? Vector2.left : Vector2.right;
            // Vector2 newPosition = new(_rb.position.x + _moveSpeed * direction.x * Time.deltaTime, _rb.position.y);
            // _rb.MovePosition(newPosition);

            // version 2
            // float direction = _isFlipx ? -1f : 1f;
            // Vector2 newPosition = new(direction * _moveSpeed, 0f);
            // _rb.linearVelocity = newPosition;

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
                // Apply jump force
                _rb.AddForceY(_jumpForce, ForceMode2D.Impulse);
                // _rb.linearVelocity = new Vector2(_rb.linearVelocityX, _jumpForce);
            }
        }
    }
}
