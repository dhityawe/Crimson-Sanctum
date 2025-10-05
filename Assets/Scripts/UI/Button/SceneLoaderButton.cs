using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CrimsonSanctum.UI
{
    /// <summary>
    /// Helper component to easily assign scene loading functionality to buttons.
    /// Works seamlessly with ButtonSelector and GameSceneManager.
    /// Attach this to individual buttons or use methods directly in Button.onClick.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SceneLoaderButton : MonoBehaviour
    {
        public enum SceneLoadAction
        {
            LoadByName,         // Load specific scene by name
            LoadNext,           // Load next scene
            LoadPrevious,       // Load previous scene
            LoadMainMenu,       // Load main menu
            LoadGameplay,       // Load gameplay scene
            ReloadCurrent,      // Reload current scene
            QuitGame            // Quit application
        }
        
        [Header("Scene Loading Settings")]
        [SerializeField] private SceneLoadAction loadAction = SceneLoadAction.LoadByName;
        [SerializeField, Scene] private string _gameplayScene, _mainMenuScene;
        
        [Header("Options")]
        [SerializeField] private bool loadOnClick = true;
        [SerializeField] private bool hideGameOverOnLoad = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;


        /// <summary>
        /// Quit the application
        /// </summary>
        private void QuitGame()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[SceneLoaderButton] Quitting game...");
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        #region Public Helper Methods for Direct Button Assignment
        
        /// <summary>
        /// Load a specific scene by name (can be assigned directly to Button.onClick)
        /// </summary>
        public void LoadSceneByName(string name)
        {
            if (hideGameOverOnLoad && GameOverManager.Instance != null)
            {
                GameOverManager.Instance.HideGameOver();
            }
            GameSceneManager.Instance.LoadingScene(name);
        }

        public void LoadMainMenu()
        {
            GameSceneManager.Instance.LoadingScene(_mainMenuScene);
        }

        public void LoadGameplay() => GameSceneManager.Instance.LoadingScene(_gameplayScene);
        
        /// <summary>
        /// Reload current scene (can be assigned directly to Button.onClick)
        /// </summary>
        public void ReloadScene()
        {
            if (hideGameOverOnLoad && GameOverManager.Instance != null)
            {
                GameOverManager.Instance.HideGameOver();
            }
            GameSceneManager.Instance.Reload();
        }
        
        /// <summary>
        /// Quit game (can be assigned directly to Button.onClick)
        /// </summary>
        public void Quit()
        {
            QuitGame();
        }

        public void LoadScene(string sceneName)
        {
            GameSceneManager.Instance.LoadingScene(sceneName);
        }
        #endregion
    }
}
