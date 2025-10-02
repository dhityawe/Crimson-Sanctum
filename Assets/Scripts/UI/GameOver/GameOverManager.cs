using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Unity.Cinemachine;

namespace CrimsonSanctum.UI
{
    public class GameOverManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas gameOverCanvas;
        [SerializeField] private MaskedFadeOverlay maskedOverlay;
        [SerializeField] private TextMeshProUGUI gameOverTitle;
        [SerializeField] private GameObject restartPrompt;
        
        [Header("Animation Timing")]
        [SerializeField] private float fadeInDuration = 2.5f;
        [SerializeField] private float titleAppearDelay = 1.5f;
        [SerializeField] private float titleFadeDuration = 1f;
        [SerializeField] private float promptAppearDelay = 2.5f;
        [SerializeField] private float promptFadeDuration = 0.8f;
        
        [Header("Character Masking")]
        [SerializeField] private float characterMaskRadius = 120f;
        [SerializeField] private float characterMaskSoftness = 80f;
        
        [Header("Camera Zoom Effect")]
        [SerializeField] private bool enableCameraZoom = true;
        [SerializeField] private float zoomAmount = 0.7f; // 0.7 = zoom in (70% of original size), 1.0 = no zoom
        [SerializeField] private float zoomDuration = 2f;
        [SerializeField] private bool centerOnCharacter = true;
        [SerializeField] private Vector3 centerOffset = Vector3.zero;
        [SerializeField] private float centerDuration = 1.5f;
        [SerializeField] private Ease zoomEase = Ease.InOutQuad;
        
        private Camera mainCamera;
        private CinemachineCamera cinemachineCamera;
        private float originalCameraSize;
        private Vector3 originalCameraPosition;
        private Transform originalCameraParent;
        private bool wasCinemachineEnabled = false;
        private bool isCameraZoomed = false;
        
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
        
        void OnEnable()
        {
            // Subscribe to scene loaded event to reinitialize camera after scene reload
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        void OnDisable()
        {
            // Unsubscribe from scene loaded event
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Reinitialize camera references after scene reload
            if (mode == UnityEngine.SceneManagement.LoadSceneMode.Single)
            {
                Debug.Log("GameOverManager: Scene reloaded, reinitializing camera references");
                ReinitializeCameraReferences();
            }
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
            
            ReinitializeCameraReferences();
        }
        
        void ReinitializeCameraReferences()
        {
            // Reset camera state flags
            isCameraZoomed = false;
            wasCinemachineEnabled = false;
            
            // Cache camera references (they might be new after scene reload)
            mainCamera = Camera.main;
            cinemachineCamera = Object.FindFirstObjectByType<CinemachineCamera>();
            
            if (mainCamera != null)
            {
                // Make sure camera is unparented and at its natural state
                if (originalCameraParent != null)
                {
                    mainCamera.transform.SetParent(originalCameraParent);
                }
                
                // Store fresh original values
                originalCameraSize = mainCamera.orthographicSize;
                originalCameraPosition = mainCamera.transform.position;
                originalCameraParent = mainCamera.transform.parent;
                
                Debug.Log($"GameOverManager: Camera reinitialized - Size: {originalCameraSize}, Pos: {originalCameraPosition}, Parent: {originalCameraParent?.name ?? "null"}");
            }
            
            if (cinemachineCamera != null)
            {
                // Make sure Cinemachine is enabled
                if (!cinemachineCamera.enabled)
                {
                    cinemachineCamera.enabled = true;
                }
                Debug.Log("GameOverManager: Cinemachine detected - will temporarily disable during game over");
            }
            else
            {
                Debug.Log("GameOverManager: Using regular camera control");
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
            
            // Apply camera zoom and centering effect
            if (enableCameraZoom && mainCamera != null)
            {
                ApplyCameraZoom();
            }
            
            // Create animation sequence
            CreateGameOverSequence();
        }
        
        void ApplyCameraZoom()
        {
            if (mainCamera == null)
            {
                Debug.LogWarning("GameOverManager: No main camera found for zoom effect.");
                return;
            }
            
            // Temporarily disable Cinemachine if it exists
            if (cinemachineCamera != null && cinemachineCamera.enabled)
            {
                wasCinemachineEnabled = true;
                cinemachineCamera.enabled = false;
                Debug.Log("GameOverManager: Temporarily disabled Cinemachine for game over effect");
            }
            
            // Now we have full control over the camera
            isCameraZoomed = true;
            
            // Center camera on character if enabled
            if (centerOnCharacter && playerTransform != null)
            {
                // Make camera a child of player to inherit position
                mainCamera.transform.SetParent(playerTransform);
                
                // Get current local position to smoothly transition from
                Vector3 currentLocalPos = mainCamera.transform.localPosition;
                
                // Calculate target local position (centered on player with offset)
                Vector3 targetLocalPos = new Vector3(
                    centerOffset.x,
                    centerOffset.y,
                    currentLocalPos.z // Keep original Z distance
                );
                
                // Smoothly move camera to center in local space (relative to player)
                mainCamera.transform.DOLocalMove(targetLocalPos, centerDuration)
                         .SetEase(zoomEase)
                         .SetUpdate(true);
                         
                Debug.Log($"GameOverManager: Camera parented to {playerTransform.name}, smoothly centering from local {currentLocalPos} to {targetLocalPos}");
            }
            
            // Zoom in the camera (happens simultaneously with centering)
            float targetSize = originalCameraSize * zoomAmount;
            mainCamera.DOOrthoSize(targetSize, zoomDuration)
                     .SetEase(zoomEase)
                     .SetUpdate(true);
        }
        

        
        void ResetCamera()
        {
            if (!isCameraZoomed) return;
            
            isCameraZoomed = false;
            
            if (mainCamera != null)
            {
                // Kill ongoing animations
                mainCamera.DOKill();
                
                // Unparent camera before resetting (important!)
                mainCamera.transform.SetParent(originalCameraParent);
                
                // Reset camera to original state
                mainCamera.orthographicSize = originalCameraSize;
                mainCamera.transform.position = originalCameraPosition;
            }
            
            // Re-enable Cinemachine if it was disabled
            if (wasCinemachineEnabled && cinemachineCamera != null)
            {
                cinemachineCamera.enabled = true;
                wasCinemachineEnabled = false;
                Debug.Log("GameOverManager: Re-enabled Cinemachine");
            }
        }
        
        void SetupInitialStates()
        {
            // Set masked overlay to transparent
            if (maskedOverlay != null)
                maskedOverlay.SetAlpha(0f);
            
            // Hide text elements
            if (gameOverTitle != null)
                gameOverTitle.alpha = 0f;
            
            // Hide restart prompt GameObject
            if (restartPrompt != null)
                restartPrompt.SetActive(false);
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
            
            // Show restart prompt GameObject (ButtonSelector will handle fade animation)
            if (restartPrompt != null)
            {
                gameOverSequence.InsertCallback(promptAppearDelay, () => {
                    if (restartPrompt != null)
                    {
                        restartPrompt.SetActive(true);
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
            
            // Reset camera
            ResetCamera();
            
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
            
            // Make sure to re-enable Cinemachine if it was disabled
            if (wasCinemachineEnabled && cinemachineCamera != null)
            {
                cinemachineCamera.enabled = true;
            }
            
            // Reset camera if still zoomed
            if (mainCamera != null)
            {
                mainCamera.DOKill();
            }
        }
    }
}
