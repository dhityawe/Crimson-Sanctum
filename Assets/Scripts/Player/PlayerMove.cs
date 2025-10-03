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
        
        [Header("Bouncable Detection")]
        [SerializeField]
        private Transform _horizontalRaycastPoint;
        [SerializeField]
        private Transform _verticalRaycastPoint;
        [SerializeField]
        private float _horizontalRaycastDistance = 1.0f;
        [SerializeField]
        private float _verticalRaycastDistance = 1.0f;
        [SerializeField]
        private LayerMask _bouncableLayer;
        [SerializeField]
        private bool _showRaycastGizmos = true;
        [SerializeField]
        private float _bounceCooldown = 0.5f; // Cooldown time between bounces
        [SerializeField]
        private float _stuckThreshold = 0.3f; // Distance threshold to consider player stuck
        [SerializeField]
        private float _stuckCheckTime = 1.0f; // Time to wait before checking if stuck
        [SerializeField]
        private float _stuckVelocityThreshold = 0.1f; // Velocity threshold for stuck detection
        [SerializeField]
        private bool _useSmartRaycasting = true; // Only raycast in relevant directions
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
        
        // Bounce cooldown tracking
        private float _lastBounceTime;
        private Vector2 _lastBouncePosition;
        
        // Stuck detection tracking
        private Vector2 _positionBeforeCooldown;
        private float _stuckCheckStartTime;

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
            // Moved CheckBouncableCollision to FixedUpdate for physics consistency
        }

        void FixedUpdate()
        {
            if (!enabled || !_canMove) return;
            HandleMove();
            CheckBouncableCollision(); // Better for physics-related checks
        }

        public bool CanMove() => _canMove;

        #region New Collision Handling
        private void HandleCollision(Collision2D other)
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
            // _spriteAnimator.Play("Dead");
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
        
        private void CheckBouncableCollision()
        {
            if (_horizontalRaycastPoint == null || _verticalRaycastPoint == null) return;
            
            Vector2 currentPosition = transform.position;
            float currentTime = Time.fixedTime; // Use fixedTime for FixedUpdate
            
            // Enhanced stuck detection using velocity
            bool isVelocityStuck = Mathf.Abs(_rb.linearVelocityX) < _stuckVelocityThreshold;
            bool hasBeenStuckLongEnough = currentTime - _stuckCheckStartTime >= _stuckCheckTime;
            
            // Check if cooldown period has ended and player might be stuck
            if (currentTime - _lastBounceTime >= _bounceCooldown)
            {
                // Enhanced stuck detection: velocity + position + time
                if (hasBeenStuckLongEnough && isVelocityStuck)
                {
                    float distanceMoved = Vector2.Distance(currentPosition, _positionBeforeCooldown);
                    
                    if (distanceMoved < _stuckThreshold)
                    {
                        Debug.Log("Player stuck detected - forcing direction change!");
                        PerformBounce(currentPosition, currentTime, true);
                        return;
                    }
                }
            }
            else
            {
                // Still in cooldown, check distance-based prevention
                if (Vector2.Distance(currentPosition, _lastBouncePosition) < _horizontalRaycastDistance * 0.8f)
                {
                    return;
                }
            }
            
            // Smart raycasting - only check relevant directions
            bool foundBouncable = _useSmartRaycasting ? 
                CheckBouncableSmartRaycast(currentPosition) : 
                CheckBouncableAllDirections(currentPosition);
            
            if (foundBouncable)
            {
                PerformBounce(currentPosition, currentTime, false);
            }
        }
        
        private bool CheckBouncableSmartRaycast(Vector2 currentPosition)
        {
            // Determine if we're in a tight space first
            Vector2 horizontalPos = _horizontalRaycastPoint.position;
            Vector2 leftDir = _isFlipx ? Vector2.right : Vector2.left;
            Vector2 rightDir = _isFlipx ? Vector2.left : Vector2.right;
            
            RaycastHit2D leftHit = Physics2D.Raycast(horizontalPos, leftDir, _horizontalRaycastDistance, _bouncableLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(horizontalPos, rightDir, _horizontalRaycastDistance, _bouncableLayer);
            
            bool inTightSpace = leftHit.collider != null && rightHit.collider != null;
            
            if (inTightSpace)
            {
                // In tight space - check all directions
                return CheckBouncableAllDirections(currentPosition);
            }
            else
            {
                // Not in tight space - only check movement direction + verticals
                Vector2 movementDir = _isFlipx ? Vector2.left : Vector2.right;
                RaycastHit2D movementHit = Physics2D.Raycast(horizontalPos, movementDir, _horizontalRaycastDistance, _bouncableLayer);
                
                // Still check verticals for ceiling/floor bounces
                Vector2 verticalPos = _verticalRaycastPoint.position;
                RaycastHit2D upHit = Physics2D.Raycast(verticalPos, Vector2.up, _verticalRaycastDistance, _bouncableLayer);
                RaycastHit2D downHit = Physics2D.Raycast(verticalPos, Vector2.down, _verticalRaycastDistance, _bouncableLayer);
                
                return CheckHitForBouncable(movementHit) || CheckHitForBouncable(upHit) || CheckHitForBouncable(downHit);
            }
        }
        
        private bool CheckBouncableAllDirections(Vector2 currentPosition)
        {
            // Original full raycast logic
            Vector2 horizontalPos = _horizontalRaycastPoint.position;
            Vector2 leftDirection = _isFlipx ? Vector2.right : Vector2.left;
            Vector2 rightDirection = _isFlipx ? Vector2.left : Vector2.right;
            
            RaycastHit2D leftHit = Physics2D.Raycast(horizontalPos, leftDirection, _horizontalRaycastDistance, _bouncableLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(horizontalPos, rightDirection, _horizontalRaycastDistance, _bouncableLayer);
            
            Vector2 verticalPos = _verticalRaycastPoint.position;
            RaycastHit2D upHit = Physics2D.Raycast(verticalPos, Vector2.up, _verticalRaycastDistance, _bouncableLayer);
            RaycastHit2D downHit = Physics2D.Raycast(verticalPos, Vector2.down, _verticalRaycastDistance, _bouncableLayer);
            
            // Runtime visualization
            if (_showRaycastGizmos)
            {
                Debug.DrawRay(horizontalPos, leftDirection * _horizontalRaycastDistance, leftHit.collider != null ? Color.red : Color.white);
                Debug.DrawRay(horizontalPos, rightDirection * _horizontalRaycastDistance, rightHit.collider != null ? Color.green : Color.white);
                Debug.DrawRay(verticalPos, Vector2.up * _verticalRaycastDistance, upHit.collider != null ? Color.blue : Color.white);
                Debug.DrawRay(verticalPos, Vector2.down * _verticalRaycastDistance, downHit.collider != null ? Color.magenta : Color.white);
            }
            
            return CheckHitForBouncable(leftHit) || CheckHitForBouncable(rightHit) || 
                   CheckHitForBouncable(upHit) || CheckHitForBouncable(downHit);
        }
        
        private bool CheckHitForBouncable(RaycastHit2D hit)
        {
            return hit.collider != null && hit.collider.gameObject.CompareTag("Bouncable");
        }
        
        private void PerformBounce(Vector2 currentPosition, float currentTime, bool isStuckBounce)
        {
            SetMove();
            _lastBounceTime = currentTime;
            _lastBouncePosition = currentPosition;
            _positionBeforeCooldown = currentPosition;
            _stuckCheckStartTime = currentTime;
            
            string bounceType = isStuckBounce ? "Stuck" : "Normal";
            Debug.Log($"{bounceType} bounce detected!");
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
            if (IsGrounded() && _spriteAnimator.CurrentAnimation.Name != "StartSkill" || _spriteAnimator.CurrentAnimation.Name != "OnSkill")
            {
                _stateManager.ChangeState(PlayerState.Moving);
                // Debug.Log($"current state: {_stateManager.CurrentState}");
            }
        }

        private void HandleJump()
        {
            if (GameInput.Instance.IsJumpPressed() && IsGrounded())
            {
                _spriteAnimator.Play("StartJump").SetOnComplete(() => _spriteAnimator.Play("OnAir").SetOnComplete(() =>
                {
                    if (IsGrounded())
                        _spriteAnimator.Play("Move");
                }));
                // _stateManager.ChangeState(PlayerState.Jumping);
                LastLinearVelocityX = _rb.linearVelocityX;
                GetComponent<PlayerDash>()?.CancelDash();
                _rb.linearVelocity = new Vector2(_rb.linearVelocityX, _jumpForce);
            }
        }

        #region Gizmos
        void OnDrawGizmos()
        {
            if (!_showRaycastGizmos) return;
            
            // Draw horizontal raycasts
            if (_horizontalRaycastPoint != null)
            {
                Vector3 horizontalPos = _horizontalRaycastPoint.position;
                Vector3 leftDirection = _isFlipx ? Vector3.right : Vector3.left;
                Vector3 rightDirection = _isFlipx ? Vector3.left : Vector3.right;
                
                // Left raycast (red)
                Gizmos.color = Color.red;
                Gizmos.DrawRay(horizontalPos, leftDirection * _horizontalRaycastDistance);
                
                // Right raycast (green)
                Gizmos.color = Color.green;
                Gizmos.DrawRay(horizontalPos, rightDirection * _horizontalRaycastDistance);
                
                // Draw raycast origin point
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(horizontalPos, 0.1f);
            }
            
            // Draw vertical raycasts
            if (_verticalRaycastPoint != null)
            {
                Vector3 verticalPos = _verticalRaycastPoint.position;
                
                // Up raycast (blue)
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(verticalPos, Vector3.up * _verticalRaycastDistance);
                
                // Down raycast (magenta)
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(verticalPos, Vector3.down * _verticalRaycastDistance);
                
                // Draw raycast origin point
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(verticalPos, 0.1f);
            }
        }
        #endregion
    }
}
