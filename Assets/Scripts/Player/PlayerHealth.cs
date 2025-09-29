using UnityEngine;

namespace Assets.Scripts.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 5;
        private int currentHealth;

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("Player has died.");
            PlayerEvents.OnPlayerDeath?.Invoke();
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