using UnityEngine;
using System;
using GabrielBigardi.SpriteAnimator;
using DG.Tweening;

namespace Assets.Scripts.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 5;

        [Header("Invulnerability Settings")]
        [SerializeField] private float invulnerabilityDuration = 1.5f;
        [SerializeField] private bool debugInvulnerability = false;
        
        [Header("Visual Effects")]
        [SerializeField] private SpriteRenderer _playerSprite; // Main player sprite to apply effects
        [SerializeField] private ReusableEffectAnimator _effectAnimator; // Reusable effect animator
        [SerializeField] private Color _hitColor = Color.red; // Color to flash when hit
        [SerializeField] private float _colorBlinkDuration = 0.15f; // Duration of color blink
        [SerializeField] private float _flickerInterval = 0.1f; // Time between flickers during invulnerability
        [SerializeField] private float _flickerAlpha = 0.3f; // Alpha value when flickering (0 = invisible, 1 = fully visible)

        // Health state
        private int currentHealth;
        private PlayerStateManager _stateManager;

        // Invulnerability state
        private bool isInvulnerable = false;
        private float invulnerabilityTimer = 0f;

        #region Actions
        public static event Action OnDeath;
        public static event Action<int> OnHealthChanged; // Event for UI updates
        public static event Action TakingDamage;
        public static event Action OnInvulnerabilityStart;
        public static event Action OnInvulnerabilityEnd;
        #endregion

        public bool IsInvulnerable => isInvulnerable;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        
        // DOTween sequences for visual effects
        private Sequence _flickerSequence;
        private Color _originalColor;

        private void Awake()
        {
            _stateManager = GetComponent<PlayerStateManager>();
            currentHealth = maxHealth;
            
            // Auto-find player sprite if not assigned
            if (_playerSprite == null)
            {
                _playerSprite = GetComponent<SpriteRenderer>();
                if (_playerSprite == null)
                {
                    _playerSprite = GetComponentInChildren<SpriteRenderer>();
                }
            }
            
            // Store original color
            if (_playerSprite != null)
            {
                _originalColor = _playerSprite.color;
            }
        }

        private void Update()
        {
            UpdateInvulnerability();
        }
        
        private void OnDestroy()
        {
            // Clean up DOTween sequences
            _flickerSequence?.Kill();
        }

        /// <summary>
        /// Attempts to deal damage to the player. Returns true if damage was applied, false if blocked by invulnerability.
        /// </summary>
        public bool TakeDamage(int damage)
        {
            // Gate: Block damage if invulnerable
            if (isInvulnerable)
            {
                if (debugInvulnerability)
                    Debug.Log("[PlayerHealth] Damage blocked - invulnerable");
                return false;
            }

            // Apply damage
            currentHealth -= damage;
            TakingDamage?.Invoke();
            if (debugInvulnerability)
                Debug.Log($"[PlayerHealth] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

            // Trigger invulnerability after taking damage
            StartInvulnerability();

            // Notify listeners
            OnHealthChanged?.Invoke(currentHealth);

            // Check for death
            if (currentHealth <= 0)
            {
                Die();
            }

            return true; // Damage was applied
        }

        private void StartInvulnerability()
        {
            if (isInvulnerable) return; // Already invulnerable

            isInvulnerable = true;
            invulnerabilityTimer = invulnerabilityDuration;

            if (debugInvulnerability)
                Debug.Log($"[PlayerHealth] Invulnerability started for {invulnerabilityDuration}s");

            // Start visual effects
            StartHitVisualEffects();
            
            OnInvulnerabilityStart?.Invoke();
        }

        private void UpdateInvulnerability()
        {
            if (isInvulnerable)
            {
                // Stop invulnerability effects if player is dead
                if (_stateManager != null && _stateManager.CurrentState == PlayerState.Dead)
                {
                    StopHitVisualEffects();
                    isInvulnerable = false;
                    return;
                }
                
                invulnerabilityTimer -= Time.deltaTime;

                if (invulnerabilityTimer <= 0f)
                {
                    EndInvulnerability();
                }
            }
        }

        private void EndInvulnerability()
        {
            isInvulnerable = false;
            invulnerabilityTimer = 0f;

            if (debugInvulnerability)
                Debug.Log("[PlayerHealth] Invulnerability ended");

            // Stop visual effects
            StopHitVisualEffects();
            
            OnInvulnerabilityEnd?.Invoke();
        }

        private void Die()
        {
            if (debugInvulnerability)
                Debug.Log("[PlayerHealth] Player died");

            // Stop all visual effects immediately on death
            StopHitVisualEffects();
            
            OnDeath?.Invoke();
            PlayerEvents.OnPlayerDeath?.Invoke();
            if (_stateManager != null)
                _stateManager.ChangeState(PlayerState.Dead);
        }

        /// <summary>
        /// Heals the player by the specified amount (optional feature)
        /// </summary>
        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            OnHealthChanged?.Invoke(currentHealth);
        }

        /// <summary>
        /// Force invulnerability for external effects (optional feature)
        /// </summary>
        public void ForceInvulnerability(float duration)
        {
            isInvulnerable = true;
            invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, duration);
        }

        #region Visual Effects
        
        /// <summary>
        /// Plays random hit effect animation at player position, stays in place
        /// </summary>
        public void ApplyEffect()
        {
            // Check if effect animator exists
            if (_effectAnimator == null)
            {
                Debug.LogWarning("[PlayerHealth] Effect animator not assigned!");
                return;
            }
            
            // Play random effect - will unparent and stay at spawn position
            _effectAnimator.PlayRandomEffect(transform);
        }
        
        /// <summary>
        /// Starts all visual effects when hit (color blink + alpha flickering)
        /// </summary>
        private void StartHitVisualEffects()
        {
            if (_playerSprite == null) return;
            
            // Don't start visual effects if player is dead
            if (_stateManager != null && _stateManager.CurrentState == PlayerState.Dead)
                return;
            
            // Kill any existing sequences
            _flickerSequence?.Kill();
            
            // 1. Color Blink Effect - Flash to hit color then back to original
            _playerSprite.DOColor(_hitColor, _colorBlinkDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // After blink, return to original color
                    _playerSprite.DOColor(_originalColor, _colorBlinkDuration)
                        .SetEase(Ease.InQuad);
                });
            
            // 2. Alpha Flicker Effect - Make sprite blink during invulnerability
            StartAlphaFlickerEffect();
        }
        
        /// <summary>
        /// Creates a looping alpha flicker effect to show invulnerability
        /// </summary>
        private void StartAlphaFlickerEffect()
        {
            if (_playerSprite == null) return;
            
            // Create flicker color with reduced alpha
            Color flickerColor = new Color(_originalColor.r, _originalColor.g, _originalColor.b, _flickerAlpha);
            Color fullColor = new Color(_originalColor.r, _originalColor.g, _originalColor.b, _originalColor.a);
            
            // Create looping sequence: fade out -> fade in
            _flickerSequence = DOTween.Sequence();
            _flickerSequence.Append(_playerSprite.DOColor(flickerColor, _flickerInterval)
                .SetEase(Ease.Linear));
            _flickerSequence.Append(_playerSprite.DOColor(fullColor, _flickerInterval)
                .SetEase(Ease.Linear));
            _flickerSequence.SetLoops(-1); // Infinite loop during invulnerability
        }
        
        /// <summary>
        /// Stops all visual effects and resets sprite to normal
        /// </summary>
        private void StopHitVisualEffects()
        {
            if (_playerSprite == null) return;
            
            // Kill flicker sequence
            _flickerSequence?.Kill();
            
            // Reset to original color with full alpha smoothly
            _playerSprite.DOColor(_originalColor, 0.2f).SetEase(Ease.OutQuad);
        }

        #endregion



        #region V1 - Original Code (Commented for Rollback)
        /*
        // Original health system was basic
        // This can be extended with more features like:
        // - Health regeneration
        // - Damage resistance
        // - Health UI updates
        // - Death animations
        */
        #endregion
    }
}