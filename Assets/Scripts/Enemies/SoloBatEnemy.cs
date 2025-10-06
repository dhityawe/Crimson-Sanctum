using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class SoloBatEnemy : Enemy
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        private Vector2 direction = Vector2.left;

        private void Start()
        {
            // Auto-find sprite renderer if not assigned
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                {
                    _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }
            }
        }

        protected override void Move()
        {
            transform.Translate(direction * speed * Time.deltaTime);
            
            // Flip sprite based on movement direction
            if (_spriteRenderer != null)
            {
                if (direction.x < 0) // Moving left
                {
                    _spriteRenderer.flipX = false;
                }
                else if (direction.x > 0) // Moving right
                {
                    _spriteRenderer.flipX = true;
                }
            }
        }

        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            base.OnTriggerEnter2D(collision);
            
            if (collision.CompareTag("Bouncable") || collision.CompareTag("Ground"))
            {
                direction = -direction;
            }
        }
    }
}