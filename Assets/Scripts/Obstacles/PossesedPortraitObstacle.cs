using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using CrimsonSanctum.Audio;
using GabrielBigardi.SpriteAnimator;

public class PossessedPortraitObstacle : ObstacleBase, IActivatable
{
    [Header("Detection & Firing")]
    [SerializeField] private Transform detectionCenter; // Center point for detection square
    [SerializeField] private Vector2 detectionSize = new Vector2(10f, 8f); // Size of detection square
    [SerializeField] private float fireInterval = 3f; // Time between shots (if player in range)
    [SerializeField] private float chargeDuration = 0.8f; // Time to charge before firing
    [SerializeField] private float laserTravelTime = 0.2f; // Time for laser to travel to target
    [SerializeField] private float maxLaserDistance = 20f; // Maximum laser range
    [SerializeField] private LayerMask playerLayer; // Layer for player detection
    [SerializeField] private LayerMask laserLayerMask; // What the laser can hit (walls, player, etc.)

    [Header("Visual Effects")]
    [SerializeField] private Transform shootPoint; // Where the laser shoots from
    [SerializeField] private Light2D leftEyeLight; // Left eye 2D Light component
    [SerializeField] private Light2D rightEyeLight; // Right eye 2D Light component
    [SerializeField] private LineRenderer laserLine; // Assign LineRenderer for laser beam
    [SerializeField] private ParticleSystem laserImpactEffect; // Particle effect for laser impact
    [SerializeField] private Light2D impactLight; // Light that flashes at impact point
    [SerializeField] private Light2D laserBeamLight; // Light that follows the laser beam
    [SerializeField] private List<SpriteAnimator> eyeAnimators; // Eye sprite animators
    
    [Header("Audio")]
    [SerializeField] private List<AudioClip> sfxList; // 0: Fire, 1: Charge, 3: Impact
    [SerializeField][Range(0, 1)] private float sfxVolume = 1f;
    
    [Header("Pitch Settings")]
    [SerializeField][Range(0.5f, 1.5f)] private float chargePitchStart = 0.7f;
    [SerializeField][Range(1f, 2f)] private float chargePitchEnd = 1.5f;
    [SerializeField][Range(0.1f, 0.5f)] private float impactPitchStart = 0.2f;
    [SerializeField][Range(0.8f, 1.2f)] private float impactPitchPeak = 1.0f;
    
    [Header("Animation Settings")]
    [SerializeField][Range(0.5f, 0.95f)] private float eyeBlinkTriggerPercent = 0.8f; // When to trigger blink (80% of charge)
    [SerializeField][Range(0.3f, 0.9f)] private float impactFadeOutStartPercent = 0.7f; // When to start volume fade (70% of fall phase)
    
    [Header("Detection Visualization")]
    [SerializeField] private bool showDetectionGizmo = true;
    [SerializeField] private Color detectionGizmoColor = new Color(1f, 1f, 0f, 0.3f); // Yellow semi-transparent
    [SerializeField] private Color activeDetectionColor = new Color(1f, 0f, 0f, 0.5f); // Red semi-transparent

    // State tracking
    private Transform playerTransform;
    private Collider2D playerCollider;
    private Vector3 targetPosition;
    private bool isCharging = false;
    private bool canFire = true;
    private bool playerInZone = false;
    
    // Cached values
    private Color originalLeftEyeLightColor;
    private Color originalRightEyeLightColor;
    
    // Active components
    private GameObject activeLaserCollider;
    
    // Coroutine tracking
    private Coroutine fireCoroutine;
    private Coroutine impactPitchCoroutine;
    
    // Constants
    private const string DEATH_ZONE_TAG = "DeathZone";
    private const string BLINK_ANIMATION = "Blink";
    private const string IDLE_ANIMATION = "Idle";
    private const int SFX_INDEX_FIRE = 0;
    private const int SFX_INDEX_CHARGE = 1;
    private const int SFX_INDEX_IMPACT = 3;

    #region Unity Lifecycle
    
    protected override void Initialize()
    {
        CacheComponents();
        InitializeLaserVisuals();
        CacheOriginalColors();
        ValidateSetup();
        
        // Start detection check coroutine
        StartCoroutine(DetectionCheckLoop());
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy(); // Calls base CleanupAudioSources()
        CleanupTweens();
        CleanupPortraitAudio(); // Custom cleanup for portrait-specific audio
    }
    
