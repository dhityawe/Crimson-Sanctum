using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class CrawlerEnemy : Enemy
    {
        [SerializeField] private float moveTime = 1f;
        [SerializeField] private float waitTime = 1f;
        private Vector2 direction = Vector2.left;
        private float timer = 0f;
        private bool isMoving = true;

        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            base.OnTriggerEnter2D(collision);
            
            if (collision.CompareTag("Bouncable"))
            {
                direction = -direction;
            }
        }

        protected override void Update()
        {
            Wait();
            Move();
        }

        private void Wait()
        {
            if (isMoving) return;

            if (timer < waitTime)
            {
                timer += Time.deltaTime;
            }
            else
            {
                isMoving = true;
                timer = moveTime;
            }
        }

        protected override void Move()
        {
            if (!isMoving) return;

            if (timer > 0)
            {
                transform.Translate(direction * speed * Time.deltaTime);
                timer -= Time.deltaTime;
            }
            else
            {
                timer = 0f;
                isMoving = false;
            }
        }
    }
}