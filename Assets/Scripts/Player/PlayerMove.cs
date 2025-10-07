using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Core.Managers;
using GabrielBigardi.SpriteAnimator;
using CrimsonSanctum.Audio;


namespace Assets.Scripts.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : MonoBehaviour, IPlayerAbility
    {
        #region SerializeFields
        [Header("Player Settings")]
        [SerializeField]
        [Range(1f, 5)]private float _moveSpeed;
        [SerializeField] private float _jumpForce;
        [SerializeField] private SpriteRenderer _charSprite;
        [SerializeField]
        private bool _isFlipx;
    
        [Header("Ground")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _playerLayer; // Player layer to exclude from raycasts
        
        [Header("Bouncable Detection")]
        [SerializeField] private Transform _horizontalRaycastPoint;
        [SerializeField] private Transform _verticalRaycastPoint;
        [SerializeField] private float _horizontalRaycastDistance = 1.0f;
        [SerializeField] private float _verticalRaycastDistance = 1.0f;
        [SerializeField] private bool _showRaycastGizmos = true;
        [SerializeField] private float _bounceCooldown = 0.5f; // Cooldown time between bounces
        [SerializeField] private float _stuckThreshold = 0.3f; // Distance threshold to consider player stuck
        [SerializeField]private float _stuckCheckTime = 1.0f; // Time to wait before checking if stuck
        [SerializeField] private float _stuckVelocityThreshold = 0.1f; // Velocity threshold for stuck detection
        [SerializeField] private bool _useSmartRaycasting = true; // Only raycast in relevant directions
        
        [Header("Knockback & Invulnerability")]
        [SerializeField] private float _knockbackForce = 5f; // Force applied when hit
        [SerializeField] private float _knockbackUpwardForce = 3f; // Upward force for jump effect
        [SerializeField] private float _knockbackDuration = 0.2f; // How long the knockback lasts
        [SerializeField] private float _knockbackRecoveryTime = 0.3f; // Time to smoothly transition back to normal movement
        #endregion

        [Header("Animator")]
        [SerializeField] private SpriteAnimator _spriteAnimator;
        [SerializeField] private SpriteAnimator _effectAnimator;

        [Header("Audio List")]
        [SerializeField] private List<AudioClip> _sfxList;
        [SerializeField] [Tooltip("Time between footstep sound loops (creates rhythmic footsteps)")] 
        private float _footstepInterval = 0.3f; // Time between footstep sounds
        [SerializeField] [Range(0f, 1f)] [Tooltip("Volume for footstep and jump SFX")]
        private float _sfxVolume = 0.8f;

        [HideInInspector] public float LastLinearVelocityX { get; set; }

        #region Actions
        public static event Action OnPickupCoin;
        #endregion

        private bool _canMove;
        private Rigidbody2D _rb;
        private PlayerStateManager _stateManager;
        private PlayerCollisionHandler _collisionHandler;
        private PlayerHealth _playerHealth; // Cached reference to PlayerHealth component
        private AfterImageEffect _afterImageEffect; // Cached reference to AfterImageEffect component
        private PlayerDash _playerDash; // Cached reference to PlayerDash component
        
        // Knockback tracking
        private bool _isKnockbackActive = false;
        private bool _isRecoveringFromKnockback = false;
        private float _knockbackRecoveryTimer = 0f;
        
        // Bounce cooldown tracking
        private float _lastBounceTime;
        private Vector2 _lastBouncePosition;
        
        // Stuck detection tracking
        private Vector2 _positionBeforeCooldown;
        private float _stuckCheckStartTime;
        
        // SFX tracking
        private float _footstepTimer = 0f;
        private Dictionary<string, AudioSource> _playerAudioSources = new Dictionary<string, AudioSource>();

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
            _playerHealth = GetComponent<PlayerHealth>();
            _afterImageEffect = GetComponent<AfterImageEffect>();
            _playerDash = GetComponent<PlayerDash>();
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
            
            // Subscribe to health events for knockback
            if (_playerHealth != null)
            {
                PlayerHealth.OnInvulnerabilityStart += OnTakeDamage;
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
            
            // Unsubscribe from health events
            PlayerHealth.OnInvulnerabilityStart -= OnTakeDamage;
            
            // Cleanup persistent AudioSources
            CleanupPlayerAudioSources();
        }

        void Update()
        {
            if (!enabled || !_canMove) return;
            
            // Update knockback recovery transition
            if (_isRecoveringFromKnockback)
            {
                _knockbackRecoveryTimer -= Time.deltaTime;
                if (_knockbackRecoveryTimer <= 0f)
                {
                    _isRecoveringFromKnockback = false;
                }
            }
            
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

        #region Audio Management
        
        /// <summary>
        /// Creates a persistent AudioSource for player sounds
        /// </summary>
        private AudioSource CreatePlayerAudioSource(string name, AudioClip clip, float volume = 1f, bool loop = false)
        {
            if (clip == null) return null;
            
            // Check if AudioSource already exists
            if (_playerAudioSources.ContainsKey(name))
            {
                var existingSource = _playerAudioSources[name];
                if (existingSource != null)
                {
                    existingSource.clip = clip;
                    existingSource.volume = volume * _sfxVolume;
                    existingSource.loop = loop;
                    return existingSource;
                }
            }
            
            // Create new AudioSource GameObject as child
            GameObject audioObject = new GameObject($"PlayerAudio_{name}");
            audioObject.transform.SetParent(transform);
            audioObject.transform.localPosition = Vector3.zero;
            
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume * _sfxVolume;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D sound
            
            _playerAudioSources[name] = source;
            return source;
        }
        
        /// <summary>
        /// Stops a specific player AudioSource
        /// </summary>
        private void StopPlayerAudioSource(string name)
        {
            if (_playerAudioSources.ContainsKey(name))
            {
                var source = _playerAudioSources[name];
                if (source != null && source.isPlaying)
                {
                    source.Stop();
                }
            }
        }
        
        /// <summary>
        /// Cleanup all player AudioSources
        /// </summary>
        private void CleanupPlayerAudioSources()
        {
            foreach (var kvp in _playerAudioSources)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Stop();
                    Destroy(kvp.Value.gameObject);
                }
            }
            _playerAudioSources.Clear();
        }
        
        #endregion

        #region New Collision Handling
        private void HandleCollision(Collision2D other)
        {
            if (!enabled) return;
            
            if (other.gameObject.CompareTag("DeathZone"))
            {
                // Just attempt damage - PlayerHealth handles invulnerability
                HandleDamage();
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
                // Just attempt damage - PlayerHealth handles invulnerability
                HandleDamage();
            }
            else if (other.gameObject.CompareTag("Pickable"))
            {
                HandleCoinPickup();
            }
        }

        private void HandleDamage()
        {
            // Attempt to deal damage - PlayerHealth will handle invulnerability check
            if (_playerHealth != null)
            {
                bool damageApplied = _playerHealth.TakeDamage(1);
                
                // Play hit sound if damage was applied
                if (damageApplied && _sfxList != null && _sfxList.Count > 2 && _sfxList[2] != null)
                {
                    AudioSource hitSource = CreatePlayerAudioSource("Hit", _sfxList[2], 1f, false);
                    if (hitSource != null)
                    {
                        hitSource.Play();
                    }
                }
            }
        }
        
        /// <summary>
        /// Called when PlayerHealth starts invulnerability (meaning damage was actually taken)
        /// </summary>
        private void OnTakeDamage()
        {
            // Apply knockback when damage is confirmed
            ApplyKnockback();
        }
        
        private void ApplyKnockback()
        {
            if (_rb == null) return;

            _playerHealth.ApplyEffect();
            
            // Stop footstep sound during knockback
            StopPlayerAudioSource("Footstep");
            
            // Start afterimage effect during knockback
            if (_afterImageEffect != null)
            {
                _afterImageEffect.StartAfterimage();
            }
            
            // Calculate knockback direction (opposite to facing direction)
            float knockbackDirection = _isFlipx ? 1f : -1f; // Push opposite to facing direction
            
            // Apply both horizontal and upward force for engaging effect
            Vector2 knockbackVelocity = new Vector2(
                knockbackDirection * _knockbackForce, 
                _knockbackUpwardForce // Add small jump
            );
            
            // Apply knockback force
            _rb.linearVelocity = knockbackVelocity;
            
            // Set knockback active flag
            _isKnockbackActive = true;
            
            // Schedule knockback end
            Invoke(nameof(EndKnockback), _knockbackDuration);
        }
        
        private void EndKnockback()
        {
            _isKnockbackActive = false;
            _isRecoveringFromKnockback = true;
            _knockbackRecoveryTimer = _knockbackRecoveryTime;
            
            // Stop afterimage effect when knockback ends
            if (_afterImageEffect != null)
            {
                _afterImageEffect.StopAfterimage();
            }
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
            Vector2 horizontalPos = _horizontalRaycastPoint.position;
            Vector2 movementDir = _isFlipx ? Vector2.left : Vector2.right;
            
            // Check horizontal movement direction for bouncable (exclude player layer)
            int horizontalLayerMask = ~_playerLayer; // Exclude player from raycast
            RaycastHit2D horizontalHit = Physics2D.Raycast(horizontalPos, movementDir, _horizontalRaycastDistance, horizontalLayerMask);
            bool foundHorizontalBouncable = CheckHitForBouncable(horizontalHit);
            
            // Check vertical down for bouncable (exclude ground layer to allow jumping)
            Vector2 verticalPos = _verticalRaycastPoint.position;
            int verticalLayerMask = ~_groundLayer;
            RaycastHit2D verticalHit = Physics2D.Raycast(verticalPos, Vector2.down, _verticalRaycastDistance, verticalLayerMask);
            bool foundVerticalBouncable = CheckHitForBouncable(verticalHit);
            
            // Runtime visualization
            if (_showRaycastGizmos)
            {
                Debug.DrawRay(horizontalPos, movementDir * _horizontalRaycastDistance, 
                    foundHorizontalBouncable ? Color.red : Color.white);
                Debug.DrawRay(verticalPos, Vector2.down * _verticalRaycastDistance, 
                    foundVerticalBouncable ? Color.magenta : Color.white);
            }
            
            return foundHorizontalBouncable || foundVerticalBouncable;
        }
        
        private bool CheckBouncableAllDirections(Vector2 currentPosition)
        {
            Vector2 horizontalPos = _horizontalRaycastPoint.position;
            Vector2 leftDirection = _isFlipx ? Vector2.right : Vector2.left;
            Vector2 rightDirection = _isFlipx ? Vector2.left : Vector2.right;
            
            // Horizontal raycasts check ALL layers EXCEPT player for Bouncable tag
            int horizontalLayerMask = ~_playerLayer; // Exclude player from raycast
            RaycastHit2D leftHit = Physics2D.Raycast(horizontalPos, leftDirection, _horizontalRaycastDistance, horizontalLayerMask);
            RaycastHit2D rightHit = Physics2D.Raycast(horizontalPos, rightDirection, _horizontalRaycastDistance, horizontalLayerMask);
            
            bool foundLeftBouncable = CheckHitForBouncable(leftHit);
            bool foundRightBouncable = CheckHitForBouncable(rightHit);
            
            // Vertical bounce check excludes ground layer
            Vector2 verticalPos = _verticalRaycastPoint.position;
            int verticalLayerMask = ~_groundLayer;
            RaycastHit2D downHit = Physics2D.Raycast(verticalPos, Vector2.down, _verticalRaycastDistance, verticalLayerMask);
            bool foundVerticalBouncable = CheckHitForBouncable(downHit);
            
            // Runtime visualization
            if (_showRaycastGizmos)
            {
                Debug.DrawRay(horizontalPos, leftDirection * _horizontalRaycastDistance, 
                    foundLeftBouncable ? Color.red : Color.white);
                Debug.DrawRay(horizontalPos, rightDirection * _horizontalRaycastDistance, 
                    foundRightBouncable ? Color.green : Color.white);
                Debug.DrawRay(verticalPos, Vector2.down * _verticalRaycastDistance, 
                    foundVerticalBouncable ? Color.magenta : Color.white);
            }
            
            return foundLeftBouncable || foundRightBouncable || foundVerticalBouncable;
        }
        
        private bool CheckHitForBouncable(RaycastHit2D hit)
        {
            // Check if raycast hit something
            if (hit.collider == null) return false;
            
            // Check if it has the "Bouncable" tag
            return hit.collider.CompareTag("Bouncable");
        }
        
        private void PerformBounce(Vector2 currentPosition, float currentTime, bool isStuckBounce)
        {
            SetMove();
            _lastBounceTime = currentTime;
            _lastBouncePosition = currentPosition;
            _positionBeforeCooldown = currentPosition;
            _stuckCheckStartTime = currentTime;
            
            string bounceType = isStuckBounce ? "Stuck" : "Normal";
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
            if (_groundCheck == null) return false;
            
            return Physics2D.OverlapCapsule(_groundCheck.position, new Vector2(0.7f, 0.1f), CapsuleDirection2D.Horizontal, 0, _groundLayer);
        }

        public void HandleMove()
        {
            // Don't allow movement control during active knockback
            if (_isKnockbackActive)
            {
                StopPlayerAudioSource("Footstep");
                return;
            }
            
            float direction = _isFlipx ? -1f : 1f;
            float targetX = direction * _moveSpeed;
            
            // Handle footstep sound system
            HandleFootstepSound();
            
            // Smooth transition after knockback ends
            if (_isRecoveringFromKnockback)
            {
                // Calculate recovery progress (0 = just started, 1 = finished)
                float recoveryProgress = 1f - (_knockbackRecoveryTimer / _knockbackRecoveryTime);

                // First half: decelerate from knockback velocity to ~0
                // Second half: accelerate from ~0 to movement speed
                float lerpSpeed;
                if (recoveryProgress < 0.5f)
                {
                    // Deceleration phase (0 to 0.5)
                    lerpSpeed = Mathf.Lerp(0.02f, 0.15f, recoveryProgress * 2f); // Slower deceleration
                }
                else
                {
                    // Acceleration phase (0.5 to 1.0)
                    lerpSpeed = Mathf.Lerp(0.15f, 0.4f, (recoveryProgress - 0.5f) * 2f); // Faster acceleration
                }

                float newX = Mathf.Lerp(_rb.linearVelocityX, targetX, lerpSpeed);
                _rb.linearVelocity = new Vector2(newX, _rb.linearVelocityY);
            }
            else
            {
                // Normal movement
                float newX = Mathf.Lerp(_rb.linearVelocityX, targetX, 0.1f);
                _rb.linearVelocity = new Vector2(newX, _rb.linearVelocityY);
            }
            
            if (IsGrounded() && _spriteAnimator.CurrentAnimation.Name != "StartSkill" || _spriteAnimator.CurrentAnimation.Name != "OnSkill")
            {
                _stateManager.ChangeState(PlayerState.Moving);
                // Debug.Log($"current state: {_stateManager.CurrentState}");
            }
        }
        
        /// <summary>
        /// Handles footstep sound playback with rhythmic looping
        /// </summary>
        private void HandleFootstepSound()
        {
            // Validation checks
            if (_sfxList == null || _sfxList.Count == 0 || _sfxList[0] == null) return;
            
            // Use raycast-based ground detection (same as jumping)
            bool isGrounded = IsGroundedRaycast();
            
            // Check if dashing - don't play footsteps while dashing
            bool isDashing = _playerDash != null && _stateManager != null && 
                           (_stateManager.CurrentState == PlayerState.Dashing || 
                            (_playerDash.IsActive && _stateManager.CurrentState == PlayerState.Moving));
            
            // Player is grounded and NOT dashing - handle footstep rhythm
            if (isGrounded && !isDashing)
            {
                _footstepTimer += Time.fixedDeltaTime;
                
                // Time to play next footstep cycle
                if (_footstepTimer >= _footstepInterval)
                {
                    // Create/get persistent footstep AudioSource
                    AudioSource footstepSource = CreatePlayerAudioSource("Footstep", _sfxList[0], 1f, false);
                    if (footstepSource != null && !footstepSource.isPlaying)
                    {
                        footstepSource.Play();
                    }
                    
                    _footstepTimer = 0f;
                }
            }
            // Player is not grounded or is dashing - stop footsteps
            else
            {
                StopPlayerAudioSource("Footstep");
                _footstepTimer = 0f; // Reset for immediate play on landing
            }
        }
        
        private void HandleJump()
        {
            if (GameInput.Instance.IsJumpPressed() && IsGroundedRaycast())
            {
                _spriteAnimator.Play("StartJump").SetOnComplete(() => _spriteAnimator.Play("OnAir").SetOnComplete(() =>
                {
                    if (IsGroundedRaycast())
                        _spriteAnimator.Play("Move");
                }));
                // _stateManager.ChangeState(PlayerState.Jumping);
                LastLinearVelocityX = _rb.linearVelocityX;
                GetComponent<PlayerDash>()?.CancelDash();
                _rb.linearVelocity = new Vector2(_rb.linearVelocityX, _jumpForce);
                
                // Play jump SFX with persistent AudioSource
                if (_sfxList != null && _sfxList.Count > 1 && _sfxList[1] != null)
                {
                    AudioSource jumpSource = CreatePlayerAudioSource("Jump", _sfxList[1], 1f, false);
                    if (jumpSource != null)
                    {
                        jumpSource.Play();
                    }
                }
            }
        }
        
        /// <summary>
        /// Custom raycast-based ground detection using the vertical raycast point
        /// </summary>
        private bool IsGroundedRaycast()
        {
            if (_verticalRaycastPoint == null) return false;
            
            Vector2 verticalPos = _verticalRaycastPoint.position;
            RaycastHit2D hit = Physics2D.Raycast(verticalPos, Vector2.down, _verticalRaycastDistance, _groundLayer);
            
            return hit.collider != null;
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
                
                // Draw raycast origin point for GroundChecck
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(horizontalPos, 0.1f);
            }
            
            // Draw vertical raycast - ONLY DOWNWARD
            if (_verticalRaycastPoint != null)
            {
                Vector3 verticalPos = _verticalRaycastPoint.position;
                
                // Down raycast only (magenta)
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
