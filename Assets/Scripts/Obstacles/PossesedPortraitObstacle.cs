using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class PossessedPortraitObstacle : ObstacleBase, IActivatable
{
    [Header("Detection & Firing")]
    public float fireInterval = 3f; // Time between shots (if player in range)
    public float chargeDuration = 0.8f; // Time to charge before firing
    public float laserTravelTime = 0.2f; // Time for laser to travel to target
    public LayerMask laserLayerMask; // What the laser can hit (walls, player, etc.)

    [Header("Visual & Audio")]
    public Transform leftEye; // Assign eye transforms in Inspector
    public Transform rightEye;
    public Transform shootPoint; // Where the laser shoots from
    public Light2D leftEyeLight; // Left eye 2D Light component
    public Light2D rightEyeLight; // Right eye 2D Light component
    public LineRenderer laserLine; // Assign LineRenderer for laser beam
    public ParticleSystem laserImpactEffect; // Particle effect for laser impact
    public Light2D impactLight; // Light that flashes at impact point
    public Light2D laserBeamLight; // Light that follows the laser beam
    // public GameObject laserColliderPrefab; // Prefab with BoxCollider2D tagged as "DeathZone"
    [SerializeField] private List<AudioClip> sfxList;

    [Header("Components")]
    public Collider2D detectionZone; // Drag your trigger collider here

    private Transform playerTransform;
    private Vector3 targetPosition; // Locked target position when charging
    private Coroutine fireCoroutine;
    private bool isCharging = false;
    private bool canFire = true;
    private Color originalLeftEyeLightColor;
    private Color originalRightEyeLightColor;
    private GameObject activeLaserCollider; // Dynamic collider for the laser beam

    protected override void Initialize()
    {
        // Get player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning("PossessedPortrait: Player not found!");

        // Setup laser line
        if (laserLine != null)
        {
            laserLine.enabled = false;
            laserLine.positionCount = 2;
        }

        // Setup laser beam light
        if (laserBeamLight != null)
        {
            laserBeamLight.enabled = false;
            laserBeamLight.color = new Color(1f, 0.2f, 0.2f, 1f); // Red laser light
            laserBeamLight.intensity = 1.5f;
            laserBeamLight.pointLightOuterRadius = 2f;
        }

        // Store original light colors
        if (leftEyeLight != null)
            originalLeftEyeLightColor = leftEyeLight.color;
        if (rightEyeLight != null)
            originalRightEyeLightColor = rightEyeLight.color;

        // Ensure detection zone is trigger
        if (detectionZone != null)
            detectionZone.isTrigger = true;
        else
            Debug.LogError("PossessedPortrait: Detection Zone not assigned!");

        // No need to subscribe to events; use Unity's OnTriggerStay2D callback instead.
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && canFire && !isCharging)
        {
            if (fireCoroutine == null)
                fireCoroutine = StartCoroutine(FireCycle());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Stop all firing activity when player leaves detection zone
            if (fireCoroutine != null)
            {
                StopCoroutine(fireCoroutine);
                fireCoroutine = null;
            }

            // If currently charging, interrupt it
            if (isCharging)
            {
                StopAllCoroutines(); // This will stop the ChargeAndFire coroutine
                isCharging = false;
                canFire = true;

                // Hide laser if it's visible
                if (laserLine != null)
                    laserLine.enabled = false;

                // Reset eye lights to original colors immediately
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
        }
    }

    IEnumerator FireCycle()
    {
        while (playerTransform != null && detectionZone.IsTouching(playerTransform.GetComponent<Collider2D>()))
        {
            yield return new WaitForSeconds(fireInterval);

            if (!canFire || isCharging) continue;

            StartCoroutine(ChargeAndFire());
        }

        fireCoroutine = null;
    }

    IEnumerator ChargeAndFire()
    {
        isCharging = true;
        canFire = false;

        // ▶️ STEP 1: Lock target position when charging starts
        targetPosition = playerTransform.position;

        // ▶️ STEP 2: Look at locked target position
        if (leftEye != null) leftEye.LookAt(targetPosition, Vector3.back);
        if (rightEye != null) rightEye.LookAt(targetPosition, Vector3.back);

        // ▶️ STEP 3: Play whisper + start glow and eye lights
        //! SoundManager.Instance?.Play(whisperSFX);

        // Smoothly transition eye lights to red during charging
        AnimateEyeLightsToRed();

        // ▶️ STEP 4: Play charge sound
        //! DOVirtual.DelayedCall(chargeDuration * 0.3f, () => SoundManager.Instance?.Play(chargeSFX));

        // ▶️ STEP 5: Fire laser after charge
        yield return new WaitForSeconds(chargeDuration);

        FireLaser();

        // ▶️ STEP 6: Cooldown
        yield return new WaitForSeconds(0.5f);
        
        // Reset eye lights back to original colors
        ResetEyeLights();
        
        isCharging = false;
        canFire = true;
    }

    void FireLaser()
    {
        if (laserLine == null) return;

        // Enable laser visual and light
        laserLine.enabled = true;
        if (laserBeamLight != null)
        {
            laserBeamLight.enabled = true;
        }

        // Use shootPoint if assigned, otherwise fallback to eye calculation
        Vector3 startPos;
        if (shootPoint != null)
        {
            startPos = shootPoint.position;
        }
        else
        {
            // Fallback: calculate from eyes
            startPos = leftEye != null ? leftEye.position : transform.position;
            if (rightEye != null)
                startPos = (leftEye.position + rightEye.position) * 0.5f;
        }

        // Use locked target position instead of current player position
        Vector2 direction = (targetPosition - startPos).normalized;
        
        // Raycast to find hit point (use reasonable max distance based on detection zone size)
        float maxDistance = 20f; // Adjust based on your level design
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, maxDistance, laserLayerMask);

        Vector3 endPos = hit.collider != null ? hit.point : startPos + (Vector3)direction * maxDistance;

        // Start laser beam from shoot point
        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, startPos); // Start with no length

        // Position laser light at start
        if (laserBeamLight != null)
        {
            laserBeamLight.transform.position = startPos;
        }

        // Animate laser traveling to target over time
        laserLine.DOKill();
        
        // Flash effect at start
        if (laserLine.material.HasProperty("_Color"))
        {
            laserLine.material.color = new Color(1, 0.2f, 0.2f, 1f);
        }

        // Animate laser extending to target
        DOVirtual.Float(0, 1, laserTravelTime, (progress) =>
        {
            Vector3 currentEndPos = Vector3.Lerp(startPos, endPos, progress);
            laserLine.SetPosition(1, currentEndPos);
            
            // Move laser light to follow the laser beam tip
            if (laserBeamLight != null)
            {
                Vector3 lightPos = Vector3.Lerp(startPos, currentEndPos, 0.8f); // Slightly behind the tip
                laserBeamLight.transform.position = lightPos;
                
                // Pulsing intensity effect
                laserBeamLight.intensity = 1.5f + Mathf.Sin(Time.time * 20f) * 0.3f;
            }
            
            // Update laser collider to match the current laser line
            UpdateLaserCollider(startPos, currentEndPos);
            
            // Check for hits during travel (more realistic collision detection)
            if (progress >= 0.9f) // Near the end of travel
            {
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    OnPlayerHit();
                }
                else if (hit.collider != null)
                {
                    // Play sizzle if hit wall/obstacle
                    //! SoundManager.Instance?.Play(sizzleSFX);
                    //! CameraShaker.Instance?.Shake(0.1f, 0.05f);
                }
            }
        })
        .OnStart(() =>
        {
            // Play laser SFX when it starts traveling
            //! SoundManager.Instance?.Play(laserSFX);
        })
        .OnComplete(() =>
        {
            // Trigger impact effect at the end position
            TriggerImpactEffect(endPos, hit.collider != null);
            
            // Flash impact light
            FlashImpactLight(endPos);
            
            // Fade out laser after it reaches target
            if (laserLine.material.HasProperty("_Color"))
            {
                laserLine.material.DOColor(new Color(1, 0.2f, 0.2f, 0f), "_Color", 0.3f)
                    .OnComplete(() => {
                        laserLine.enabled = false;
                        if (laserBeamLight != null) laserBeamLight.enabled = false;
                        DestroyLaserCollider(); // Remove collider when laser disappears
                    });
            }
            else
            {
                DOVirtual.DelayedCall(0.3f, () => {
                    laserLine.enabled = false;
                    if (laserBeamLight != null) laserBeamLight.enabled = false;
                    DestroyLaserCollider(); // Remove collider when laser disappears
                });
            }
        });
    }

    void AnimateEyeLightsToRed()
    {
        // Smoothly transition both eye lights to red (255, 0, 0)
        Color targetRedColor = new Color(1f, 0f, 0f, 1f); // Full red

        if (leftEyeLight != null)
        {
            Color startColor = leftEyeLight.color;
            DOVirtual.Color(startColor, targetRedColor, chargeDuration, (color) =>
            {
                leftEyeLight.color = color;
            }).SetEase(Ease.InOutSine);
        }

        if (rightEyeLight != null)
        {
            Color startColor = rightEyeLight.color;
            DOVirtual.Color(startColor, targetRedColor, chargeDuration, (color) =>
            {
                rightEyeLight.color = color;
            }).SetEase(Ease.InOutSine);
        }
    }

    void ResetEyeLights()
    {
        // Smoothly transition eye lights back to original colors
        float resetDuration = 0.5f; // Duration for cooldown transition

        if (leftEyeLight != null)
        {
            DOTween.Kill(leftEyeLight); // Stop any ongoing color animation
            Color currentColor = leftEyeLight.color;
            DOVirtual.Color(currentColor, originalLeftEyeLightColor, resetDuration, (color) =>
            {
                leftEyeLight.color = color;
            }).SetEase(Ease.InOutSine);
        }

        if (rightEyeLight != null)
        {
            DOTween.Kill(rightEyeLight); // Stop any ongoing color animation
            Color currentColor = rightEyeLight.color;
            DOVirtual.Color(currentColor, originalRightEyeLightColor, resetDuration, (color) =>
            {
                rightEyeLight.color = color;
            }).SetEase(Ease.InOutSine);
        }
    }

    void TriggerImpactEffect(Vector3 impactPosition, bool hitSomething)
    {
        if (laserImpactEffect != null)
        {
            // Position the particle system at the impact point
            laserImpactEffect.transform.position = impactPosition;
            
            // Stop any existing particles and clear them
            laserImpactEffect.Stop(true);
            laserImpactEffect.Clear();
            
            // Configure particle system based on what was hit
            var main = laserImpactEffect.main;
            var emission = laserImpactEffect.emission;
            
            if (hitSomething)
            {
                // More intense effect when hitting something solid
                main.startColor = new Color(1f, 0.4f, 0f, 1f); // Orange sparks
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0.0f, 18) // Single burst of 18 particles
                });
            }
            else
            {
                // Lighter effect when laser reaches max distance
                main.startColor = new Color(1f, 0.2f, 0.2f, 0.8f); // Red dissipation
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0.0f, 10) // Single burst of 10 particles
                });
            }
            
            // Play the particle effect once
            laserImpactEffect.Play();
        }
    }

    void FlashImpactLight(Vector3 impactPosition)
    {
        if (impactLight != null)
        {
            // Position the light at impact point
            impactLight.transform.position = impactPosition;
            
            // Configure light for impact flash
            impactLight.color = new Color(1f, 0.4f, 0.1f, 1f); // Bright orange
            impactLight.intensity = 0f; // Start invisible
            
            // Flash sequence: fade in quickly, then fade out using DOVirtual
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
    }

    void UpdateLaserCollider(Vector3 startPos, Vector3 endPos)
    {
        // Create collider if it doesn't exist
        if (activeLaserCollider == null)
        {

            // Create a simple collider GameObject if no prefab provided
            activeLaserCollider = new GameObject("LaserCollider");
            var collider = activeLaserCollider.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            activeLaserCollider.tag = "DeathZone";
        }

        // Calculate laser properties
        Vector3 direction = (endPos - startPos);
        float distance = direction.magnitude;
        Vector3 center = (startPos + endPos) * 0.5f;

        // Position and scale the collider to match the laser
        activeLaserCollider.transform.position = center;
        activeLaserCollider.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
        
        // Set collider size (width matches laser width, length matches distance)
        var boxCollider = activeLaserCollider.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(laserLine.startWidth, distance);
        }
    }

    void DestroyLaserCollider()
    {
        if (activeLaserCollider != null)
        {
            Destroy(activeLaserCollider);
            activeLaserCollider = null;
        }
    }
    
    // ✅ IActivatable — for manual control or Room Director
    public void Activate()
    {
        if (detectionZone != null && fireCoroutine == null && playerTransform != null)
            fireCoroutine = StartCoroutine(FireCycle());
    }

    public void Deactivate()
    {
        if (fireCoroutine != null)
        {
            StopCoroutine(fireCoroutine);
            fireCoroutine = null;
        }
        isCharging = false;
        canFire = true;
        if (laserLine != null) laserLine.enabled = false;
        if (laserBeamLight != null) laserBeamLight.enabled = false;
        DestroyLaserCollider(); // Clean up laser collider
    }

    protected override void OnPlayerHit()
    {
        PlayHitEffect();
        //! CameraShaker.Instance?.Shake(0.4f, 0.3f);
        //! GameManager.Instance.PlayerDie();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
}