using UnityEngine;
using Assets.Scripts.Systems;
using Assets.Scripts.Player;
using Unity.Cinemachine;

public class PlayingState : BaseState<GameManager>
{
    private bool isSubscribedToPlayerDeath = false;
    private bool isSubscribedToNewPlayerEvents = false;
    
    public void EnterState(GameManager owner)
    {
        // Get selected character data
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
        else
        {
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
}