    #endregion
    
    #region Player Detection
    
    #region Initialization
    
    private void CacheComponents()
    {
        FindPlayer();
    }
    
    private void InitializeLaserVisuals()
    {
        if (laserLine != null)
        {
            laserLine.enabled = false;
            laserLine.positionCount = 2;
        }

        if (laserBeamLight != null)
        {
            laserBeamLight.enabled = false;
            laserBeamLight.color = new Color(1f, 0.2f, 0.2f, 1f);
            laserBeamLight.intensity = 1.5f;
            laserBeamLight.pointLightOuterRadius = 2f;
        }
    }
    
    private void CacheOriginalColors()
    {
        if (leftEyeLight != null)
            originalLeftEyeLightColor = leftEyeLight.color;
        if (rightEyeLight != null)
            originalRightEyeLightColor = rightEyeLight.color;
    }
    
    private void ValidateSetup()
    {
        if (detectionCenter == null)
        {
            Debug.LogWarning("PossessedPortrait: Detection Center not assigned! Using portrait position.");
            // Create a default detection center at portrait position
            GameObject centerObj = new GameObject("DetectionCenter");
            centerObj.transform.SetParent(transform);
            centerObj.transform.localPosition = Vector3.zero;
            detectionCenter = centerObj.transform;
        }
    }
    
    #endregion

    #region Detection Loop
    
