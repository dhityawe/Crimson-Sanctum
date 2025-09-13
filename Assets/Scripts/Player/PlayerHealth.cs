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
        }
    }
}