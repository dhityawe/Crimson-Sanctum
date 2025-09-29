using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.Systems;

public class GameOverState : BaseState<GameManager>
{
    public void EnterState(GameManager owner)
    {
        Debug.Log("Game Over");
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
        // Clean up if needed
    }
    
    private void RestartScene()
    {
        // Get current scene name and reload it
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
}
