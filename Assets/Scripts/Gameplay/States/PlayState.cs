using UnityEngine;
using Assets.Scripts.Systems;
using Assets.Scripts.Player;
using Unity.Cinemachine;

public class PlayingState : BaseState<GameManager>
{
    private bool isSubscribedToPlayerDeath = false;
    
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
                Debug.Log($"Playing as {selectedCharacter.Name}");
                
                // Assign cinemachine camera to follow the player
                AssignCameraToPlayer(spawnedPlayer);
                
                // Subscribe to player death event
                PlayerMove.OnDeath += OnPlayerDeath;
                isSubscribedToPlayerDeath = true;
            }
            else
            {
                Debug.LogError("Failed to spawn character!");
            }
        }
        else
        {
            Debug.LogError("Selected character data is null!");
        }
    }
    
    public void UpdateState(GameManager owner)
    {
        // Playing state doesn't need input handling in Update
        // All game logic is handled by the spawned player and other systems
    }
    
    public void ExitState(GameManager owner)
    {
        // Unsubscribe from player death event
        if (isSubscribedToPlayerDeath)
        {
            PlayerMove.OnDeath -= OnPlayerDeath;
            isSubscribedToPlayerDeath = false;
        }
    }
    
    private void OnPlayerDeath()
    {
        // Find GameManager and change to game over state
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ChangeToGameOverState();
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
            Debug.Log($"Cinemachine camera now following player: {player.name}");
        }
        else
        {
            Debug.LogWarning("CinemachineCamera not found in scene!");
        }
    }
    
    /// <summary>
    /// Enable player movement and interaction scripts
    /// </summary>
    private void EnablePlayerScripts(GameObject character)
    {
        // Enable PlayerMove script
        PlayerMove playerMove = character.GetComponent<PlayerMove>();
        if (playerMove != null)
        {
            playerMove.enabled = true;
        }
        
        // Enable any other player-specific scripts
        // Add more scripts here as needed
    }
}
