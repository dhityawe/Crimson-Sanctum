using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CrimsonSanctum.UI
{
    [RequireComponent(typeof(Image))]
    public class MaskedFadeOverlay : MonoBehaviour
    {
        [Header("Mask Mode")]
        [SerializeField] private MaskMode maskMode = MaskMode.FullDarken;
        
        [Header("Mask Settings")]
        [SerializeField] private Transform targetToExclude;
        [SerializeField] private float maskRadius = 150f;
        [SerializeField] private float maskSoftness = 50f;
        [SerializeField] private bool followTarget = true;
        
        public enum MaskMode
        {
            FullDarken,    // Complete blackout except character (true Hades style)
            RadiusMode     // Customizable radius around character
        }
        
        private Image overlayImage;
        private Material overlayMaterial;
        private Camera mainCamera;
        private RectTransform rectTransform;
        
        // Shader property IDs for performance
        private static readonly int MaskCenterID = Shader.PropertyToID("_MaskCenter");
        private static readonly int MaskRadiusID = Shader.PropertyToID("_MaskRadius");
        private static readonly int MaskSoftnessID = Shader.PropertyToID("_MaskSoftness");
        private static readonly int FadeAlphaID = Shader.PropertyToID("_FadeAlpha");
        private static readonly int MaskModeID = Shader.PropertyToID("_MaskMode");
        private static readonly int CharacterBoundsID = Shader.PropertyToID("_CharacterBounds");
        
        void Awake()
        {
            overlayImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            mainCamera = Camera.main;
            
            // Create material instance for this overlay
            CreateOverlayMaterial();
        }
        
        void CreateOverlayMaterial()
        {
            // Create a simple masked fade shader material
            Shader fadeShader = Shader.Find("UI/MaskedFade");
            if (fadeShader == null)
            {
                // Fallback to default UI shader if custom shader not found
                fadeShader = Shader.Find("UI/Default");
            }
            
            overlayMaterial = new Material(fadeShader);
            overlayImage.material = overlayMaterial;
            
            // Set initial values
            overlayMaterial.SetFloat(MaskRadiusID, maskRadius);
            overlayMaterial.SetFloat(MaskSoftnessID, maskSoftness);
            overlayMaterial.SetFloat(FadeAlphaID, 0f);
            overlayMaterial.SetFloat(MaskModeID, (float)maskMode);
        }
        
        void Update()
        {
            if (followTarget && targetToExclude != null && overlayMaterial != null)
            {
                UpdateMaskPosition();
            }
        }
        
        void UpdateMaskPosition()
        {
            if (mainCamera == null || targetToExclude == null) return;
            
            // Convert world position to screen space
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetToExclude.position);
            
            // Convert to canvas coordinates
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPosition, null, out Vector2 localPosition);
            
            // Normalize to UV coordinates (0-1 range)
            Vector2 rectSize = rectTransform.rect.size;
            Vector2 normalizedPosition = new Vector2(
                (localPosition.x + rectSize.x * 0.5f) / rectSize.x,
                (localPosition.y + rectSize.y * 0.5f) / rectSize.y
            );
            
            // Update shader
            overlayMaterial.SetVector(MaskCenterID, normalizedPosition);
            
            // For FullDarken mode, we need character bounds
            if (maskMode == MaskMode.FullDarken)
            {
                UpdateCharacterBounds();
            }
        }
        
        void UpdateCharacterBounds()
        {
            if (targetToExclude == null || overlayMaterial == null) return;
            
            // Get character renderer bounds
            Renderer characterRenderer = targetToExclude.GetComponent<Renderer>();
            if (characterRenderer == null)
                characterRenderer = targetToExclude.GetComponentInChildren<Renderer>();
            
            if (characterRenderer != null)
            {
                Bounds bounds = characterRenderer.bounds;
                
                // Convert world bounds to screen space
                Vector3 minWorld = bounds.min;
                Vector3 maxWorld = bounds.max;
                
                Vector2 minScreen = mainCamera.WorldToScreenPoint(minWorld);
                Vector2 maxScreen = mainCamera.WorldToScreenPoint(maxWorld);
                
                // Convert to UV coordinates
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, minScreen, null, out Vector2 minLocal);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, maxScreen, null, out Vector2 maxLocal);
                
                Vector2 rectSize = rectTransform.rect.size;
                Vector4 normalizedBounds = new Vector4(
                    (minLocal.x + rectSize.x * 0.5f) / rectSize.x,
                    (minLocal.y + rectSize.y * 0.5f) / rectSize.y,
                    (maxLocal.x + rectSize.x * 0.5f) / rectSize.x,
                    (maxLocal.y + rectSize.y * 0.5f) / rectSize.y
                );
                
                overlayMaterial.SetVector(CharacterBoundsID, normalizedBounds);
            }
        }
        
        public void SetMaskMode(MaskMode mode)
        {
            maskMode = mode;
            if (overlayMaterial != null)
                overlayMaterial.SetFloat(MaskModeID, (float)mode);
        }
        
        public void SetTarget(Transform target)
        {
            targetToExclude = target;
        }
        
        public void SetMaskRadius(float radius)
        {
            maskRadius = radius;
            if (overlayMaterial != null)
                overlayMaterial.SetFloat(MaskRadiusID, radius);
        }
        
        public void SetMaskSoftness(float softness)
        {
            maskSoftness = softness;
            if (overlayMaterial != null)
                overlayMaterial.SetFloat(MaskSoftnessID, softness);
        }
        
        public void FadeIn(float duration, System.Action onComplete = null)
        {
            if (overlayMaterial != null)
            {
                overlayMaterial.DOFloat(1f, FadeAlphaID, duration)
                              .SetEase(Ease.InQuart)
                              .OnComplete(() => onComplete?.Invoke());
            }
        }
        
        public void FadeOut(float duration, System.Action onComplete = null)
        {
            if (overlayMaterial != null)
            {
                overlayMaterial.DOFloat(0f, FadeAlphaID, duration)
                              .SetEase(Ease.OutQuart)
                              .OnComplete(() => onComplete?.Invoke());
            }
        }
        
        public void SetAlpha(float alpha)
        {
            if (overlayMaterial != null)
                overlayMaterial.SetFloat(FadeAlphaID, alpha);
        }
        
        public float GetAlpha()
        {
            return overlayMaterial != null ? overlayMaterial.GetFloat(FadeAlphaID) : 0f;
        }
        
        void OnDestroy()
        {
            if (overlayMaterial != null)
            {
                DOTween.Kill(overlayMaterial);
                DestroyImmediate(overlayMaterial);
            }
        }
    }
}