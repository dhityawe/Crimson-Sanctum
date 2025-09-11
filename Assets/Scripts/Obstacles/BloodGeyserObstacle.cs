using UnityEngine;
using DG.Tweening;
using System.Collections;

public class BloodGeyserObstacle : ObstacleBase, IActivatable
{
    [Header("Geyser Behavior")]
    public GeyserTriggerType triggerType = GeyserTriggerType.Timed;
    public float minInterval = 1.5f; // If Random or Timed
    public float maxInterval = 2.5f; // If Random
    public float eruptionDuration = 0.6f;

    [Header("Visual & Audio")]
    public bool useWarningGlow = true;
    public float warningDuration = 0.4f;
    public Color warningColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    public string gurgleSFX = "GeyserGurgle";
    public string eruptionSFX = "GeyserErupt";
    public string dripSFX = "GeyserDrip";

    [Header("Components")]
    public GameObject hitCollider; // Drag your HitCollider child here
    public ParticleSystem eruptionVFX; // Optional: assign if you have one

    private SpriteRenderer spriteRenderer;
    private Collider2D hitColliderComponent;
    private Tweener warningSequence;
    private Coroutine cycleCoroutine;
    private bool isErupting = false;

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

        // Start geyser cycle
        StartCycle();
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
            float delay = triggerType == GeyserTriggerType.Random
                ? Random.Range(minInterval, maxInterval)
                : minInterval;

            yield return new WaitForSeconds(delay);

            TriggerEruption();
        }
    }

    void TriggerEruption()
    {
        if (isErupting) return;

        isErupting = true;

        // ▶️ STEP 1: Warning Phase
        if (useWarningGlow && spriteRenderer != null)
        {
            warningSequence = spriteRenderer.DOColor(warningColor, warningDuration / 2f)
                                           .SetLoops(2, LoopType.Yoyo);
                                        //    .OnStart(() => SoundManager.Instance?.Play(gurgleSFX));
        }

        // ▶️ STEP 2: Activate HitCollider + VFX + SFX after warning
        DOVirtual.DelayedCall(warningDuration, () =>
        {
            // Enable hit collider
            if (hitColliderComponent != null)
                hitColliderComponent.enabled = true;

            // Play eruption SFX
            // SoundManager.Instance?.Play(eruptionSFX);

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
        isErupting = false;

        // Disable hit collider
        if (hitColliderComponent != null)
            hitColliderComponent.enabled = false;

        // Reset color
        if (spriteRenderer != null)
            spriteRenderer.DOColor(Color.white, 0.1f);

        // Play drip sound
        // SoundManager.Instance?.Play(dripSFX);

        // Stop warning if still running
        if (warningSequence != null)
        {
            warningSequence.Kill();
            warningSequence = null;
        }
    }

    // ✅ IActivatable — for manual control or Room Director
    public void Activate() => TriggerEruption();
    public void Deactivate() => EndEruption();

    protected override void OnPlayerHit()
    {
        // Only kill if currently erupting
        if (isErupting)
        {
            PlayHitEffect();
            //! CameraShaker.Instance?.Shake(0.4f, 0.3f);
            //! GameManager.Instance.PlayerDie();
        }
    }

    private void OnDestroy()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        if (warningSequence != null) warningSequence.Kill();
        DOTween.Kill(this);
    }
}

public enum GeyserTriggerType
{
    Timed,
    Random
}