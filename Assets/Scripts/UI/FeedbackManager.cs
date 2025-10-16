using UnityEngine;
using UnityEngine.UI;
using GabrielBigardi.SpriteAnimator;
using DG.Tweening;
using Assets.Scripts.Player;

public class FeedbackManager : MonoBehaviour
{
    [Header("Panel Reference")]
    [SerializeField] private Image colorPanel;
    [SerializeField] private Image feedbackPanel;

    [Header("Feedback Configs")]
    [SerializeField] private float feedbackDuration = 0.5f;
    [SerializeField] private float feedbackPanelMaxAlpha = 1f; // 0-1 range (1 = 255)
    [SerializeField] private float colorPanelMaxAlpha = 0.12f; // 0-1 range (0.12 ≈ 30/255)
    [SerializeField] private Color hurtColor = Color.red;
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;
    
    [Header("Camera Shake")]
    [SerializeField] private GameObject effectCamera;
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private int shakeVibrato = 10;
    [SerializeField] private float shakeRandomness = 90f;

    [Header("Animator Reference")]
    [SerializeField] private UISpriteAnimator feedbackPanelAnim;

    private Sequence hurtSequence;
    private Tween shakeTween;
    
    private void Start()
    {
        // Ensure effect camera starts disabled
        if (effectCamera != null)
        {
            effectCamera.SetActive(false);
        }
    }

    private void OnEnable()
    {
        PlayerHealth.TakingDamage += Hurt;
    }

    private void OnDisable()
    {
        PlayerHealth.TakingDamage -= Hurt;
    }
    
    private void Hurt()
    {
        // Kill any existing hurt animation
        if (hurtSequence != null && hurtSequence.IsActive())
        {
            hurtSequence.Kill();
        }
        
        // Play hurt animation
        feedbackPanelAnim.Play("Hurt");
        
        // Start camera shake
        StartCameraShake();
        
        // Create DOTween sequence for smooth transitions
        hurtSequence = DOTween.Sequence();
        
        float halfDuration = feedbackDuration * 0.5f;
        
        // === Feedback Panel Animation (0 → 255 → 0) ===
        if (feedbackPanel != null)
        {
            Color feedbackStartColor = feedbackPanel.color;
            feedbackStartColor.a = 0f;
            feedbackPanel.color = feedbackStartColor;
            
            // Fade in to max alpha
            hurtSequence.Append(
                feedbackPanel.DOFade(feedbackPanelMaxAlpha, halfDuration)
                    .SetEase(fadeInEase)
            );

            // Fade out back to 0
            hurtSequence.Append(
                feedbackPanel.DOFade(0f, halfDuration)
                    .SetEase(fadeOutEase)
            );
            
            feedbackPanelAnim.Play("Idle");
        }
        
        // === Color Panel Animation (0 → 30 → 0 with Red Color) ===
        if (colorPanel != null)
        {
            // Set color to red with 0 alpha
            Color colorStartColor = hurtColor;
            colorStartColor.a = 0f;
            colorPanel.color = colorStartColor;
            
            // Fade in to max alpha (parallel with feedback panel)
            hurtSequence.Insert(0f, 
                colorPanel.DOFade(colorPanelMaxAlpha, halfDuration)
                    .SetEase(fadeInEase)
            );
            
            // Fade out back to 0 (parallel with feedback panel)
            hurtSequence.Insert(halfDuration, 
                colorPanel.DOFade(0f, halfDuration)
                    .SetEase(fadeOutEase)
            );
        }
        
        // Optional: Callback when hurt animation completes
        hurtSequence.OnComplete(() =>
        {
            hurtSequence = null;
        });
    }
    
    private void StartCameraShake()
    {
        if (effectCamera == null) return;
        
        // Kill any existing shake tween
        if (shakeTween != null && shakeTween.IsActive())
        {
            shakeTween.Kill();
        }
        
        // Enable effect camera
        effectCamera.SetActive(true);
        
        // Shake the camera transform
        Vector3 originalPosition = effectCamera.transform.position;
        shakeTween = effectCamera.transform.DOShakePosition(
            feedbackDuration,
            shakeIntensity,
            shakeVibrato,
            shakeRandomness,
            false,
            true
        ).OnComplete(() =>
        {
            // Ensure camera returns to original position
            effectCamera.transform.position = originalPosition;
            
            // Disable effect camera
            effectCamera.SetActive(false);
            
            shakeTween = null;
        });
    }
    
    private void OnDestroy()
    {
        // Cleanup: Kill any active sequences
        if (hurtSequence != null && hurtSequence.IsActive())
        {
            hurtSequence.Kill();
        }
        
        // Kill shake tween
        if (shakeTween != null && shakeTween.IsActive())
        {
            shakeTween.Kill();
        }
        
        // Ensure effect camera is disabled
        if (effectCamera != null)
        {
            effectCamera.SetActive(false);
        }
    }
}
