using Assets.Scripts.Player;
using UnityEngine;

namespace Assets.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        [SerializeField] protected float speed = 2f;
        protected bool isActive = false;

        protected virtual void Update()
        {
            Move();
        }

        protected virtual void Move()
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);
        }

        protected virtual void Activate()
        {

        }
        
        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                collision.GetComponent<PlayerHealth>().TakeDamage(1);
            }
        }
    }
}