using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Unity.Cinemachine;

namespace CrimsonSanctum.UI
{
    /// <summary>
    /// Manages the game over screen with Hades-style visual effects.
    /// Handles camera zoom, character masking, and UI animations.
    /// </summary>
    public class GameOverManager : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("UI References")]
        [SerializeField] private Canvas gameOverCanvas;
        [SerializeField] private MaskedFadeOverlay maskedOverlay;
        [SerializeField] private TextMeshProUGUI gameOverTitle;
        [SerializeField] private GameObject restartPrompt;
        
        [Header("Camera Reference")]
        [SerializeField] private Camera cameraReference;
        
        [Header("Animation Timing")]
        [SerializeField] private float fadeInDuration = 2.5f;
        [SerializeField] private float titleAppearDelay = 1.5f;
        [SerializeField] private float titleFadeDuration = 1f;
        [SerializeField] private float promptAppearDelay = 2.5f;
        
        [Header("Character Masking")]
        [SerializeField] private float characterMaskRadius = 120f;
        [SerializeField] private float characterMaskSoftness = 80f;
        
        [Header("Camera Zoom Effect")]
        [SerializeField] private bool enableCameraZoom = true;
        [SerializeField] private float zoomAmount = 0.7f;
        [SerializeField] private float zoomDuration = 2f;
        [SerializeField] private bool centerOnCharacter = true;
        [SerializeField] private Vector3 centerOffset = Vector3.zero;
        [SerializeField] private float centerDuration = 1.5f;
        [SerializeField] private Ease zoomEase = Ease.InOutQuad;
        
        #endregion
        
        #region Private Fields
        
        private Camera mainCamera;
        private CinemachineCamera cinemachineCamera;
        private float originalCameraSize;
        private Vector3 originalCameraPosition;
        private Transform originalCameraParent;
        private bool wasCinemachineEnabled;
        private bool isCameraZoomed;
        
        private Transform playerTransform;
        private Sequence gameOverSequence;
        private bool isActive;
        
        #endregion
        
        #region Singleton
        
        public static GameOverManager Instance { get; private set; }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Instance = this;
            }
            
            InitializeComponents();
        }
        
        #endregion
        
        #region Initialization
        
        void InitializeComponents()
        {
            if (gameOverCanvas != null)
                gameOverCanvas.gameObject.SetActive(false);
            
            if (maskedOverlay != null)
            {
                maskedOverlay.SetMaskRadius(characterMaskRadius);
                maskedOverlay.SetMaskSoftness(characterMaskSoftness);
            }
            
            InitializeCameraReferences();
        }
        
        void InitializeCameraReferences()
        {
            mainCamera = cameraReference != null ? cameraReference : Camera.main;
            
            if (mainCamera == null)
            {
                GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (camObj != null)
                    mainCamera = camObj.GetComponent<Camera>();
            }
            
            if (mainCamera != null)
            {
                originalCameraSize = mainCamera.orthographicSize;
                originalCameraPosition = mainCamera.transform.position;
                originalCameraParent = mainCamera.transform.parent;
            }
            
            cinemachineCamera = Object.FindFirstObjectByType<CinemachineCamera>();
        }
        
        #endregion
        
        #region Public Methods
        
        public void TriggerGameOver(Transform playerTransform = null)
        {
            if (isActive) return;
            
            isActive = true;
            this.playerTransform = playerTransform;
            
            if (this.playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    this.playerTransform = player.transform;
            }
            
            StartGameOverSequence();
        }
        
        public void HideGameOver()
        {
            if (!isActive) return;
            
            isActive = false;
            gameOverSequence?.Kill();
            ResetCamera();
            
            if (gameOverCanvas != null)
                gameOverCanvas.gameObject.SetActive(false);
            
            if (maskedOverlay != null)
                maskedOverlay.SetAlpha(0f);
            
            playerTransform = null;
        }
        
        public bool IsGameOverActive() => isActive;
        
        #endregion
        
        #region Game Over Sequence
        
        void StartGameOverSequence()
        {
            if (gameOverCanvas == null) return;
            
            gameOverCanvas.gameObject.SetActive(true);
            gameOverSequence?.Kill();
            
            SetupInitialStates();
            
            if (maskedOverlay != null && playerTransform != null)
                maskedOverlay.SetTarget(playerTransform);
            
            if (enableCameraZoom && mainCamera != null)
                ApplyCameraZoom();
            
            CreateGameOverSequence();
        }
        
        void SetupInitialStates()
        {
            if (maskedOverlay != null)
                maskedOverlay.SetAlpha(0f);
            
            if (gameOverTitle != null)
                gameOverTitle.alpha = 0f;
            
            if (restartPrompt != null)
                restartPrompt.SetActive(false);
        }
        
        void CreateGameOverSequence()
        {
            gameOverSequence = DOTween.Sequence();
            
            // Fade in overlay
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
                gameOverSequence.AppendInterval(fadeInDuration);
            }
            
            // Show title with scale animation
            if (gameOverTitle != null)
            {
                Vector3 originalScale = gameOverTitle.transform.localScale;
                gameOverTitle.transform.localScale = originalScale * 0.8f;
                
                gameOverSequence.Insert(titleAppearDelay,
                    gameOverTitle.DOFade(1f, titleFadeDuration).SetEase(Ease.OutQuart)
                );
                
                gameOverSequence.Insert(titleAppearDelay,
                    gameOverTitle.transform.DOScale(originalScale, titleFadeDuration).SetEase(Ease.OutBack)
                );
            }
            
            // Show restart prompt
            if (restartPrompt != null)
            {
                gameOverSequence.InsertCallback(promptAppearDelay, () => {
                    if (restartPrompt != null)
                        restartPrompt.SetActive(true);
                });
            }
        }
        
        #endregion
        
        #region Camera Control
        
        void ApplyCameraZoom()
        {
            if (mainCamera == null) return;
            
            // Disable Cinemachine temporarily
            if (cinemachineCamera != null && cinemachineCamera.enabled)
            {
                wasCinemachineEnabled = true;
                cinemachineCamera.enabled = false;
            }
            
            isCameraZoomed = true;
            
            // Center camera on character
            if (centerOnCharacter && playerTransform != null)
            {
                mainCamera.transform.SetParent(playerTransform);
                
                Vector3 currentLocalPos = mainCamera.transform.localPosition;
                Vector3 targetLocalPos = new Vector3(
                    centerOffset.x,
                    centerOffset.y,
                    currentLocalPos.z
                );
                
                mainCamera.transform.DOLocalMove(targetLocalPos, centerDuration)
                         .SetEase(zoomEase)
                         .SetUpdate(true);
            }
            
            // Zoom camera
            float targetSize = originalCameraSize * zoomAmount;
            mainCamera.DOOrthoSize(targetSize, zoomDuration)
                     .SetEase(zoomEase)
                     .SetUpdate(true);
        }
        
        void ResetCamera()
        {
            if (!isCameraZoomed || mainCamera == null) return;
            
            isCameraZoomed = false;
            mainCamera.DOKill();
            
            mainCamera.transform.SetParent(originalCameraParent);
            mainCamera.orthographicSize = originalCameraSize;
            mainCamera.transform.position = originalCameraPosition;
            
            if (wasCinemachineEnabled && cinemachineCamera != null)
            {
                cinemachineCamera.enabled = true;
                wasCinemachineEnabled = false;
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        void OnDestroy()
        {
            gameOverSequence?.Kill();
            
            if (wasCinemachineEnabled && cinemachineCamera != null)
                cinemachineCamera.enabled = true;
            
            if (mainCamera != null)
                mainCamera.DOKill();
        }
        
        #endregion
    }
}
