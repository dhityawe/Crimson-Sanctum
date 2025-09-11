using UnityEngine;
using DG.Tweening;

public enum SpikeTriggerType
{
    OnPlayerEnter,
    Timed
}

public class SpikeCeilingObstacle : ObstacleBase, IActivatable
{
    [Header("Spike Ceiling Settings")]
    public SpikeTriggerType triggerType = SpikeTriggerType.OnPlayerEnter;
    public float triggerDelay = 1f; // If timed
    public bool usePhysics = true; // Use Rigidbody2D for realistic movement
    
    [Header("Physics Settings")]
    [SerializeField] private float mass = 10f; // Mass of the spike ceiling
    [SerializeField] private float maxFallSpeed = 15f; // Terminal velocity
    [SerializeField] private float gravityScale = 3f; // How heavy it feels
    [SerializeField] private float airDrag = 0f; // Air resistance (0 = no resistance)

    [Header("Visual & Audio")]
    public bool showWarningGlow = true;
    public float warningDuration = 0.6f;
    public Color warningColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
    public string dropSFX = "SpikeCeilingDrop";
    public string impactSFX = "SpikeCeilingImpact";
    public string playerHitSFX = "Squish";

    private SpriteRenderer spriteRenderer;
    private Collider2D myCollider;
    private Rigidbody2D rb;
    private bool hasDropped = false;
    private Transform playerTransform;
    private Vector3 originalPosition;

    protected override void Initialize()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        originalPosition = transform.position;

        // Setup Rigidbody2D for physics-based movement
        if (usePhysics)
        {
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            
            // Configure for realistic physics
            rb.bodyType = RigidbodyType2D.Kinematic; // Start as kinematic, switch to dynamic when dropping
            rb.mass = mass; // Set realistic mass
            rb.gravityScale = gravityScale; // Gravity effect
            rb.linearDamping = airDrag; // Air resistance
            rb.angularDamping = 10f; // Prevent spinning
            rb.freezeRotation = true; // Keep it upright
        }

        // Get player if using proximity trigger
        if (triggerType == SpikeTriggerType.OnPlayerEnter)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
            else
                Debug.LogWarning("Spike Ceiling: Player not found for proximity trigger!");
        }

        // Start based on trigger type
        if (triggerType == SpikeTriggerType.Timed)
        {
            DOVirtual.DelayedCall(triggerDelay, TriggerDrop);
        }
        else if (triggerType == SpikeTriggerType.OnPlayerEnter)
        {
            enabled = true; // Keep Update active to check distance
        }
    }

    void Update()
    {
        if (triggerType == SpikeTriggerType.OnPlayerEnter && !hasDropped)
        {
            CheckPlayerProximity();
        }

        // Cap fall speed for physics-based movement
        if (usePhysics && hasDropped && rb != null)
        {
            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
            }
        }
    }

    void CheckPlayerProximity()
    {
        if (playerTransform != null)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            if (distance < 3f) // hardcoded trigger radius — or make it configurable
            {
                TriggerDrop();
            }
        }
    }

    public void TriggerDrop()
    {
        if (hasDropped) return;
        hasDropped = true;

        // Optional: Visual warning glow
        if (showWarningGlow && spriteRenderer != null)
        {
            spriteRenderer.DOColor(warningColor, warningDuration / 2f)
                         .OnComplete(() => spriteRenderer.DOColor(Color.white, warningDuration / 2f));
        }

        // Play drop SFX
        // SoundManager.Instance?.Play(dropSFX);

        if (usePhysics && rb != null)
        {
            // Realistic physics-based drop - just enable gravity, no artificial forces
            rb.bodyType = RigidbodyType2D.Dynamic;
            // Gravity will naturally accelerate it from 0 velocity
            // Acceleration = gravity * gravityScale * mass
        }
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasDropped && usePhysics)
        {
            // Check if we hit ground
            if (collision.gameObject.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                OnGroundImpact();
            }
            
            // Check if we hit player
            if (collision.gameObject.CompareTag("Player"))
            {
                OnPlayerHit();
            }
        }
        
        // Call base implementation for any additional logic
        base.OnCollisionEnter2D(collision);
    }

    void OnGroundImpact()
    {
        // Stop all movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Lock in place
        }

        // Play metallic CLANG
        //! SoundManager.Instance?.Play(impactSFX);

        // Optional: camera shake
        //! CameraShaker.Instance?.Shake(0.4f, 0.2f);
    }

    void OnDropComplete()
    {
        // Fallback — if didn't hit ground, just stop
        if (!hasDropped) return; // already handled

        // If no ground hit, we still stop (safety)
        // But ideally, your level design ensures ground is below
    }

    // ✅ IActivatable — for Room Director or manual control
    public void Activate() => TriggerDrop();
    public void Deactivate() { /* Can't undrop */ }

    protected override void OnPlayerHit()
    {
        // Only kill if ceiling is falling or has landed
        if (hasDropped)
        {
            PlayHitEffect();
            //! SoundManager.Instance?.Play(playerHitSFX);
            // CameraShaker.Instance?.Shake(0.5f, 0.4f);
            // GameManager.Instance.PlayerDie();
        }
    }
}