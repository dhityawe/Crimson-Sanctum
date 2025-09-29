using System;
using UnityEngine;
using Assets.Scripts.Core.Managers;
using GabrielBigardi.SpriteAnimator;

namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : MonoBehaviour, IPlayerAbility
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

        [Header("Animator")]
        [SerializeField]
        private SpriteAnimator _spriteAnimator;

        [HideInInspector] public float LastLinearVelocityX { get; set; }

        #region Actions
        public static event Action OnDeath;
        public static event Action OnPickupCoin;
        #endregion

        private bool _canMove;
        private Rigidbody2D _rb;
        private PlayerStateManager _stateManager;
        private PlayerCollisionHandler _collisionHandler;

        #region IPlayerAbility Implementation
        public bool IsActive { get; private set; }
        
        public bool CanActivate()
        {
            return _stateManager == null || 
                   (_stateManager.CurrentState != PlayerState.Preview &&
                    _stateManager.CurrentState != PlayerState.Climbing && 
                    _stateManager.CurrentState != PlayerState.Dead);
        }
        
        public void Activate()
        {
            IsActive = true;
            _canMove = true;
        }
        
        public void Deactivate()
        {
            IsActive = false;
            _canMove = false;
        }
        
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
        #endregion

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stateManager = GetComponent<PlayerStateManager>();
            _collisionHandler = GetComponent<PlayerCollisionHandler>();
        }

        void Start()
        {
            _canMove = true;
            _charSprite.flipX = _isFlipx;
            IsActive = true;
            
            // Subscribe to collision events
            if (_collisionHandler != null)
            {
                _collisionHandler.OnCollisionEnter += HandleCollision;
                _collisionHandler.OnTriggerEnter += HandleTrigger;
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (_collisionHandler != null)
            {
                _collisionHandler.OnCollisionEnter -= HandleCollision;
                _collisionHandler.OnTriggerEnter -= HandleTrigger;
            }
        }

        void Update()
        {
            if (!enabled || !_canMove) return;
            HandleJump();
        }

        void FixedUpdate()
        {
            if (!enabled || !_canMove) return;
            HandleMove();
        }

        public bool CanMove() => _canMove;

        #region New Collision Handling
        private void HandleCollision(Collision2D other)
        {
            if (!enabled) return;
            
            if (other.gameObject.CompareTag("Bouncable"))
            {
                SetMove();
            }
            else if (other.gameObject.CompareTag("DeathZone"))
            {
                HandleDeath();
            }
            else if (other.gameObject.CompareTag("Pickable"))
            {
                HandleCoinPickup();
            }
        }

        private void HandleTrigger(Collider2D other)
        {
            if (!enabled) return;
            
            if (other.gameObject.CompareTag("DeathZone"))
            {
                HandleDeath();
            }
            else if (other.gameObject.CompareTag("Pickable"))
            {
                HandleCoinPickup();
            }
        }

        private void HandleDeath()
        {
            _spriteAnimator.Play("Dead");
            OnDeath?.Invoke();
            PlayerEvents.OnPlayerDeath?.Invoke();
            if (_stateManager != null)
                _stateManager.ChangeState(PlayerState.Dead);
        }

        private void HandleCoinPickup()
        {
            OnPickupCoin?.Invoke();
            PlayerEvents.OnCoinPickup?.Invoke();
        }
        #endregion


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
            float direction = _isFlipx ? -1f : 1f;
            float targetX = direction * _moveSpeed;
            float newX = Mathf.Lerp(_rb.linearVelocityX, targetX, 0.1f);
            _rb.linearVelocity = new Vector2(newX, _rb.linearVelocityY);
        }

        private void HandleJump()
        {
            if (GameInput.Instance.IsJumpPressed() && IsGrounded())
            {
                _spriteAnimator.Play("StartJump").SetOnComplete(() => _spriteAnimator.Play("OnAir"));
                LastLinearVelocityX = _rb.linearVelocityX;
                GetComponent<PlayerDash>()?.CancelDash();
                _rb.linearVelocity = new Vector2(_rb.linearVelocityX, _jumpForce);
            }
        }
    }
}
