using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class SoloBatEnemy : Enemy
    {
        private Vector2 direction = Vector2.left;

        protected override void Move()
        {
            transform.Translate(direction * speed * Time.deltaTime);
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