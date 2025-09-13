using UnityEngine;
using DG.Tweening;
using GabrielBigardi.SpriteAnimator;

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
    [SerializeField] private AudioSource warningAudioSource; // Optional audio for creaking sounds
    
    [Header("Physics Settings")]
    [SerializeField] private float mass = 15f; // Mass of the chandelier (heavier than spike ceiling)
    [SerializeField] private float maxFallSpeed = 12f; // Terminal velocity
    [SerializeField] private float gravityScale = 2.5f; // How heavy it feels
    [SerializeField] private float airDrag = 0.1f; // Slight air resistance for chandelier
    
    [Header("Collider Management")]
    [SerializeField] private Collider2D childCollider; // Collider in child GameObject to disable on ground hit

    [Header("Animator")]
    public SpriteAnimator spriteAnimator;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool hasDropped = false;
    private bool isWarning = false; // Track warning state
    private Vector3 originalPosition;

    protected override void Initialize()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        originalPosition = transform.position;
        
        // Get player reference for Drop behavior
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

        // Start behavior based on type
        if (behaviorType == ChandelierBehaviorType.Swing)
        {
            StartSwing();
        }
        else if (behaviorType == ChandelierBehaviorType.Drop)
        {
            // For Drop behavior, start swinging first, then drop when player approaches
            StartSwing(); // Always start with swing animation
            enabled = true; // Keep Update active for proximity check
        }
    }

    private void StartSwing()
    {
        DOTween.Kill(transform); // Kill any existing tweens

        // Calculate duration for full swing cycle based on speed
        float swingDuration = 1f / swingSpeed; // Time for one direction

        // Start from -swingAngle and swing to +swingAngle, then loop with yoyo
        transform.localRotation = Quaternion.Euler(0, 0, -swingAngle); // Start at negative angle
        
        transform.DOLocalRotate(new Vector3(0, 0, swingAngle), swingDuration)
                .SetEase(swingEase)
                .SetLoops(-1, LoopType.Yoyo); // This will go: -angle -> +angle -> -angle -> +angle...
    }

    private void CheckPlayerProximity()
    {
        if (playerTransform != null && !hasDropped && !isWarning)
        {
            // Use child collider position if available, otherwise fallback to main transform
            Vector3 triggerPosition = childCollider != null ? childCollider.transform.position : transform.position;
            float distance = Vector2.Distance(triggerPosition, playerTransform.position);
            
            if (distance < triggerDistance)
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

        // ▶️ Start drop warning phase
        OnDropWarning();

        // ▶️ Wait for drop delay before actually dropping
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
        if (useDropWarning && warningAudioSource != null)
        {
            warningAudioSource.Play();
        }
        
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

        // ▶️ End warning phase
        OnDropWarningEnd();

        // Physics-based drop - similar to SpikeCeilingObstacle
        if (rb != null)
        {
            // Reset rotation to normal and enable physics
            transform.rotation = Quaternion.identity;
            rb.bodyType = RigidbodyType2D.Dynamic;
            // Gravity will naturally accelerate it from 0 velocity
        }

        //! SoundManager.Instance?.Play("ChandelierDrop");
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
        if (warningAudioSource != null)
        {
            warningAudioSource.Stop();
        }
        
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

        // Play metallic impact sound
        //! SoundManager.Instance?.Play("ChandelierImpact");

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