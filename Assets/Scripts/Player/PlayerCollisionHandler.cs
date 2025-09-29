using UnityEngine;
using System;

namespace Assets.Scripts.Player
{
    public class PlayerCollisionHandler : MonoBehaviour
{
    public event Action<Collision2D> OnCollisionEnter;
    public event Action<Collision2D> OnCollisionExit;
    public event Action<Collider2D> OnTriggerEnter;
    public event Action<Collider2D> OnTriggerExit;
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnCollisionEnter?.Invoke(collision);
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        OnCollisionExit?.Invoke(collision);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        OnTriggerEnter?.Invoke(other);
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        OnTriggerExit?.Invoke(other);
    }

    #region V1 - Original Code (Commented for Rollback)
    /*
    // Original collision handling was done in individual components
    // This centralized approach allows for better event management
    */
    #endregion
    }
}