    /// <summary>
    /// Continuously checks if player is within the detection square
    /// </summary>
    private IEnumerator DetectionCheckLoop()
    {
        const float checkInterval = 0.2f; // Check 5 times per second
        
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            
            if (playerTransform == null)
            {
                FindPlayer();
                continue;
            }
            
            bool wasInZone = playerInZone;
            playerInZone = IsPlayerInDetectionZone();
            
            // Player just entered zone
            if (playerInZone && !wasInZone)
            {
                OnPlayerEnterZone();
            }
            // Player just left zone
            else if (!playerInZone && wasInZone)
            {
                OnPlayerExitZone();
            }
        }
    }
    
    /// <summary>
    /// Checks if player is within the detection square bounds
    /// </summary>
    private bool IsPlayerInDetectionZone()
    {
        if (playerTransform == null || detectionCenter == null)
            return false;
        
        Vector2 centerPos = detectionCenter.position;
        Vector2 playerPos = playerTransform.position;
        
        // Calculate the bounds of the detection square
        float halfWidth = detectionSize.x * 0.5f;
        float halfHeight = detectionSize.y * 0.5f;
        
        // Check if player is within bounds
        bool withinX = Mathf.Abs(playerPos.x - centerPos.x) <= halfWidth;
        bool withinY = Mathf.Abs(playerPos.y - centerPos.y) <= halfHeight;
        
        return withinX && withinY;
    }
    
    private void OnPlayerEnterZone()
    {
        StartFiring();
    }
    
    private void OnPlayerExitZone()
    {
        StopFiring();
    }
    
    #endregion

    private void FindPlayer()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (IsInLayerMask(obj, playerLayer))
            {
                playerTransform = obj.transform;
                playerCollider = obj.GetComponent<Collider2D>();
                return;
            }
        }
        
        Debug.LogWarning($"PossessedPortrait: No player found on layer mask {playerLayer.value}");
    }
    
    #endregion
    
    #region Fire Cycle

    private void StartFiring()
    {
        if (fireCoroutine == null && playerTransform != null)
        {
            fireCoroutine = StartCoroutine(FireCycle());
        }
    }

    private void StopFiring()
    {
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }

        if (isCharging)
        {
            StopChargeSound();
            isCharging = false;
            canFire = true;

            if (laserLine != null)
                laserLine.enabled = false;

            ResetEyeLightsImmediate();
        }
    }

    private IEnumerator FireCycle()
    {
        yield return new WaitForSeconds(fireInterval);
        
        while (playerInZone && playerTransform != null)
        {
            if (canFire && !isCharging)
            {
                yield return StartCoroutine(ChargeAndFire());
            }
            
            yield return new WaitForSeconds(fireInterval);
        }

        fireCoroutine = null;
    }

    private IEnumerator ChargeAndFire()
    {
        isCharging = true;
        canFire = false;
        
        // ⚠️ Show warning visual during charge
        ShowWarningVisual(chargeDuration);

        AnimateEyeLightsToRed();

        if (IsAudioClipValid(SFX_INDEX_CHARGE))
        {
            StartCoroutine(PlayChargeSoundWithPitch());
        }

        // Wait for 60% of charge duration before locking target
        float lockTargetTime = chargeDuration * 0.6f;
        yield return new WaitForSeconds(lockTargetTime);

        // Lock target position at 80% of charge duration
        if (playerTransform != null)
        {
            targetPosition = playerTransform.position;
        }

        // Wait for remaining 40% of charge duration
        float remainingTime = chargeDuration * 0.4f;
        yield return new WaitForSeconds(remainingTime);

        StopChargeSound();
        FireLaser();

        yield return new WaitForSeconds(0.5f);
        
        ResetEyeLights();
        
        isCharging = false;
        canFire = true;
    }
    
    #endregion
    
    #region Laser Firing

    private IEnumerator PlayChargeSoundWithPitch()
    {
        if (!IsAudioClipValid(SFX_INDEX_CHARGE))
            yield break;

        // Use base class persistent AudioSource system
        AudioSource chargeSoundSource = CreateObstacleAudioSource("Charge", sfxList[SFX_INDEX_CHARGE], sfxVolume, loop: true);
        if (chargeSoundSource != null)
        {
            chargeSoundSource.pitch = chargePitchStart;
            chargeSoundSource.Play();
        }
        
        yield return AnimatePitch(chargeSoundSource, chargePitchStart, chargePitchEnd, chargeDuration);
    }

    private void StopChargeSound()
    {
        StopObstacleAudioSource("Charge");
    }
    
    private IEnumerator PlayImpactSoundWithPitch(float duration)
    {
        if (!IsAudioClipValid(SFX_INDEX_IMPACT))
            yield break;

        // Use base class persistent AudioSource system
        AudioSource impactSoundSource = CreateObstacleAudioSource("Impact", sfxList[SFX_INDEX_IMPACT], sfxVolume, loop: true);
        if (impactSoundSource != null)
        {
            impactSoundSource.pitch = impactPitchStart;
            impactSoundSource.Play();
        }
        
        float halfDuration = duration * 0.3f;
        
        // Phase 1: Rise from start to peak
        yield return AnimatePitch(impactSoundSource, impactPitchStart, impactPitchPeak, halfDuration);
        
        // Phase 2: Fall from peak to start with volume fade
        yield return AnimatePitchWithFade(impactSoundSource, impactPitchPeak, impactPitchStart, halfDuration);
        
        StopImpactSound();
    }

    private void StopImpactSound()
    {
        if (impactPitchCoroutine != null)
        {
            StopCoroutine(impactPitchCoroutine);
            impactPitchCoroutine = null;
        }
        
        StopObstacleAudioSource("Impact");
    }
    
    #endregion
    
    #region Impact Effects
    
    private IEnumerator AnimatePitch(AudioSource source, float startPitch, float endPitch, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && source != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float currentPitch = Mathf.Lerp(startPitch, endPitch, progress);
            
            if (source != null)
                source.pitch = currentPitch;
            
            yield return null;
        }
    }
    
    private IEnumerator AnimatePitchWithFade(AudioSource source, float startPitch, float endPitch, float duration)
    {
        float elapsedTime = 0f;
        float initialVolume = source != null ? source.volume : sfxVolume;
        
        while (elapsedTime < duration && source != null)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float currentPitch = Mathf.Lerp(startPitch, endPitch, progress);
            
            if (source != null)
            {
                source.pitch = currentPitch;
                
                // Fade out volume in the last portion
                if (progress >= impactFadeOutStartPercent)
                {
                    float fadeProgress = (progress - impactFadeOutStartPercent) / (1f - impactFadeOutStartPercent);
                    source.volume = Mathf.Lerp(initialVolume, 0f, fadeProgress);
                }
            }
            
            yield return null;
        }
    }
    
    
    private bool IsAudioClipValid(int index)
    {
        return audioManager != null && 
               sfxList != null && 
               sfxList.Count > index && 
               sfxList[index] != null;
    }
    
    #endregion
    
    #region Impact Effects

    private void FireLaser()
    {
        if (laserLine == null) return;

        EnableLaserVisuals();

        Vector3 startPos = shootPoint != null ? shootPoint.position : transform.position;
        Vector2 direction = (targetPosition - startPos).normalized;
        
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, maxLaserDistance, laserLayerMask);
        Vector3 endPos = hit.collider != null ? hit.point : startPos + (Vector3)direction * maxLaserDistance;

        InitializeLaserBeam(startPos);
        AnimateLaserBeam(startPos, endPos, hit);
    }
    
    private void EnableLaserVisuals()
    {
        laserLine.enabled = true;
        
        if (laserBeamLight != null)
            laserBeamLight.enabled = true;
        
        if (laserLine.material.HasProperty("_Color"))
            laserLine.material.color = new Color(1, 0.2f, 0.2f, 1f);
    }
    
    private void InitializeLaserBeam(Vector3 startPos)
    {
        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, startPos);
        
        if (laserBeamLight != null)
            laserBeamLight.transform.position = startPos;
    }
    
    private void AnimateLaserBeam(Vector3 startPos, Vector3 endPos, RaycastHit2D hit)
    {
        laserLine.DOKill();
        
        DOVirtual.Float(0, 1, laserTravelTime, (progress) =>
        {
            Vector3 currentEndPos = Vector3.Lerp(startPos, endPos, progress);
            laserLine.SetPosition(1, currentEndPos);
            
            UpdateLaserBeamLight(startPos, currentEndPos);
            UpdateLaserCollider(startPos, currentEndPos);
            
            if (progress >= 0.9f)
            {
                HandleLaserHit(hit);
            }
        })
        .OnStart(() => PlayLaserFireSound())
        .OnComplete(() => HandleLaserComplete(endPos, hit.collider != null));
    }
    
    private void UpdateLaserBeamLight(Vector3 startPos, Vector3 currentEndPos)
    {
        if (laserBeamLight != null)
        {
            Vector3 lightPos = Vector3.Lerp(startPos, currentEndPos, 0.8f);
            laserBeamLight.transform.position = lightPos;
            laserBeamLight.intensity = 1.5f + Mathf.Sin(Time.time * 20f) * 0.3f;
        }
    }
    
    private void HandleLaserHit(RaycastHit2D hit)
    {
        if (hit.collider != null && IsInLayerMask(hit.collider.gameObject, playerLayer))
        {
            OnPlayerHit();
        }
    }
    
    private void PlayLaserFireSound()
    {
        if (IsAudioClipValid(SFX_INDEX_FIRE))
        {
            // Use base class persistent AudioSource system
            AudioSource fireSource = CreateObstacleAudioSource("Fire", sfxList[SFX_INDEX_FIRE], sfxVolume);
            if (fireSource != null)
                fireSource.Play();
        }
    }
    
    private void HandleLaserComplete(Vector3 endPos, bool hitSomething)
    {
        TriggerImpactEffect(endPos, hitSomething);
        FlashImpactLight(endPos);
        FadeOutLaser();
    }
    
    private void FadeOutLaser()
    {
        if (laserLine.material.HasProperty("_Color"))
        {
            laserLine.material.DOColor(new Color(1, 0.2f, 0.2f, 0f), "_Color", 0.3f)
                .OnComplete(DisableLaser);
        }
        else
        {
            DOVirtual.DelayedCall(0.3f, DisableLaser);
        }
    }
    
    private void DisableLaser()
    {
        laserLine.enabled = false;
        if (laserBeamLight != null) laserBeamLight.enabled = false;
        DestroyLaserCollider();
    }
    
    #endregion
    
    #region Eye Animations

    private void AnimateEyeLightsToRed()
    {
        Color targetRedColor = new Color(1f, 0f, 0f, 1f);

        AnimateEyeLight(leftEyeLight, targetRedColor);
        AnimateEyeLight(rightEyeLight, targetRedColor);

        float blinkTriggerTime = chargeDuration * eyeBlinkTriggerPercent;
        DOVirtual.DelayedCall(blinkTriggerTime, PlayEyeBlinkAnimation);
    }
    
    private void AnimateEyeLight(Light2D light, Color targetColor)
    {
        if (light != null)
        {
            Color startColor = light.color;
            DOVirtual.Color(startColor, targetColor, chargeDuration, (color) =>
            {
                light.color = color;
            }).SetEase(Ease.InOutSine);
        }
    }

    private void PlayEyeBlinkAnimation()
    {
        if (eyeAnimators != null && eyeAnimators.Count > 0)
        {
            foreach (var animator in eyeAnimators)
            {
                if (animator != null)
                {
                    animator.Play(BLINK_ANIMATION).SetOnComplete(() =>
                    {
                        animator.Play(IDLE_ANIMATION);
                    });
                }
            }
        }
    }

    private void ResetEyeLights()
    {
        const float resetDuration = 0.5f;
        ResetEyeLight(leftEyeLight, originalLeftEyeLightColor, resetDuration);
        ResetEyeLight(rightEyeLight, originalRightEyeLightColor, resetDuration);
    }
    
    private void ResetEyeLight(Light2D light, Color originalColor, float duration)
    {
        if (light != null)
        {
            DOTween.Kill(light);
            Color currentColor = light.color;
            DOVirtual.Color(currentColor, originalColor, duration, (color) =>
            {
                light.color = color;
            }).SetEase(Ease.InOutSine);
        }
    }

    private void ResetEyeLightsImmediate()
    {
        if (leftEyeLight != null)
        {
            DOTween.Kill(leftEyeLight);
            leftEyeLight.color = originalLeftEyeLightColor;
        }

        if (rightEyeLight != null)
        {
            DOTween.Kill(rightEyeLight);
            rightEyeLight.color = originalRightEyeLightColor;
        }
    }
    
    #endregion
    
    #region Audio Management

    private void TriggerImpactEffect(Vector3 impactPosition, bool hitSomething)
    {
        if (laserImpactEffect == null) return;
        
        ConfigureParticleEffect(impactPosition, hitSomething);
        laserImpactEffect.Play();
        PlayImpactSoundEffect();
    }
    
    private void ConfigureParticleEffect(Vector3 position, bool hitSomething)
    {
        laserImpactEffect.transform.position = position;
        laserImpactEffect.Stop(true);
        laserImpactEffect.Clear();
        
        var main = laserImpactEffect.main;
        var emission = laserImpactEffect.emission;
        
        if (hitSomething)
        {
            main.startColor = new Color(1f, 0.4f, 0f, 1f); // Orange sparks
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 18) });
        }
        else
        {
            main.startColor = new Color(1f, 0.2f, 0.2f, 0.8f); // Red dissipation
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 10) });
        }
    }
    
    private void PlayImpactSoundEffect()
    {
        if (!IsAudioClipValid(SFX_INDEX_IMPACT)) return;
        
        var main = laserImpactEffect.main;
        float particleDuration = main.duration + main.startLifetime.constantMax;
        
        if (impactPitchCoroutine != null)
        {
            StopCoroutine(impactPitchCoroutine);
        }
        impactPitchCoroutine = StartCoroutine(PlayImpactSoundWithPitch(particleDuration));
    }

    private void FlashImpactLight(Vector3 impactPosition)
    {
        if (impactLight == null) return;
        
        impactLight.transform.position = impactPosition;
        impactLight.color = new Color(1f, 0.4f, 0.1f, 1f);
        impactLight.intensity = 0f;
        
        DOVirtual.Float(0f, 2f, 0.1f, (intensity) =>
        {
            impactLight.intensity = intensity;
        })
        .OnComplete(() =>
        {
            DOVirtual.Float(2f, 0f, 0.4f, (intensity) =>
            {
                impactLight.intensity = intensity;
            }).SetEase(Ease.OutQuad);
        });
    }
    
    #endregion
    
    #region Laser Collider Management

    private void UpdateLaserCollider(Vector3 startPos, Vector3 endPos)
    {
        if (activeLaserCollider == null)
        {
            activeLaserCollider = new GameObject("LaserCollider");
            var collider = activeLaserCollider.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            activeLaserCollider.tag = DEATH_ZONE_TAG;
        }

        Vector3 direction = (endPos - startPos);
        float distance = direction.magnitude;
        Vector3 center = (startPos + endPos) * 0.5f;

        activeLaserCollider.transform.position = center;
        activeLaserCollider.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        
        var boxCollider = activeLaserCollider.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(laserLine.startWidth, distance);
        }
    }

    private void DestroyLaserCollider()
    {
        if (activeLaserCollider != null)
        {
            Destroy(activeLaserCollider);
            activeLaserCollider = null;
        }
    }
    
    #endregion
    
    #region IActivatable Implementation
    public void Activate()
    {
        // Check if player is currently in detection zone
        if (playerTransform != null && IsPlayerInDetectionZone())
        {
            playerInZone = true;
            StartFiring();
        }
    }

    public void Deactivate()
    {
        StopFiring();
        StopImpactSound();
        
        if (laserLine != null) laserLine.enabled = false;
        if (laserBeamLight != null) laserBeamLight.enabled = false;
        DestroyLaserCollider();
    }

    protected override void OnPlayerHit()
    {
        PlayHitEffect();
        //! CameraShaker.Instance?.Shake(0.4f, 0.3f);
        //! GameManager.Instance.PlayerDie();
    }
    
    #endregion
    
    #region Cleanup

    private void CleanupTweens()
    {
        DOTween.Kill(this);
    }
    
    private void CleanupPortraitAudio()
    {
        // Stop any active sounds - base class will handle AudioSource cleanup
        StopChargeSound();
        StopImpactSound();
    }
    
    #endregion
    
    #region Utilities

    /// <summary>
    /// Helper method to check if a GameObject is in a specific LayerMask
    /// </summary>
    private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return ((1 << obj.layer) & layerMask) != 0;
    }
    
    #endregion
    
    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showDetectionGizmo) return;
        
        // Get detection center position
        Vector3 centerPos = detectionCenter != null ? detectionCenter.position : transform.position;
        
        // Draw detection square
        Color gizmoColor = playerInZone ? activeDetectionColor : detectionGizmoColor;
        Gizmos.color = gizmoColor;
        
        // Draw filled rectangle
        DrawGizmoSquare(centerPos, detectionSize);
        
        // Draw border
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        DrawGizmoSquareBorder(centerPos, detectionSize);
        
        // Draw center point
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(centerPos, 0.2f);
        
        // Draw aiming line during charge
        if (isCharging && targetPosition != Vector3.zero)
        {
            Vector3 startPos = shootPoint != null ? shootPoint.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(startPos, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }
        
        // Draw line to current player position when in zone
        if (playerInZone && playerTransform != null)
        {
            Vector3 startPos = shootPoint != null ? shootPoint.position : transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPos, playerTransform.position);
        }
    }
    
    /// <summary>
    /// Draws a filled square gizmo
    /// </summary>
    private void DrawGizmoSquare(Vector3 center, Vector2 size)
    {
        Vector3[] vertices = new Vector3[4];
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        
        vertices[0] = center + new Vector3(-halfWidth, -halfHeight, 0);
        vertices[1] = center + new Vector3(halfWidth, -halfHeight, 0);
        vertices[2] = center + new Vector3(halfWidth, halfHeight, 0);
        vertices[3] = center + new Vector3(-halfWidth, halfHeight, 0);
        
        // Draw two triangles to fill the square
        Gizmos.DrawLine(vertices[0], vertices[2]);
        Gizmos.DrawLine(vertices[1], vertices[3]);
    }
    
    /// <summary>
    /// Draws the border of a square gizmo
    /// </summary>
    private void DrawGizmoSquareBorder(Vector3 center, Vector2 size)
    {
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        
        Vector3 topLeft = center + new Vector3(-halfWidth, halfHeight, 0);
        Vector3 topRight = center + new Vector3(halfWidth, halfHeight, 0);
        Vector3 bottomLeft = center + new Vector3(-halfWidth, -halfHeight, 0);
        Vector3 bottomRight = center + new Vector3(halfWidth, -halfHeight, 0);
        
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
    
    #endregion
}