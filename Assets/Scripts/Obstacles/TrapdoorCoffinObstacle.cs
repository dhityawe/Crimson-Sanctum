using UnityEngine;
using DG.Tweening;
using GabrielBigardi.SpriteAnimator;
using System.Collections.Generic;
using CrimsonSanctum.Audio;

public class TrapdoorCoffinObstacle : ObstacleBase, IActivatable
{
    [Header("Coffin Behavior")]
    public CoffinTriggerType triggerType = CoffinTriggerType.OnPlayerProximity;
    public float triggerRadius = 2f; // If proximity-based
    public float triggerDelay = 0.5f; // Delay before opening after being triggered
    public float closeDelay = 1.5f; // Time before auto-close
    public Ease openEase = Ease.InOutQuart;
    public Ease closeEase = Ease.OutBack;

    [Header("Components")]
    public GameObject bodyCollider; // Drag child GameObject here
    public SpriteRenderer coffinLid; // The lid sprite (or whole model)

    [Header("Animation")]
    public SpriteAnimator spriteAnimator;

    [Header("Warning Effects")]
    [SerializeField] private bool useWarningGlow = false;
    [SerializeField] private Color warningGlowColor = Color.red;
    [SerializeField] private float warningGlowIntensity = 1.5f;
    [SerializeField] private ParticleSystem warningParticles; // Optional particle system for warning

    [Header("Audio")]
    [SerializeField] private List<AudioClip> sfxList;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    private Collider2D bodyColliderComponent;
    private int originalSortingOrder;
    private Color originalSpriteColor;
    private bool isOpen = false;
    private bool isTriggered = false; // Prevent multiple triggers

    protected override void Initialize()
    {
        if (bodyCollider == null)
        {
            Debug.LogError("TrapdoorCoffin: bodyCollider not assigned!");
            return;
        }

        bodyColliderComponent = bodyCollider.GetComponent<Collider2D>();
        if (bodyColliderComponent == null)
        {
            Debug.LogError("TrapdoorCoffin: bodyCollider has no Collider2D!");
            return;
        }

        if (coffinLid == null)
        {
            coffinLid = GetComponent<SpriteRenderer>();
            if (coffinLid == null)
            {
                Debug.LogError("TrapdoorCoffin: coffinLid SpriteRenderer not found!");
                return;
            }
        }

        originalSortingOrder = coffinLid.sortingOrder;
        originalSpriteColor = coffinLid.color;

        if (triggerType == CoffinTriggerType.OnPlayerProximity)
        {
            enabled = true; // Keep Update active for proximity check
        }
    }

    void Update()
    {
        if (triggerType == CoffinTriggerType.OnPlayerProximity && !isTriggered)
        {
            CheckPlayerProximity();
        }
    }

    void CheckPlayerProximity()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < triggerRadius)
            {
                Activate();
            }
        }
    }

    public void Activate()
    {
        if (isTriggered) return;

        isTriggered = true;

        // ▶️ Start warning phase
        OnTriggerWarning();

        // ▶️ Wait for trigger delay before opening
        DOVirtual.DelayedCall(triggerDelay, StartOpening);
    }

    /// <summary>
    /// Called immediately when coffin is triggered - override or extend for custom warning effects
    /// </summary>
    protected virtual void OnTriggerWarning()
    {
        // ▶️ Play creak SFX (warning sound) using persistent AudioSource
        if (sfxList != null && sfxList.Count > 2 && sfxList[2] != null)
        {
            AudioSource warningSource = CreateObstacleAudioSource("Warning", sfxList[2], sfxVolume);
            if (warningSource != null)
                warningSource.Play();
        }
        
        // ▶️ Add glow effect if enabled
        if (useWarningGlow && coffinLid != null)
        {
            StartGlowEffect();
        }
        
        // ▶️ Start warning particles if assigned
        if (warningParticles != null)
        {
            warningParticles.Play();
        }
        
        Debug.Log("Coffin: Warning phase started");
    }

    /// <summary>
    /// Example glow effect implementation
    /// </summary>
    protected virtual void StartGlowEffect()
    {
        coffinLid.DOColor(warningGlowColor * warningGlowIntensity, triggerDelay)
                 .SetEase(Ease.InOutSine);
    }

    void StartOpening()
    {
        if (isOpen) return;

        // ▶️ End warning phase
        OnWarningEnd();

        isOpen = true;

        // ▶️ Animate lid open
        // play sprite animatoor play Open and OnComplete call OnOpenComplete
        spriteAnimator.Play("Open").SetOnComplete(OnOpenComplete);
    }

    /// <summary>
    /// Called when warning phase ends and opening begins - override or extend for cleanup
    /// </summary>
    protected virtual void OnWarningEnd()
    {
        // ▶️ Stop glow effect
        if (useWarningGlow && coffinLid != null)
        {
            StopGlowEffect();
        }
        
        // ▶️ Stop warning particles
        if (warningParticles != null)
        {
            warningParticles.Stop();
        }
        
        Debug.Log("Coffin: Warning phase ended, opening started");
    }

    /// <summary>
    /// Example glow effect cleanup
    /// </summary>
    protected virtual void StopGlowEffect()
    {
        coffinLid.DOKill();
        coffinLid.color = originalSpriteColor;
    }

    void OnOpenComplete()
    {
        // ▶️ Disable bodyCollider
        if (bodyColliderComponent != null)
            bodyColliderComponent.enabled = false;

        // ▶️ Set sorting order to 6 (open state)
        coffinLid.sortingOrder = 6;

        // ▶️ Play open SFX (index 1) using persistent AudioSource
        if (sfxList != null && sfxList.Count > 1 && sfxList[1] != null)
        {
            AudioSource openSource = CreateObstacleAudioSource("Open", sfxList[1], sfxVolume);
            if (openSource != null)
                openSource.Play();
        }

        // ▶️ Auto-close after delay
        DOVirtual.DelayedCall(closeDelay, CloseCoffin);
    }

    void CloseCoffin()
    {
        if (!isOpen) return;

        // set back sorting order to original
        coffinLid.sortingOrder = originalSortingOrder;

        // ▶️ Play close SFX (index 0) using persistent AudioSource
        if (sfxList != null && sfxList.Count > 0 && sfxList[0] != null)
        {
            AudioSource closeSource = CreateObstacleAudioSource("Close", sfxList[0], sfxVolume);
            if (closeSource != null)
                closeSource.Play();
        }
        
        // if animation on complete play OnCloseComplete
        spriteAnimator.Play("Close").SetOnComplete(OnCloseComplete);
    }

    void OnCloseComplete()
    {
        // ▶️ Re-enable bodyCollider
        if (bodyColliderComponent != null)
            bodyColliderComponent.enabled = true;

        // ▶️ Restore original sorting order
        coffinLid.sortingOrder = originalSortingOrder;

        isOpen = false;
        isTriggered = false; // Reset trigger state for reuse
    }

    public void Deactivate()
    {
        if (isOpen)
            CloseCoffin();
    }

    protected override void OnPlayerHit()
    {
        // Only kill if coffin is OPEN (falling into void)
        if (isOpen)
        {
            PlayHitEffect();
            //! CameraShaker.Instance?.Shake(0.3f, 0.2f);
            //! GameManager.Instance.PlayerDie();
        }
    }

    protected override void OnDestroy()
    {
        DOTween.Kill(this);
        base.OnDestroy();
    }
}

public enum CoffinTriggerType
{
    OnPlayerProximity,
    OnPressurePlate // You can hook this up manually via other script
}