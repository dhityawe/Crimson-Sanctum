using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace CrimsonSanctum.UI
{
    public class GameOverManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas gameOverCanvas;
        [SerializeField] private MaskedFadeOverlay maskedOverlay;
        [SerializeField] private TextMeshProUGUI gameOverTitle;
        [SerializeField] private TextMeshProUGUI restartPrompt;
        
        [Header("Animation Timing")]
        [SerializeField] private float fadeInDuration = 2.5f;
        [SerializeField] private float titleAppearDelay = 1.5f;
        [SerializeField] private float titleFadeDuration = 1f;
        [SerializeField] private float promptAppearDelay = 2.5f;
        [SerializeField] private float promptFadeDuration = 0.8f;
        
        [Header("Character Masking")]
        [SerializeField] private float characterMaskRadius = 120f;
        [SerializeField] private float characterMaskSoftness = 80f;
        
        private Transform playerTransform;
        private Sequence gameOverSequence;
        private bool isActive = false;
        
        public static GameOverManager Instance { get; private set; }
        
        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeComponents();
        }
        
        void InitializeComponents()
        {
            // Hide UI initially
            if (gameOverCanvas != null)
                gameOverCanvas.gameObject.SetActive(false);
            
            // Setup masked overlay if assigned
            if (maskedOverlay != null)
            {
                maskedOverlay.SetMaskRadius(characterMaskRadius);
                maskedOverlay.SetMaskSoftness(characterMaskSoftness);
            }
        }
        
        public void TriggerGameOver(Transform playerTransform = null)
        {
            if (isActive) return;
            
            isActive = true;
            this.playerTransform = playerTransform;
            
            // Find player if not provided
            if (this.playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    this.playerTransform = player.transform;
            }
            
            StartGameOverSequence();
        }
        
        void StartGameOverSequence()
        {
            // Require manual setup - no automatic UI creation for performance
            if (gameOverCanvas == null)
            {
                Debug.LogError("GameOverManager: No gameOverCanvas assigned! Please set up the UI manually in the inspector.");
                return;
            }
            
            // Activate canvas
            gameOverCanvas.gameObject.SetActive(true);
            
            // Kill any existing sequence
            gameOverSequence?.Kill();
            
            // Setup initial states
            SetupInitialStates();
            
            // Configure masked overlay
            if (maskedOverlay != null && playerTransform != null)
            {
                maskedOverlay.SetTarget(playerTransform);
            }
            
            // Create animation sequence
            CreateGameOverSequence();
        }
        
        void SetupInitialStates()
        {
            // Set masked overlay to transparent
            if (maskedOverlay != null)
                maskedOverlay.SetAlpha(0f);
            
            // Hide text elements
            if (gameOverTitle != null)
                gameOverTitle.alpha = 0f;
            
            if (restartPrompt != null)
                restartPrompt.alpha = 0f;
        }
        
        void CreateGameOverSequence()
        {
            gameOverSequence = DOTween.Sequence();
            
            // Fade in masked overlay if available
            if (maskedOverlay != null)
            {
                gameOverSequence.Append(
                    DOTween.To(() => maskedOverlay.GetAlpha(), 
                              x => maskedOverlay.SetAlpha(x), 
                              1f, fadeInDuration)
                           .SetEase(Ease.InQuart)
                );
            }
            else
            {
                // Simple delay if no overlay available
                gameOverSequence.AppendInterval(fadeInDuration);
            }
            
            // Show "GAME OVER" title
            if (gameOverTitle != null)
            {
                gameOverSequence.Insert(titleAppearDelay,
                    gameOverTitle.DOFade(1f, titleFadeDuration)
                                 .SetEase(Ease.OutQuart)
                );
                
                // Add slight scale animation to title
                Vector3 originalScale = gameOverTitle.transform.localScale;
                gameOverTitle.transform.localScale = originalScale * 0.8f;
                gameOverSequence.Insert(titleAppearDelay,
                    gameOverTitle.transform.DOScale(originalScale, titleFadeDuration)
                                          .SetEase(Ease.OutBack)
                );
            }
            
            // Show restart prompt
            if (restartPrompt != null)
            {
                gameOverSequence.Insert(promptAppearDelay,
                    restartPrompt.DOFade(0.8f, promptFadeDuration)
                                 .SetEase(Ease.OutQuart)
                );
                
                // Add breathing effect to restart prompt
                gameOverSequence.AppendCallback(() => {
                    if (restartPrompt != null)
                    {
                        restartPrompt.DOFade(0.3f, 1.2f)
                                    .SetLoops(-1, LoopType.Yoyo)
                                    .SetEase(Ease.InOutSine);
                    }
                });
            }
        }
        
        public void HideGameOver()
        {
            if (!isActive) return;
            
            isActive = false;
            
            // Kill animations
            gameOverSequence?.Kill();
            
            // Hide canvas
            if (gameOverCanvas != null)
                gameOverCanvas.gameObject.SetActive(false);
            
            // Reset overlay
            if (maskedOverlay != null)
                maskedOverlay.SetAlpha(0f);
        }
        
        public bool IsGameOverActive()
        {
            return isActive;
        }
        
        void OnDestroy()
        {
            gameOverSequence?.Kill();
        }
    }
}
