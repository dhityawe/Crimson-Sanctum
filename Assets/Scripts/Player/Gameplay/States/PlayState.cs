using UnityEngine;
using Assets.Scripts.Systems;
using Assets.Scripts.Player;
using Unity.Cinemachine;
using System.Collections;
using Assets.Scripts.Core.Managers;
using UnityEngine.UI;
using DG.Tweening;
using CrimsonSanctum.UI;
using TMPro;

public class PlayingState : BaseState<GameManager>
{
    private bool isSubscribedToPlayerDeath = false;
    private bool isSubscribedToNewPlayerEvents = false;
    private Coroutine _tutorialCoroutine;
    
    // Tutorial effects variables (matching GameOverManager style)
    private GameObject _tutorialOverlayObject;
    private MaskedFadeOverlay _maskedOverlay;
    private Canvas _tutorialCanvas;
    
    // Camera control variables
    private Camera _mainCamera;
    private CinemachineCamera _cinemachineCamera;
    private float _originalCameraSize;
    private Vector3 _originalCameraPosition;
    private Transform _originalCameraParent;
    private bool _wasCinemachineEnabled;
    private bool _isCameraZoomed;
    
    // Tutorial settings (matching GameOverManager)
    private float _characterMaskRadius = 120f;
    private float _characterMaskSoftness = 80f;
    private float _zoomAmount = 0.7f;
    private float _zoomDuration = 1f;
    private float _fadeInDuration = 1f;
    private float _fadeOutDuration = 1f;
    
    public void EnterState(GameManager owner)
    {
        InitializeCharacter(owner);
        
        if(!PlayerPrefs.HasKey("Tutorial"))
        {
            
            _tutorialCoroutine = owner.StartCoroutine(StartTutorial(owner));
        }
    }
    
    public void UpdateState(GameManager owner)
    {
        // Playing state doesn't need input handling in Update
        // All game logic is handled by the spawned player and other systems
    }
    
    public void ExitState(GameManager owner)
    {
        // Unsubscribe from player death events (both old and new systems)
        if (isSubscribedToPlayerDeath)
        {
            PlayerHealth.OnDeath -= OnPlayerDeath;
            isSubscribedToPlayerDeath = false;
        }
        
        if (isSubscribedToNewPlayerEvents)
        {
            PlayerEvents.OnPlayerDeath -= OnPlayerDeathNew;
            isSubscribedToNewPlayerEvents = false;
        }
        
        // Cleanup tutorial effects if still active
        CleanupTutorialOverlay();
        ResetCamera();
    }

