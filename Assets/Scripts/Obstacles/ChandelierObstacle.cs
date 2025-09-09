using UnityEngine;
using DG.Tweening;

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
    [SerializeField] private float dropDistance = 3f; // How far it falls
    [SerializeField] private float dropDuration = 0.5f;
    [SerializeField] private Ease dropEase = Ease.InOutQuart;
    [SerializeField] private bool usePhysicsOnDrop = false; // True = Rigidbody, False = Tween
    [SerializeField] private bool dropOnPlayerApproach = false;
    [SerializeField] private float triggerDistance = 3f;

    [Header("Visual Warning")]
    [SerializeField] private bool showWarningGlow = true;
    [SerializeField] private float warningDuration = 0.5f;
    [SerializeField] private Color warningColor = new Color(1, 0.3f, 0.3f, 0.8f);

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Sequence swingSequence;
    private bool hasDropped = false;

    protected override void Initialize()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (dropOnPlayerApproach)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        // Start behavior based on type
        if (behaviorType == ChandelierBehaviorType.Swing)
        {
            // if (data?.activationDelay > 0)
            //     DOVirtual.DelayedCall(data.activationDelay, StartSwing);
            // else
                StartSwing();
        }
        else if (behaviorType == ChandelierBehaviorType.Drop)
        {
            if (dropOnPlayerApproach)
            {
                enabled = true; // Keep Update active for proximity check
            }
            else
            {
                DOVirtual.DelayedCall(dropDelay, TriggerDrop);
            }
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
        if (playerTransform != null && !hasDropped)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            if (distance < triggerDistance)
            {
                TriggerDrop();
            }
        }
    }

    private void TriggerDrop()
    {
        if (hasDropped) return;
        hasDropped = true;

        if (showWarningGlow && spriteRenderer != null)
        {
            // Optional: Pulse red before drop
            Sequence warnSeq = DOTween.Sequence()
                .Append(spriteRenderer.DOColor(warningColor, warningDuration / 2f))
                .Append(spriteRenderer.DOColor(Color.white, warningDuration / 2f));
        }

        if (usePhysicsOnDrop)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = Vector2.down * (dropDistance / dropDuration); // approximate force
            }
        }
        else
        {
            // Tween-based drop — designer-controlled, frame-perfect
            transform.DOMoveY(transform.position.y - dropDistance, dropDuration)
                   .SetEase(dropEase)
                   .OnComplete(() =>
                   {
                       // Optional: shake camera or play heavy impact SFX
                    //!    SoundManager.Instance?.Play("ChandelierImpact");
                    //!    CameraShaker.Instance?.Shake(0.3f, 0.2f);
                   });
        }

        //! SoundManager.Instance?.Play("ChandelierDrop");
    }

    void Update()
    {
        if (behaviorType == ChandelierBehaviorType.Drop && dropOnPlayerApproach)
        {
            CheckPlayerProximity();
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
        else if (behaviorType == ChandelierBehaviorType.Drop && !dropOnPlayerApproach)
            TriggerDrop();
    }

    public void Deactivate()
    {
        DOTween.Pause(transform);
    }

    protected override void OnPlayerHit()
    {
        PlayHitEffect();

        // Optional: screen shake on hit
        //! CameraShaker.Instance?.Shake(0.4f, 0.3f);

        //! GameManager.Instance.PlayerDie();
    }

    // Clean up tweens on destroy
    private void OnDestroy()
    {
        DOTween.Kill(transform); // Kill any tweens targeting this transform
    }
}