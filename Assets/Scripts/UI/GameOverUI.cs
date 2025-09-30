using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace CrimsonSanctum.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Canvas gameOverCanvas;
        [SerializeField] private Image blackOverlay;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private TextMeshProUGUI restartText;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 2f;
        [SerializeField] private float textAppearDelay = 1f;
        [SerializeField] private float textFadeDuration = 1f;
        
        [Header("Mask Settings")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float maskRadius = 100f;
        
        private RectTransform canvasRectTransform;
        private Camera mainCamera;
        private Sequence gameOverSequence;
        
        void Awake()
        {
            if (gameOverCanvas == null)
                gameOverCanvas = GetComponent<Canvas>();
                
            canvasRectTransform = gameOverCanvas.GetComponent<RectTransform>();
            mainCamera = Camera.main;
            
            // Initially hide the game over UI
            gameOverCanvas.gameObject.SetActive(false);
        }
        
        public void ShowGameOverEffect()
        {
            gameOverCanvas.gameObject.SetActive(true);
            
            // Kill any existing sequence
            gameOverSequence?.Kill();
            
            // Find the player if not assigned
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }
            
            StartGameOverSequence();
        }
        
        private void StartGameOverSequence()
        {
            gameOverSequence = DOTween.Sequence();
            
            // Set initial states
            blackOverlay.color = new Color(0, 0, 0, 0);
            gameOverText.alpha = 0;
            restartText.alpha = 0;
            
            // Create the fade sequence
            gameOverSequence.Append(blackOverlay.DOFade(1f, fadeInDuration).SetEase(Ease.InQuart))
                           .AppendInterval(textAppearDelay)
                           .Append(gameOverText.DOFade(1f, textFadeDuration).SetEase(Ease.OutQuart))
                           .Append(restartText.DOFade(0.7f, textFadeDuration * 0.5f).SetEase(Ease.OutQuart));
                           
            // Add breathing effect to restart text
            gameOverSequence.AppendCallback(() => {
                restartText.DOFade(0.3f, 1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            });
        }
        
        public void HideGameOverEffect()
        {
            gameOverSequence?.Kill();
            gameOverCanvas.gameObject.SetActive(false);
        }
        
        // Method to update mask position if player is moving during death animation
        public void UpdateMaskPosition(Vector3 worldPosition)
        {
            if (mainCamera != null && canvasRectTransform != null)
            {
                Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform, screenPosition, null, out Vector2 localPosition);
                
                // Update mask position (this will be used with the shader mask)
                // For now, we'll store this position for use with the mask shader
            }
        }
        
        void OnDestroy()
        {
            gameOverSequence?.Kill();
        }
    }
}