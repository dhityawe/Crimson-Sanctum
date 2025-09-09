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
            if (!_canMove) return;
            HandleJump();
        }

        void FixedUpdate()
        {
            if (!_canMove) return;
            HandleMove();
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
            else if (other.gameObject.CompareTag("Ground"))
            {
                _isGrounded = true;
            }
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                _isGrounded = false;
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

        public void HandleMove()
        {
            Vector2 direction = _isFlipx ? Vector2.left : Vector2.right;
            Vector2 newPosition = _rb.position + _moveSpeed * Time.fixedDeltaTime * direction;
            _rb.MovePosition(newPosition);
        }

        private void HandleJump()
        {
            
            // Check if jump input is pressed and player is grounded
            if (GameInput.Instance.IsJumpPressed() && _isGrounded)
            {
                // Apply jump force
                _rb.AddForceY(_jumpForce);
            }
        }
    }
}
