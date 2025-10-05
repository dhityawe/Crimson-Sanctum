using UnityEngine;
using System;

namespace Assets.Scripts.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 5;
        
        [Header("Invulnerability Settings")]
        [SerializeField] private float invulnerabilityDuration = 1.5f;
        [SerializeField] private bool debugInvulnerability = false;
        
        // Health state
        private int currentHealth;
        private PlayerStateManager _stateManager;
        
        // Invulnerability state
        private bool isInvulnerable = false;
        private float invulnerabilityTimer = 0f;

        #region Actions
        public static event Action OnDeath;
        public static event Action<int> OnHealthChanged; // Event for UI updates
        public static event Action OnInvulnerabilityStart;
        public static event Action OnInvulnerabilityEnd;
        #endregion
        
        public bool IsInvulnerable => isInvulnerable;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        private void Awake()
        {
            _stateManager = GetComponent<PlayerStateManager>();
            currentHealth = maxHealth;
        }
        
        private void Update()
        {
            UpdateInvulnerability();
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
            
            OnInvulnerabilityStart?.Invoke();
        }
        
        private void UpdateInvulnerability()
        {
            if (isInvulnerable)
            {
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
            
            OnInvulnerabilityEnd?.Invoke();
        }

        private void Die()
        {
            if (debugInvulnerability)
                Debug.Log("[PlayerHealth] Player died");
            
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