using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class BatSwarmEnemy : Enemy
    {
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField] private Transform batBody;
        [SerializeField] private Collider2D activationTriggerCollider;
        [SerializeField] private SpriteRenderer _spriteRenderer;

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

        private void OnDrawGizmos()
        {
            if (pointA != null && pointB != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pointA.position, pointB.position);
            }
        }

        protected override void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player") && !isActive)
            {
                Activate();
                return;
            }

            base.OnTriggerEnter2D(collision);
        }

        protected override void Activate()
        {
            isActive = true;
            activationTriggerCollider.enabled = false;
            batBody.transform.position = pointA.position;
        }

        protected override void Move()
        {
            if (!isActive) return;

            if (Vector2.Distance(batBody.transform.position, pointB.position) > 0.1f)
            {
                Vector2 previousPosition = batBody.transform.position;
                batBody.transform.position = Vector2.MoveTowards(batBody.transform.position, pointB.position, speed * Time.deltaTime);
                
                // Flip sprite based on movement direction
                if (_spriteRenderer != null)
                {
                    Vector2 moveDirection = (Vector2)batBody.transform.position - previousPosition;
                    
                    if (moveDirection.x < 0) // Moving left
                    {
                        _spriteRenderer.flipX = true;
                    }
                    else if (moveDirection.x > 0) // Moving right
                    {
                        _spriteRenderer.flipX = false;
                    }
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
