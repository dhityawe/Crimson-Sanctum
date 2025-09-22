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
    [SerializeField] private float triggerDistance = 3f;
    
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
    private AudioManager audioManager;
    
    // Components
    [SerializeField]private SpriteRenderer spriteRenderer;
    [SerializeField]private Rigidbody2D rb;
    
    // State tracking
    private bool hasDropped = false;
    private bool isWarning = false;
    private Vector3 originalPosition;
    
    // Swing SFX tracking
    private bool hasPlayedPositiveAngleSFX = false;
    private bool hasPlayedNegativeAngleSFX = false;
    private float angleThreshold = 5f; // Tolerance for angle detection
    private bool allowSwingSFX = false; // Prevent initial SFX on start

    protected override void Initialize()
    {
        // Cache beneficial components only
        audioManager = AudioManager.Instance;
        originalPosition = transform.position;
        
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

        // Reset SFX flags and disable SFX initially
        hasPlayedPositiveAngleSFX = false;
        hasPlayedNegativeAngleSFX = false;
        allowSwingSFX = false;

        // Calculate duration for full swing cycle based on swingSpeed (already randomized in Initialize)
        float swingDuration = 1f / swingSpeed; // Time for one direction

        // Randomly choose starting direction (true = positive angle, false = negative angle)
        bool startPositive = Random.Range(0, 2) == 0;
        float startAngle = startPositive ? swingAngle : -swingAngle;
        float targetAngle = startPositive ? -swingAngle : swingAngle;

        // Start at one extreme angle
        transform.localRotation = Quaternion.Euler(0, 0, startAngle);
        
        // Animate between the two extreme angles (never passing through 0)
        transform.DOLocalRotate(new Vector3(0, 0, targetAngle), swingDuration)
                .SetEase(swingEase)
                .SetLoops(-1, LoopType.Yoyo); // This will go: startAngle -> targetAngle -> startAngle...

        // Enable swing SFX after a very short delay
        DOVirtual.DelayedCall(0.1f, () => allowSwingSFX = true);
    }

    private void CheckPlayerProximity()
    {
        if (playerTransform != null && !hasDropped && !isWarning)
        {
            // Use child collider position if available, otherwise fallback to main transform
            Vector3 triggerPosition = childCollider != null ? childCollider.transform.position : transform.position;
            float distanceSquared = (triggerPosition - playerTransform.position).sqrMagnitude;
            float triggerDistanceSquared = triggerDistance * triggerDistance;
            
            if (distanceSquared < triggerDistanceSquared)
            {
                TriggerDrop();
            }
        }
    }

    private void TriggerDrop()
    {
        if (hasDropped || isWarning) return;
        isWarning = true;

        // Stop swinging animation
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
            // Reset rotation to normal and enable physics
            transform.rotation = Quaternion.identity;
            rb.bodyType = RigidbodyType2D.Dynamic;
            // Gravity will naturally accelerate it from 0 velocity
        }
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

        // Check swing angles for SFX (only when swinging)
        if ((behaviorType == ChandelierBehaviorType.Swing || 
            (behaviorType == ChandelierBehaviorType.Drop && !hasDropped && !isWarning)))
        {
            CheckSwingAngleSFX();
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

    private void CheckSwingAngleSFX()
    {
        // Don't play SFX if not allowed yet (prevents initial startup sound)
        if (!allowSwingSFX) return;

        float currentAngle = transform.localRotation.eulerAngles.z;
        // Convert angle to -180 to 180 range
        if (currentAngle > 180f) currentAngle -= 360f;

        // Check if we reached positive swing angle
        if (Mathf.Abs(currentAngle - swingAngle) < angleThreshold && !hasPlayedPositiveAngleSFX)
        {
            PlaySwingSFX();
            hasPlayedPositiveAngleSFX = true;
            hasPlayedNegativeAngleSFX = false; // Reset the other flag
        }
        // Check if we reached negative swing angle
        else if (Mathf.Abs(currentAngle - (-swingAngle)) < angleThreshold && !hasPlayedNegativeAngleSFX)
        {
            PlaySwingSFX();
            hasPlayedNegativeAngleSFX = true;
            hasPlayedPositiveAngleSFX = false; // Reset the other flag
        }
    }

    private void PlaySwingSFX()
    {
        if (sfxList != null && sfxList.Count > 0 && sfxList[0] != null && audioManager != null)
        {
            audioManager.PlaySFX(sfxList[0], sfxVolume);
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
            // Play collision SFX for any collision when dropped
            PlayCollisionSFX();
            
            // Check if we hit ground
            if (collision.gameObject.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                OnGroundImpact();
            }
            spriteAnimator.Play("Break");
        }
        
        // Call base implementation for any additional logic
        base.OnCollisionEnter2D(collision);
    }

    private void PlayCollisionSFX()
    {
        if (sfxList != null && sfxList.Count > 1 && sfxList[1] != null && audioManager != null)
        {
            audioManager.PlaySFX(sfxList[1], sfxVolume);
        }
    }

    void OnGroundImpact()
    {
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

    }

    // Clean up tweens on destroy
    private void OnDestroy()
    {
        DOTween.Kill(transform); // Kill any tweens targeting this transform
    }

    // Debug visualization for trigger distance
    private void OnDrawGizmos()
    {
        if (behaviorType == ChandelierBehaviorType.Drop)
        {
            // Use child collider position if available, otherwise fallback to main transform
            Vector3 triggerPosition = childCollider != null ? childCollider.transform.position : transform.position;
            
            // Draw trigger distance as a wire sphere
            Gizmos.color = hasDropped ? Color.red : (isWarning ? new Color(1f, 0.5f, 0f) : Color.yellow);
            Gizmos.DrawWireSphere(triggerPosition, triggerDistance);
            
            // Draw a label
            string state = hasDropped ? "DROPPED" : (isWarning ? "WARNING" : "WAITING");
            UnityEditor.Handles.Label(triggerPosition + Vector3.up * (triggerDistance + 0.5f), 
                $"Trigger: {triggerDistance}m\n{state}\n{(childCollider != null ? "From Child" : "From Main")}");
        }
    }
}