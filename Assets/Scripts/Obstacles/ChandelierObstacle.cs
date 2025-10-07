using UnityEngine;
using DG.Tweening;
using GabrielBigardi.SpriteAnimator;
using System.Collections.Generic;
using CrimsonSanctum.Audio;

public enum ChandelierBehaviorType
{
    Swing,
    Drop
}

public class ChandelierObstacle : ObstacleBase, IMovable, IActivatable
{
    [Header("Chandelier Behavior")]
    public ChandelierBehaviorType behaviorType = ChandelierBehaviorType.Swing;

    [Header("Swing Settings")]
    [SerializeField] private float swingAngle = 45f;
    [SerializeField] private float swingSpeed = 1f; // Speed of the swing (higher = faster)
    [SerializeField] private Ease swingEase = Ease.InOutSine;

    [Header("Drop Settings")]
    [SerializeField] private float dropDelay = 1f;
    [SerializeField] private float raycastDistance = 3f; // Raycast distance downward to detect player
    [SerializeField] private LayerMask playerLayer; // Layer to detect (set to Player layer)
    
    [Header("Warning Effects")]
    [SerializeField] private bool useDropWarning = true;
    [SerializeField] private ParticleSystem warningParticles; // Optional particles for drop warning
    
    [Header("Physics Settings")]
    [SerializeField] private float mass = 15f; // Mass of the chandelier (heavier than spike ceiling)
    [SerializeField] private float maxFallSpeed = 12f; // Terminal velocity
    [SerializeField] private float gravityScale = 2.5f; // How heavy it feels
    [SerializeField] private float airDrag = 0.1f; // Slight air resistance for chandelier
    
    [Header("Collider Management")]
    [SerializeField] private Collider2D childCollider; // Collider in child GameObject to disable on ground hit

    [Header("Animator & Audio")]
    public SpriteAnimator spriteAnimator;
    public List<AudioClip> sfxList; // Assign drop sound here
    [Range(0, 1)] public float sfxVolume = 1f;

    // Beneficial cached components
    private Transform playerTransform;
    
    // Components
    [SerializeField]private SpriteRenderer spriteRenderer;
    [SerializeField]private Rigidbody2D rb;
    
    // State tracking
    private bool hasDropped = false;
    private bool isWarning = false;
    private Vector3 originalLocalPosition;

    public override void ResetObstacle()
    {
        if (hasDropped == false) return; // Only reset if it has dropped

        // Reset state
        hasDropped = false;
        isWarning = false;

        // Reset position and rotation
        transform.localPosition = originalLocalPosition;
        transform.rotation = Quaternion.identity;

        // Reset Rigidbody2D if used
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic; // Reset to kinematic
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezePositionX; // Freeze Y for swing
        }

        // Re-enable child collider if assigned
        if (childCollider != null)
        {
            childCollider.enabled = true;
            // Re-enable collision with Player/DeathZone layers in case it was disabled
            int playerLayer = LayerMask.NameToLayer("Player");
            int deathZoneLayer = LayerMask.NameToLayer("DeathZone");
            Physics2D.IgnoreLayerCollision(playerLayer, deathZoneLayer, false);
        }

        // Reset animator to idle state
        if (spriteAnimator != null)
        {
            spriteAnimator.Play("Idle");
        }

        // Stop any warning particles
        if (warningParticles != null)
        {
            warningParticles.Stop();
            warningParticles.Clear();
        }

        // Stop all tweens on this transform
        DOTween.Kill(transform);

