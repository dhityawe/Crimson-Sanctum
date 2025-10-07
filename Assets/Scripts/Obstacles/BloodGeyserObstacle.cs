using UnityEngine;
using DG.Tweening;
using System.Collections;
using GabrielBigardi.SpriteAnimator;
using System.Collections.Generic;
using CrimsonSanctum.Audio;
using Hellmade.Sound;

public class BloodGeyserObstacle : ObstacleBase, IActivatable
{
    [Header("Geyser Behavior")]
    public GeyserTriggerType triggerType = GeyserTriggerType.Timed;
    public float minInterval = 1.5f; // If Random or Timed
    public float maxInterval = 2.5f; // If Random
    public float triggerDelay = 0.3f; // Delay before warning phase starts
    public float eruptionDuration = 0.6f;

    [Header("Visual & Audio")]
    public bool useWarningGlow = true;
    public float warningDuration = 0.4f;
    public Color warningColor = new Color(1f, 0.3f, 0.3f, 0.8f);

    public List<AudioClip> sfxList; 

    [Range(0, 1)]
    public float sfxVolume = 1f;

    [Header("Animator")]
    public SpriteAnimator spriteAnimator;

    [Header("Components")]
    public GameObject hitCollider; // Drag your HitCollider child here
    public ParticleSystem eruptionVFX; // Optional: assign if you have one

    private SpriteRenderer spriteRenderer;
    private Collider2D hitColliderComponent;
    private Tweener warningSequence;
    private Coroutine cycleCoroutine;
    private bool isErupting = false;
    private bool isTriggered = false; // Track trigger state

    protected override void Initialize()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (hitCollider == null)
        {
            Debug.LogError("BloodGeyser: HitCollider not assigned!");
            return;
        }

        hitColliderComponent = hitCollider.GetComponent<Collider2D>();
        if (hitColliderComponent != null)
            hitColliderComponent.enabled = false; // Start disabled
        
        // Start idle/ambient looping sound (sfxList[0])
        if (sfxList != null && sfxList.Count > 0 && sfxList[0] != null)
        {
            AudioSource idleSource = CreateObstacleAudioSource("Idle", sfxList[0], sfxVolume, loop: true);
            if (idleSource != null)
                idleSource.Play();
        }
        
        // Start geyser cycle
        StartCycle();
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy(); // Calls CleanupAudioSources()
        
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        if (warningSequence != null) warningSequence.Kill();
        DOTween.Kill(this);
    }

    void StartCycle()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        cycleCoroutine = StartCoroutine(GeyserCycle());
    }

    IEnumerator GeyserCycle()
    {
        while (true)
        {
            // Wait for current eruption to completely finish before scheduling next one
            yield return new WaitUntil(() => !isTriggered && !isErupting);

            float delay = triggerType == GeyserTriggerType.Random
                ? Random.Range(minInterval, maxInterval)
                : minInterval;

            yield return new WaitForSeconds(delay);

            // Double-check we're still not active before triggering
            if (!isTriggered && !isErupting)
            {
                TriggerEruption();
            }
        }
    }

    void TriggerEruption()
    {
        if (isTriggered) 
        {
            // Debug.LogWarning("Geyser: Attempted to trigger while already triggered!");
            return;
        }

        // Debug.Log("Geyser: Triggering eruption");
        isTriggered = true;

        // ▶️ Start trigger warning phase
        OnTriggerWarning();

        // ▶️ Wait for trigger delay before starting eruption warning
        DOVirtual.DelayedCall(triggerDelay, StartEruption);
    }

    #region Trigger and Eruption Phases
    protected virtual void OnTriggerWarning()
    {
        spriteAnimator.Play("OnStart");
        
        // ▶️ Play initial gurgle SFX (trigger sound) - one-shot sound
        if (sfxList != null && sfxList.Count > 1 && sfxList[1] != null)
        {
            AudioSource triggerSource = CreateObstacleAudioSource("Trigger", sfxList[1], sfxVolume);
            if (triggerSource != null)
                triggerSource.Play();
        }

        // ▶️ Add initial trigger effects here (subtle warnings, ground rumble, etc.)
        // Example: StartGroundRumble();
        // Example: PlayTriggerParticles();
    }

    void StartEruption() // Called after trigger delay
    {
        if (isErupting) 
        {
            Debug.LogWarning("Geyser: Attempted to start eruption while already erupting!");
            return;
        }

        // Debug.Log("Geyser: Starting eruption");
        isErupting = true;

        // ▶️ STEP 1: Eruption Warning Phase
        if (useWarningGlow && spriteRenderer != null)
        {
            warningSequence = spriteRenderer.DOColor(warningColor, warningDuration / 2f)
                                           .SetLoops(2, LoopType.Yoyo);
        }

        // ▶️ STEP 2: Activate HitCollider + VFX + SFX after warning
        DOVirtual.DelayedCall(warningDuration, () =>
        {
            // Enable hit collider
            if (hitColliderComponent != null)
                spriteAnimator.Play("Errupting");

                hitColliderComponent.enabled = true;

            // Play eruption SFX (looping sound during eruption)
            if (sfxList != null && sfxList.Count > 2 && sfxList[2] != null)
            {
                AudioSource eruptionSource = CreateObstacleAudioSource("Eruption", sfxList[2], sfxVolume, loop: true);
                if (eruptionSource != null)
                    eruptionSource.Play();
            }

            // Play VFX if assigned
            if (eruptionVFX != null)
                eruptionVFX.Play();

            // Optional: screen shake
            // CameraShaker.Instance?.Shake(0.2f, 0.1f);
        });

        // ▶️ STEP 3: Disable after eruption duration
        DOVirtual.DelayedCall(warningDuration + eruptionDuration, EndEruption);
    }

    void EndEruption()
    {
        // Debug.Log("Geyser: Ending eruption");
        
        // Stop eruption and trigger sounds
        StopObstacleAudioSource("Eruption", fadeOut: true, fadeTime: 0.3f);
        StopObstacleAudioSource("Trigger");
        
        // play OnEnd animation and then play Idle after its duration
        // If you know the duration of "OnEnd" animation, use a delayed call:
        spriteAnimator.Play("OnEnd").SetOnComplete(() => spriteAnimator.Play("Idle"));
        isErupting = false;
        isTriggered = false; // Reset trigger state for next cycle

        // Disable hit collider
        if (hitColliderComponent != null)
            hitColliderComponent.enabled = false;

        // Reset color
        if (spriteRenderer != null)
            spriteRenderer.DOColor(Color.white, 0.1f);

        // Idle/ambient sound is always looping in background (no need to restart)

        // Stop warning if still running
        if (warningSequence != null)
        {
            warningSequence.Kill();
            warningSequence = null;
        }
    }

    #endregion

    // ✅ IActivatable — for manual control or Room Director
    public void Activate() => TriggerEruption();
    public void Deactivate() => EndEruption();

    protected override void OnPlayerHit()
    {

    }
}

public enum GeyserTriggerType
{
    Timed,
    Random
}