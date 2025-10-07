using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using CrimsonSanctum.Audio;
using GabrielBigardi.SpriteAnimator;

public class SpikeCeilingObstacle : ObstacleBase, IActivatable
{
    [Header("Spike Ceiling Settings")]
    [SerializeField] private float triggerDelay = 1f; // Delay before drop after player detection
    [SerializeField] private float raycastDistance = 3f; // Raycast distance downward to detect player
    [SerializeField] private LayerMask playerLayer; // Layer to detect (set to Player layer)
    [SerializeField] private bool usePhysics = true; // Use Rigidbody2D for realistic movement
    
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
    
    // Cached components and state
    private new AudioManager audioManager;
    private Vector3 originalPosition;
    
    // State flags
    private bool hasDropped = false;
    private bool hasTriggered = false; // Prevent multiple triggers

    protected override void Initialize()
    {
        // Cache components and values at start
        originalPosition = transform.position;
        audioManager = AudioManager.Instance;
        
        // Cache and validate components
        if (spriteAnimator == null)
            spriteAnimator = GetComponent<SpriteAnimator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (thisCollider == null)
            thisCollider = GetComponent<Collider2D>();
        
        spriteAnimator?.Play("Idle");

        // Setup Rigidbody2D for physics-based movement
        if (usePhysics)
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
                if (rb == null)
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

        // Keep Update active to check for player proximity via raycast
        enabled = true;
    }

    void Update()
    {
        // Check for player proximity if not yet triggered
        if (!hasTriggered)
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
        // Cast a ray downward from the spike ceiling
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, raycastDistance, playerLayer);
        
        if (hit.collider != null)
        {
            // Player detected below the spike ceiling
            hasTriggered = true; // Prevent multiple triggers
            DOVirtual.DelayedCall(triggerDelay, TriggerDrop);
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
                IgnorePlayerCollision(collision.gameObject); // Exclude player collision so spike passes through
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

    private void IgnorePlayerCollision(GameObject player)
    {
        // Ignore collision between spike ceiling and player
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null && thisCollider != null)
        {
            Physics2D.IgnoreCollision(thisCollider, playerCollider, true);
        }
    }

    // Visualize the raycast range in the editor
    private void OnDrawGizmos()
    {
        // Draw the raycast line
        Gizmos.color = hasTriggered ? Color.red : Color.yellow;
        Vector3 startPos = Application.isPlaying ? transform.position : transform.position;
        Vector3 endPos = startPos + Vector3.down * raycastDistance;
        
        Gizmos.DrawLine(startPos, endPos);
        
        // Draw a small sphere at the end of the raycast
        Gizmos.DrawWireSphere(endPos, 0.2f);
        
        // Draw a box around the detection area
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 boxSize = new Vector3(1f, raycastDistance, 0f);
        Vector3 boxCenter = startPos + Vector3.down * (raycastDistance * 0.5f);
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}