        // Restart behavior based on type
        if (behaviorType == ChandelierBehaviorType.Swing)
        {
            StartSwing();
        }
        else if (behaviorType == ChandelierBehaviorType.Drop)
        {
            StartSwing(); // Always start with swing animation
        }
    }

    protected override void Initialize()
    {
        // Cache beneficial components only (audioManager is cached in base class)
        originalLocalPosition = transform.localPosition;

        // Randomize swing angle, speed, and behavior
        swingAngle = Random.Range(10f, 20f);
        swingSpeed = Random.Range(0.4f, 0.8f);
        behaviorType = (ChandelierBehaviorType)Random.Range(0, 2); // 0 = Swing, 1 = Drop

        // Get and cache player reference for Drop behavior
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Setup Rigidbody2D for physics-based movement (for Drop behavior)
        if (behaviorType == ChandelierBehaviorType.Drop)
        {
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            // Configure for realistic physics
            rb.bodyType = RigidbodyType2D.Kinematic; // Start as kinematic, switch to dynamic when dropping
            rb.mass = mass;
            rb.gravityScale = gravityScale;
            rb.linearDamping = airDrag;
            rb.angularDamping = 10f; // Prevent spinning
            rb.freezeRotation = true; // Keep it upright
        }
        else if (behaviorType == ChandelierBehaviorType.Swing)
        {
            // For Swing behavior, ensure Y position is frozen to prevent falling
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezePositionX;
        }

        // Ensure script is enabled BEFORE starting behavior
        enabled = true;

        // Start behavior based on type
        if (behaviorType == ChandelierBehaviorType.Swing)
        {
            StartSwing();
        }
        else if (behaviorType == ChandelierBehaviorType.Drop)
        {
            // For Drop behavior, start swinging first, then drop when player approaches
            StartSwing(); // Always start with swing animation
        }
    }

    private void StartSwing()
    {
        DOTween.Kill(transform); // Kill any existing tweens

        // Calculate duration for full swing cycle based on swingSpeed (already randomized in Initialize)
        float swingDuration = 1f / swingSpeed; // Time for one direction

        // Randomly choose starting direction (true = positive angle, false = negative angle)
        bool startPositive = Random.Range(0, 2) == 0;
        float startAngle = startPositive ? swingAngle : -swingAngle;
        float targetAngle = startPositive ? -swingAngle : swingAngle;

        // Start at one extreme angle
        transform.localRotation = Quaternion.Euler(0, 0, startAngle);
        
        // Animate between the two extreme angles with callbacks at endpoints
        transform.DOLocalRotate(new Vector3(0, 0, targetAngle), swingDuration)
                .SetEase(swingEase)
                .SetLoops(-1, LoopType.Yoyo)
                .OnStepComplete(() => 
                {
                    // This callback fires every time the tween completes one direction
                    // Perfect timing for swing creak sound at extreme angles
                    PlaySwingSFX();
                });
    }

    private void CheckPlayerProximity()
    {
        if (!hasDropped && !isWarning)
        {
            // Use child collider position if available, otherwise fallback to main transform
            Vector3 raycastOrigin = childCollider != null ? childCollider.transform.position : transform.position;
            
            // Cast a ray downward from the chandelier
            RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, raycastDistance, playerLayer);
            
            if (hit.collider != null)
            {
                // Player detected below the chandelier
                TriggerDrop();
            }
        }
    }

    private void TriggerDrop()
    {
        if (hasDropped || isWarning) return;
        isWarning = true;

        // Stop swinging animation and sound
        DOTween.Kill(transform);

        // Start drop warning phase
        OnDropWarning();

        // Wait for drop delay before actually dropping
        DOVirtual.DelayedCall(dropDelay, StartDrop);
    }

    /// <summary>
    /// Called immediately when drop is triggered - override or extend for custom warning effects
    /// </summary>
    protected virtual void OnDropWarning()
    {
        // ▶️ Start warning particles if assigned
        if (useDropWarning && warningParticles != null)
        {
            warningParticles.Play();
        }
        
        // ▶️ Play warning audio if assigned

        
        // ▶️ Add custom warning effects here
        // Example: ShakeChandelier();
        // Example: PlayCreakingSound();
        // Example: SpawnDustParticles();
    }

    /// <summary>
    /// Example warning glow implementation
    /// </summary>

    void StartDrop()
    {
        if (hasDropped) return;

        spriteAnimator.Play("OnStart").SetOnComplete(() => spriteAnimator.Play("Drop"));
        hasDropped = true;

        // End warning phase
        OnDropWarningEnd();

        // Physics-based drop - similar to SpikeCeilingObstacle
        if (rb != null)
        {
            // Unfreeze Y position to allow falling (prefab has it frozen initially)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            // Reset rotation to normal and enable physics
            transform.rotation = Quaternion.identity;
            rb.bodyType = RigidbodyType2D.Dynamic;
            // Gravity will naturally accelerate it from 0 velocity
        }
        
        // Start falling/whoosh sound during drop (will stop on collision)
        // Note: sfxList[1] is collision sound, consider adding sfxList[2] for falling whoosh if desired
        // For now, we can create a temporary whoosh effect or skip if no dedicated falling sound
    }

    /// <summary>
    /// Called when warning phase ends and dropping begins - override or extend for cleanup
    /// </summary>
    protected virtual void OnDropWarningEnd()
    {        
        // ▶️ Stop warning particles
        if (warningParticles != null)
        {
            warningParticles.Stop();
        }
        
        // ▶️ Stop warning audio
        
        // ▶️ Clean up custom warning effects here
        // Example: StopShaking();
        // Example: StopCreakingSound();
    }

    void Update()
    {
        if (behaviorType == ChandelierBehaviorType.Drop && !hasDropped && !isWarning)
        {
            CheckPlayerProximity();
        }

        // Cap fall speed for physics-based movement
        if (hasDropped && rb != null)
        {
            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
            }
        }
    }

    private void PlaySwingSFX()
    {
        if (sfxList != null && sfxList.Count > 0 && sfxList[0] != null)
        {
            // Use persistent AudioSource for swing creak sound to avoid pooling conflicts
            AudioSource swingSource = CreateObstacleAudioSource("Swing", sfxList[0], sfxVolume);
            if (swingSource != null)
                swingSource.Play();
        }
    }

    public void Move()
    {
        // DOTween handles swing — nothing needed here
    }

    public void Activate()
    {
        if (behaviorType == ChandelierBehaviorType.Swing)
            StartSwing();
        else if (behaviorType == ChandelierBehaviorType.Drop)
            TriggerDrop();
    }

    public void Deactivate()
    {
        DOTween.Pause(transform);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasDropped)
        {
            // Stop any looping falling sound
            StopObstacleAudioSource("Falling");
            
            // Play collision SFX for any collision when dropped
            PlayCollisionSFX();
            
            // Check if we hit ground
            if (collision.gameObject.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                OnGroundImpact();
            }
            
            // Check if we hit player
            if (collision.gameObject.CompareTag("Player"))
            {
                OnPlayerHit();
                
                // Immediately disable Player/DeathZone collision
                int playerLayer = LayerMask.NameToLayer("Player");
                int deathZoneLayer = LayerMask.NameToLayer("DeathZone");
                Physics2D.IgnoreLayerCollision(playerLayer, deathZoneLayer, true);
                
                // Re-enable after 2.5 seconds
                DOVirtual.DelayedCall(2.5f, () =>
                {
                    Physics2D.IgnoreLayerCollision(playerLayer, deathZoneLayer, false);
                });
                
                IgnorePlayerCollision(collision.gameObject); // Exclude player collision so chandelier passes through
            }
            
            spriteAnimator.Play("Break");
        }
        
        // Call base implementation for any additional logic
        base.OnCollisionEnter2D(collision);
    }

    private void PlayCollisionSFX()
    {
        if (sfxList != null && sfxList.Count > 1 && sfxList[1] != null)
        {
            // Use persistent AudioSource for collision sound to avoid pooling conflicts
            AudioSource collisionSource = CreateObstacleAudioSource("Collision", sfxList[1], sfxVolume);
            if (collisionSource != null)
                collisionSource.Play();
        }
    }

    void OnGroundImpact()
    {
        // Swing sounds are one-shot, no need to explicitly stop them
        // They will auto-cleanup when finished
        
        // Freeze Y position to prevent bouncing
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        }

        // Disable child collider if assigned
        if (childCollider != null)
        {
            childCollider.enabled = false;
        }

        // SFX is already played in OnCollisionEnter2D via PlayCollisionSFX()

        // Optional: camera shake
        //! CameraShaker.Instance?.Shake(0.4f, 0.2f);
    }

    protected override void OnPlayerHit()
    {
        // Chandelier hits player while falling
        if (hasDropped)
        {
            PlayHitEffect();
            //! CameraShaker.Instance?.Shake(0.4f, 0.3f);
        }
    }
    
    private void IgnorePlayerCollision(GameObject player)
    {
        // Ignore collision between chandelier and player so it passes through
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null && childCollider != null)
        {
            Physics2D.IgnoreCollision(childCollider, playerCollider, true);
        }
    }

    // Clean up tweens and audio on destroy
    protected override void OnDestroy()
    {
        base.OnDestroy(); // Calls CleanupAudioSources()
        DOTween.Kill(transform); // Kill any tweens targeting this transform
    }

    // Debug visualization for raycast detection
    private void OnDrawGizmos()
    {
        if (behaviorType == ChandelierBehaviorType.Drop)
        {
            // Use child collider position if available, otherwise fallback to main transform
            Vector3 raycastOrigin = childCollider != null ? childCollider.transform.position : transform.position;
            
            // Choose color based on state
            Gizmos.color = hasDropped ? Color.red : (isWarning ? new Color(1f, 0.5f, 0f) : Color.yellow);
            
            // Draw the raycast line
            Vector3 endPos = raycastOrigin + Vector3.down * raycastDistance;
            Gizmos.DrawLine(raycastOrigin, endPos);
            
            // Draw a small sphere at the end of the raycast
            Gizmos.DrawWireSphere(endPos, 0.2f);
            
            // Draw a box around the detection area
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Vector3 boxSize = new Vector3(1f, raycastDistance, 0f);
            Vector3 boxCenter = raycastOrigin + Vector3.down * (raycastDistance * 0.5f);
            Gizmos.DrawWireCube(boxCenter, boxSize);
            
            // Draw a label
            string state = hasDropped ? "DROPPED" : (isWarning ? "WARNING" : "WAITING");
            UnityEditor.Handles.Label(raycastOrigin + Vector3.up * 0.5f, 
                $"Raycast: {raycastDistance}m\n{state}\n{(childCollider != null ? "From Child" : "From Main")}");
        }
    }
}