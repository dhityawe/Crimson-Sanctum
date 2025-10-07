using UnityEngine;
using GabrielBigardi.SpriteAnimator;

namespace Assets.Scripts.Player
{
    /// <summary>
    /// Makes a SpriteAnimator effect reusable by unparenting it when playing,
    /// allowing it to stay in place like a particle system.
    /// Uses only SetActive for optimal performance - no Instantiate/Destroy.
    /// </summary>
    [RequireComponent(typeof(SpriteAnimator))]
    public class ReusableEffectAnimator : MonoBehaviour
    {
        private SpriteAnimator _animator;
        private Transform _originalParent;
        private bool _isPlaying;
        
        private void Awake()
        {
            _animator = GetComponent<SpriteAnimator>();
            _originalParent = transform.parent;
        }
        
        /// <summary>
        /// Plays a random animation from the SpriteAnimationObject, unparents, and stays in place
        /// </summary>
        public void PlayRandomEffect(Transform parent = null)
        {
            if (_animator == null || _animator.SpriteAnimationObject == null ||
                _animator.SpriteAnimationObject.SpriteAnimations.Count == 0)
            {
                Debug.LogWarning("[ReusableEffectAnimator] No animations found!");
                return;
            }
            
            // Re-parent to player if provided
            if (parent != null)
            {
                transform.SetParent(parent);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            
            // Ensure effect is visible (lightweight - no Instantiate!)
            gameObject.SetActive(true);
            
            // Store current world position
            Vector3 worldPosition = transform.position;
            Quaternion worldRotation = transform.rotation;
            
            // Unparent to world space so it doesn't follow player
            transform.SetParent(null);
            
            // Restore world position (in case unparenting changed it)
            transform.position = worldPosition;
            transform.rotation = worldRotation;
            
            // Get random animation
            int randAnim = Random.Range(0, _animator.SpriteAnimationObject.SpriteAnimations.Count);
            var randomAnimation = _animator.SpriteAnimationObject.SpriteAnimations[randAnim];
            
            // Play animation
            _isPlaying = true;
            _animator.Play(randomAnimation).SetOnComplete(OnAnimationComplete);
        }
        
        /// <summary>
        /// Plays a specific animation by name, unparents, and stays in place
        /// </summary>
        public void PlayEffect(string animationName, Transform parent = null)
        {
            if (_animator == null)
            {
                Debug.LogWarning("[ReusableEffectAnimator] Animator not found!");
                return;
            }
            
            // Re-parent to player if provided
            if (parent != null)
            {
                transform.SetParent(parent);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            
            // Ensure effect is visible (lightweight - no Instantiate!)
            gameObject.SetActive(true);
            
            // Store current world position
            Vector3 worldPosition = transform.position;
            Quaternion worldRotation = transform.rotation;
            
            // Unparent to world space
            transform.SetParent(null);
            
            // Restore world position
            transform.position = worldPosition;
            transform.rotation = worldRotation;
            
            // Play animation
            _isPlaying = true;
            _animator.Play(animationName).SetOnComplete(OnAnimationComplete);
        }
        
        /// <summary>
        /// Called when animation completes
        /// </summary>
        private void OnAnimationComplete()
        {
            _isPlaying = false;
            
            // Re-parent to original parent for reuse
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            
            // Hide the effect (lightweight - no Destroy!)
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Manually reset the effect to be ready for next use
        /// </summary>
        public void ResetEffect()
        {
            _isPlaying = false;
            
            if (_originalParent != null)
            {
                transform.SetParent(_originalParent);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Check if effect is currently playing
        /// </summary>
        public bool IsPlaying => _isPlaying;
    }
}
