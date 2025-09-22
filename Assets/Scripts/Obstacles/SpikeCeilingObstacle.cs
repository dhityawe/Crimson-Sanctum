using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using CrimsonSanctum.Audio;
using GabrielBigardi.SpriteAnimator;

public enum SpikeTriggerType
{
    OnPlayerEnter,
    Timed
}

public class SpikeCeilingObstacle : ObstacleBase, IActivatable
{
    [Header("Spike Ceiling Settings")]
    public SpikeTriggerType triggerType = SpikeTriggerType.OnPlayerEnter;
    public float triggerDelay = 1f; // Delay before drop (works for both trigger types)
    public float triggerDistance = 3f; // Distance from player to trigger drop
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
    public List<AudioClip> listSFX;
    [Range(0, 1)] public float sfxVolume = 1f;

    [Header("Components")]
    [SerializeField] private SpriteAnimator spriteAnimator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D thisCollider;
    [SerializeField] private Rigidbody2D rb;
    
    // Cached components
    private AudioManager audioManager;
    private bool hasDropped = false;
    private bool hasTriggered = false; // Prevent multiple triggers for proximity
    private Transform playerTransform;
    private Vector3 originalPosition;
    private float triggerDistanceSquared; // Cache squared distance for performance

    protected override void Initialize()
    {
        // Cache components and values
        originalPosition = transform.position;
        audioManager = AudioManager.Instance;
        triggerDistanceSquared = triggerDistance * triggerDistance; // Cache for performance
        
        spriteAnimator?.Play("Idle");

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
            {
                playerTransform = player.transform;
                enabled = true; // Keep Update active to check distance
            }
        }

        // Start based on trigger type
        if (triggerType == SpikeTriggerType.Timed)
        {
            DOVirtual.DelayedCall(triggerDelay, TriggerDrop);
        }
    }

    void Update()
    {
        if (triggerType == SpikeTriggerType.OnPlayerEnter && !hasTriggered)
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
            // Use squared distance for better performance (avoids sqrt calculation)
            float distanceSquared = (transform.position - playerTransform.position).sqrMagnitude;
            if (distanceSquared < triggerDistanceSquared)
            {
                // Trigger with delay, just like timed mode
                hasTriggered = true; // Prevent multiple triggers
                DOVirtual.DelayedCall(triggerDelay, TriggerDrop);
            }
        }
    }

    public void TriggerDrop()
    {
        if (hasDropped) return;
        spriteAnimator.Play("OnDrop");
        audioManager.PlaySFX(listSFX[0], sfxVolume);
        
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
            thisCollider.enabled = false;
        }
        
        spriteAnimator.Play("OnHit");

        // Play metallic CLANG
        if (audioManager != null && listSFX != null && listSFX.Count > 1 && listSFX[1] != null)
        {
            audioManager.PlaySFX(listSFX[1], sfxVolume);
        }
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
            spriteAnimator.Play("OnHit");
            if (audioManager != null && listSFX != null && listSFX.Count > 2 && listSFX[2] != null)
            {
                audioManager.PlaySFX(listSFX[2], sfxVolume);
            }
        }
    }
}