    private IEnumerator StartTutorial(GameManager game)
    {
        yield return new WaitForSeconds(5f);
        
        // Initialize camera references (like GameOverManager)
        InitializeCameraReferences();
        
        // Get player transform
        Transform playerTransform = game.CurrentPlayer != null ? game.CurrentPlayer.transform : null;
        
        // Create tutorial overlay (like GameOverManager)
        CreateTutorialOverlay(playerTransform);
        
        // Apply camera zoom (like GameOverManager)
        ApplyCameraZoom(playerTransform);
        
        // Fade in overlay with DOTween
        if (_maskedOverlay != null)
        {
            _maskedOverlay.SetAlpha(0f);
            DOTween.To(() => _maskedOverlay.GetAlpha(), 
                      x => _maskedOverlay.SetAlpha(x), 
                      1f, _fadeInDuration)
                   .SetEase(Ease.InQuart)
                   .SetUpdate(true); // Use unscaled time
        }
        
        yield return new WaitForSecondsRealtime(_fadeInDuration);

        Time.timeScale = 0;
        TMP_Text text = game.TutorialJumpText;
        text.SetText("Press space or tap screen to jump");
        text.gameObject.SetActive(true);
        text.rectTransform.DOAnchorPosY(150, 0.2f).SetUpdate(true);
        Debug.Log("Time is freeze, please press space or tap screen first to continue");
        
        while (!GameInput.Instance.IsJumpPressed())
        {
            yield return null;
        }
        
        Time.timeScale = 1;
        text.gameObject.SetActive(false);
        // Fade out overlay
        if (_maskedOverlay != null)
        {
            DOTween.To(() => _maskedOverlay.GetAlpha(), 
                      x => _maskedOverlay.SetAlpha(x), 
                      0f, _fadeOutDuration)
                   .SetEase(Ease.OutQuart);
        }
        
        // Reset camera (like GameOverManager)
        ResetCamera();
        
        yield return new WaitForSeconds(_fadeOutDuration);
        
        // Cleanup
        CleanupTutorialOverlay();
        
        Debug.Log("Great!");
        
        // Set tutorial complete
        PlayerPrefs.SetInt("Tutorial", 1);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Initialize camera references (matching GameOverManager)
    /// </summary>
    private void InitializeCameraReferences()
    {
        _mainCamera = Camera.main;
        
        if (_mainCamera == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (camObj != null)
                _mainCamera = camObj.GetComponent<Camera>();
        }
        
        if (_mainCamera != null)
        {
            _originalCameraSize = _mainCamera.orthographicSize;
            _originalCameraPosition = _mainCamera.transform.position;
            _originalCameraParent = _mainCamera.transform.parent;
        }
        
        _cinemachineCamera = Object.FindFirstObjectByType<CinemachineCamera>();
    }
    
    /// <summary>
    /// Create tutorial overlay with MaskedFadeOverlay (matching GameOverManager)
    /// </summary>
    private void CreateTutorialOverlay(Transform playerTransform)
    {
        // Create canvas if needed
        _tutorialCanvas = Object.FindFirstObjectByType<Canvas>();
        if (_tutorialCanvas == null)
        {
            GameObject canvasObj = new GameObject("TutorialCanvas");
            _tutorialCanvas = canvasObj.AddComponent<Canvas>();
            _tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _tutorialCanvas.sortingOrder = 999;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create overlay object
        _tutorialOverlayObject = new GameObject("TutorialMaskedOverlay");
        _tutorialOverlayObject.transform.SetParent(_tutorialCanvas.transform, false);
        
        // Setup RectTransform to fill screen
        RectTransform rectTransform = _tutorialOverlayObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Add Image component
        Image overlayImage = _tutorialOverlayObject.AddComponent<Image>();
        overlayImage.color = Color.black;
        
        // Add MaskedFadeOverlay component (like GameOverManager)
        _maskedOverlay = _tutorialOverlayObject.AddComponent<MaskedFadeOverlay>();
        _maskedOverlay.SetMaskRadius(_characterMaskRadius);
        _maskedOverlay.SetMaskSoftness(_characterMaskSoftness);
        _maskedOverlay.SetTarget(playerTransform);
        _maskedOverlay.SetAlpha(0f);
    }
    
    /// <summary>
    /// Apply camera zoom effect (matching GameOverManager)
    /// </summary>
    private void ApplyCameraZoom(Transform playerTransform)
    {
        if (_mainCamera == null) return;
        
        // Disable Cinemachine temporarily (like GameOverManager)
        if (_cinemachineCamera != null && _cinemachineCamera.enabled)
        {
            _wasCinemachineEnabled = true;
            _cinemachineCamera.enabled = false;
        }
        
        _isCameraZoomed = true;
        
        // Center camera on character (like GameOverManager)
        if (playerTransform != null)
        {
            _mainCamera.transform.SetParent(playerTransform);
            
            Vector3 currentLocalPos = _mainCamera.transform.localPosition;
            Vector3 targetLocalPos = new Vector3(0f, 0f, currentLocalPos.z);
            
            _mainCamera.transform.DOLocalMove(targetLocalPos, _zoomDuration)
                     .SetEase(Ease.InOutQuad)
                     .SetUpdate(true); // Use unscaled time
        }
        
        // Zoom camera (like GameOverManager)
        float targetSize = _originalCameraSize * _zoomAmount;
        _mainCamera.DOOrthoSize(targetSize, _zoomDuration)
                 .SetEase(Ease.InOutQuad)
                 .SetUpdate(true); // Use unscaled time
    }
    
    /// <summary>
    /// Reset camera to original state (matching GameOverManager)
    /// </summary>
    private void ResetCamera()
    {
        if (!_isCameraZoomed || _mainCamera == null) return;
        
        _isCameraZoomed = false;
        _mainCamera.DOKill();
        
        _mainCamera.transform.SetParent(_originalCameraParent);
        _mainCamera.orthographicSize = _originalCameraSize;
        _mainCamera.transform.position = _originalCameraPosition;
        
        if (_wasCinemachineEnabled && _cinemachineCamera != null)
        {
            _cinemachineCamera.enabled = true;
            _wasCinemachineEnabled = false;
        }
    }
    
    /// <summary>
    /// Cleanup tutorial overlay
    /// </summary>
    private void CleanupTutorialOverlay()
    {
        if (_maskedOverlay != null)
        {
            DOTween.Kill(_maskedOverlay);
        }
        
        if (_tutorialOverlayObject != null)
        {
            Object.Destroy(_tutorialOverlayObject);
            _tutorialOverlayObject = null;
            _maskedOverlay = null;
        }
    }
    
    private void OnPlayerDeath()
    {
        Debug.Log("PlayingState: Player death detected, looking for GameManager...");
        
        // Find GameManager and change to game over state
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log("PlayingState: GameManager found, ensuring it's initialized...");
            
            // Ensure GameManager is initialized (in case Start() hasn't been called yet)
            // gameManager.Initialize();
            
            Debug.Log("PlayingState: Calling ChangeToGameOverState...");
            gameManager.ChangeToGameOverState();
        }
        else
        {
            Debug.LogError("PlayingState: No GameManager found in scene!");
        }
    }

    
    /// <summary>
    /// Assign the cinemachine camera to follow the spawned player
    /// </summary>
    private void AssignCameraToPlayer(GameObject player)
    {
        // Find the cinemachine camera in the scene
        CinemachineCamera cinemachineCamera = Object.FindFirstObjectByType<CinemachineCamera>();

        if (cinemachineCamera != null)
        {
            // Set the player as the tracking target
            cinemachineCamera.Follow = player.transform;
            cinemachineCamera.ForceCameraPosition(
                cinemachineCamera.transform.position,
                cinemachineCamera.transform.rotation
            );
        }
        else
        {
        }
    }
    
    /// <summary>
    /// Enable player movement and interaction scripts
    /// </summary>
    private void EnablePlayerScripts(GameObject character)
    {
        // Enable all player components manually
        if (character.TryGetComponent<PlayerMove>(out var playerMove))
        {
            playerMove.enabled = true;
        }
        
        if (character.TryGetComponent<PlayerDash>(out var playerDash))
        {
            playerDash.enabled = true;
        }
        
        if (character.TryGetComponent<PlayerClimb>(out var playerClimb))
        {
            playerClimb.enabled = true;
        }
        
        if (character.TryGetComponent<PlayerController>(out var playerController))
        {
            playerController.enabled = true;
        }
        
        // Initialize the player system
        InitializePlayerSystem(character);
    }

    /// <summary>
    /// Initialize the new player system components
    /// </summary>
    private void InitializePlayerSystem(GameObject character)
    {
        // Ensure all required components exist
        EnsureRequiredComponents(character);

        // Initialize player state - Change from Preview to Idle for gameplay
        if (character.TryGetComponent<PlayerStateManager>(out var stateManager))
        {
            // Player starts in Preview state, change to Idle when game starts
            if (stateManager.CurrentState == PlayerState.Preview)
            {
                stateManager.ChangeState(PlayerState.Idle);
            }
            else
            {
                // Fallback: Set to Idle if not in Preview state
                stateManager.ChangeState(PlayerState.Idle);
            }
        }


        // jalanin ke cinemachine follow set position damping y to 2

        CinemachineFollow followCam = Object.FindFirstObjectByType<CinemachineFollow>();
        if (followCam != null)
        {
            followCam.TrackerSettings.PositionDamping = new Vector3(1, 2, 1);
        }
    }

    /// <summary>
    /// Ensure all required components for the new architecture exist
    /// </summary>
    private void EnsureRequiredComponents(GameObject character)
    {
        // Add PlayerStateManager if missing
        if (!character.TryGetComponent<PlayerStateManager>(out _))
        {
            character.AddComponent<PlayerStateManager>();
        }

        // Add PlayerCollisionHandler if missing
        if (!character.TryGetComponent<PlayerCollisionHandler>(out _))
        {
            character.AddComponent<PlayerCollisionHandler>();
        }

        // Add PlayerHealth if missing
        if (!character.TryGetComponent<PlayerHealth>(out _))
        {
            character.AddComponent<PlayerHealth>();
        }
        
    }

    /// <summary>
    /// Handle player death using new event system
    /// </summary>
    private void OnPlayerDeathNew()
    {
        // Find GameManager and change to game over state
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            Debug.Log("GameManager found (new event system), changing to GameOver state...");
            gameManager.ChangeToGameOverState();
        }
        else
        {
            Debug.LogError("GameManager not found (new event system)! Make sure GameManager exists in the scene.");
        }
    }

    private void InitializeCharacter(GameManager owner)
    {
        CharacterData selectedCharacter = owner.GetCharacterData(owner.SelectedCharacterIndex);

        if (selectedCharacter != null)
        {
            // Check if character is already spawned (from preview)
            GameObject spawnedPlayer = owner.CurrentPlayer;

            if (spawnedPlayer == null)
            {
                // Spawn character if not already spawned
                spawnedPlayer = owner.SpawnCharacter(selectedCharacter);
            }
            else
            {
                // Re-enable player scripts if character already exists
                EnablePlayerScripts(spawnedPlayer);
            }

            if (spawnedPlayer != null)
            {

                // Assign cinemachine camera to follow the player
                AssignCameraToPlayer(spawnedPlayer);

                // Subscribe to player death events (both old and new systems for compatibility)
                PlayerHealth.OnDeath += OnPlayerDeath;
                PlayerEvents.OnPlayerDeath += OnPlayerDeathNew;
                isSubscribedToPlayerDeath = true;
                isSubscribedToNewPlayerEvents = true;
            }
            else
            {
            }
        }
    }
}
