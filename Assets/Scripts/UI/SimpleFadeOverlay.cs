using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CrimsonSanctum.UI
{
    /// <summary>
    /// Fallback version using multiple UI elements instead of custom shader
    /// Use this if the shader-based approach has issues
    /// </summary>
    public class SimpleFadeOverlay : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image[] fadeQuadrants;
        [SerializeField] private Transform targetToExclude;
        
        [Header("Settings")]
        [SerializeField] private float exclusionRadius = 150f;
        [SerializeField] private Color fadeColor = Color.black;
        
        private Camera mainCamera;
        private RectTransform canvasRect;
        private Sequence fadeSequence;
        
        void Awake()
        {
            mainCamera = Camera.main;
            canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            
            if (fadeQuadrants == null || fadeQuadrants.Length == 0)
            {
                CreateFadeQuadrants();
            }
            
            // Initially hide all quadrants
            foreach (var quad in fadeQuadrants)
            {
                quad.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0);
            }
        }
        
        void CreateFadeQuadrants()
        {
            // Create 4 quadrants that will cover the screen except for character area
            fadeQuadrants = new Image[4];
            
            for (int i = 0; i < 4; i++)
            {
                GameObject quad = new GameObject($"FadeQuadrant_{i}");
                quad.transform.SetParent(transform);
                
                RectTransform rect = quad.AddComponent<RectTransform>();
                Image img = quad.AddComponent<Image>();
                img.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0);
                
                fadeQuadrants[i] = img;
            }
        }
        
        public void SetTarget(Transform target)
        {
            targetToExclude = target;
        }
        
        public void FadeIn(float duration)
        {
            fadeSequence?.Kill();
            fadeSequence = DOTween.Sequence();
            
            foreach (var quad in fadeQuadrants)
            {
                fadeSequence.Join(quad.DOFade(1f, duration).SetEase(Ease.InQuart));
            }
        }
        
        public void FadeOut(float duration)
        {
            fadeSequence?.Kill();
            fadeSequence = DOTween.Sequence();
            
            foreach (var quad in fadeQuadrants)
            {
                fadeSequence.Join(quad.DOFade(0f, duration).SetEase(Ease.OutQuart));
            }
        }
        
        void Update()
        {
            if (targetToExclude != null)
            {
                UpdateQuadrantPositions();
            }
        }
        
        void UpdateQuadrantPositions()
        {
            if (mainCamera == null || targetToExclude == null) return;
            
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetToExclude.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out Vector2 localPos);
            
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 center = localPos;
            
            // Top quadrant
            if (fadeQuadrants[0] != null)
            {
                RectTransform rect = fadeQuadrants[0].rectTransform;
                rect.anchorMin = new Vector2(0, 0.5f);
                rect.anchorMax = new Vector2(1, 1);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = new Vector2(0, -(canvasSize.y * 0.5f - center.y - exclusionRadius));
            }
            
            // Bottom quadrant
            if (fadeQuadrants[1] != null)
            {
                RectTransform rect = fadeQuadrants[1].rectTransform;
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = new Vector2(0, -(canvasSize.y * 0.5f + center.y - exclusionRadius));
            }
            
            // Left quadrant
            if (fadeQuadrants[2] != null)
            {
                RectTransform rect = fadeQuadrants[2].rectTransform;
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0.5f, 1);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = new Vector2(-(canvasSize.x * 0.5f + center.x - exclusionRadius), 0);
            }
            
            // Right quadrant
            if (fadeQuadrants[3] != null)
            {
                RectTransform rect = fadeQuadrants[3].rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0);
                rect.anchorMax = new Vector2(1, 1);
                rect.offsetMin = new Vector2(canvasSize.x * 0.5f - center.x + exclusionRadius, 0);
                rect.offsetMax = Vector2.zero;
            }
        }
        
        void OnDestroy()
        {
            fadeSequence?.Kill();
        }
    }
}