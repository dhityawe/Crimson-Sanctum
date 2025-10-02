using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.Systems;
using CrimsonSanctum.UI;
using Object = UnityEngine.Object;

public class GameOverState : BaseState<GameManager>
{
    private bool gameOverUITriggered = false;
    
    public void EnterState(GameManager owner)
    {
        Debug.Log("Game Over - Triggering Visual Effects");
        
        // Trigger the Hades-style game over effect
        TriggerGameOverVisuals(owner);
        
        Debug.Log("Press R to restart the game");
    }
    
    public void UpdateState(GameManager owner)
    {
        // Check for restart input
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }
    }
    
    public void ExitState(GameManager owner)
    {
        // Hide game over UI when exiting state
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.HideGameOver();
        }
        
        gameOverUITriggered = false;
    }
    
    private void TriggerGameOverVisuals(GameManager owner)
    {
        if (gameOverUITriggered) return;
        
        gameOverUITriggered = true;
        
        // Find the current player for masking
        GameObject currentPlayer = owner.CurrentPlayer;
        Debug.Log($"<color=yellow>[GameOverState] TriggerGameOverVisuals - CurrentPlayer from GameManager: {(currentPlayer != null ? currentPlayer.name : "<color=red>NULL</color>")}</color>");
        
        Transform playerTransform = currentPlayer != null ? currentPlayer.transform : null;
        
        // If no player from GameManager, try to find by tag
        if (playerTransform == null)
        {
            GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
            if (foundPlayer != null)
            {
                playerTransform = foundPlayer.transform;
                Debug.Log($"<color=orange>[GameOverState] Player not in GameManager, found by tag: {foundPlayer.name}</color>");
            }
            else
            {
                Debug.LogWarning("<color=red>[GameOverState] Could not find player transform anywhere!</color>");
            }
        }
        
        // Try to use existing GameOverManager
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.TriggerGameOver(playerTransform);
        }
        else
        {
            // No GameOverManager found - user needs to set it up manually
            Debug.LogWarning("No GameOverManager found in scene! Please add a GameOverManager with proper UI setup for the game over effect.");
            Debug.Log("GAME OVER - Press R to restart (fallback message)");
        }
    }
    
    private void RestartScene()
    {
        // Hide game over effects before restarting
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.HideGameOver();
        }
        
        // Get current scene name and reload it
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}
