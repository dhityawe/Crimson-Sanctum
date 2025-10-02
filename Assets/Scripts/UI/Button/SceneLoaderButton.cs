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
        [SerializeField] private string sceneName = "";
        
        [Header("Options")]
        [SerializeField] private bool loadOnClick = true;
        [SerializeField] private bool hideGameOverOnLoad = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        private Button button;
        
        void Awake()
        {
            button = GetComponent<Button>();
            
            if (loadOnClick)
            {
                button.onClick.AddListener(OnButtonClick);
            }
        }
        
        void OnButtonClick()
        {
            ExecuteLoadAction();
        }
        
        /// <summary>
        /// Execute the configured scene load action
        /// </summary>
        public void ExecuteLoadAction()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[SceneLoaderButton] Executing action: {loadAction}");
            }
            
            // Hide game over screen if enabled
            if (hideGameOverOnLoad && GameOverManager.Instance != null)
            {
                GameOverManager.Instance.HideGameOver();
            }
            
            // Execute the appropriate action
            switch (loadAction)
            {
                case SceneLoadAction.LoadByName:
                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        GameSceneManager.Load(sceneName);
                    }
                    else
                    {
                        Debug.LogWarning("[SceneLoaderButton] Scene name is empty! Cannot load scene.");
                    }
                    break;
                    
                case SceneLoadAction.LoadNext:
                    GameSceneManager.LoadNext();
                    break;
                    
                case SceneLoadAction.LoadPrevious:
                    GameSceneManager.LoadPrevious();
                    break;
                    
                case SceneLoadAction.LoadMainMenu:
                    GameSceneManager.GoToMainMenu();
                    break;
                    
                case SceneLoadAction.LoadGameplay:
                    GameSceneManager.StartGameplay();
                    break;
                    
                case SceneLoadAction.ReloadCurrent:
                    GameSceneManager.Reload();
                    break;
                    
                case SceneLoadAction.QuitGame:
                    QuitGame();
                    break;
                    
                default:
                    Debug.LogWarning($"[SceneLoaderButton] Unknown load action: {loadAction}");
                    break;
            }
        }
        
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
            GameSceneManager.Load(name);
        }
        
        /// <summary>
        /// Reload current scene (can be assigned directly to Button.onClick)
        /// </summary>
        public void ReloadScene()
        {
            if (hideGameOverOnLoad && GameOverManager.Instance != null)
            {
                GameOverManager.Instance.HideGameOver();
            }
            GameSceneManager.Reload();
        }
        
        /// <summary>
        /// Load main menu (can be assigned directly to Button.onClick)
        /// </summary>
        public void LoadMainMenu()
        {
            if (hideGameOverOnLoad && GameOverManager.Instance != null)
            {
                GameOverManager.Instance.HideGameOver();
            }
            GameSceneManager.GoToMainMenu();
        }
        
        /// <summary>
        /// Load next scene (can be assigned directly to Button.onClick)
        /// </summary>
        public void LoadNext()
        {
            if (hideGameOverOnLoad && GameOverManager.Instance != null)
            {
                GameOverManager.Instance.HideGameOver();
            }
            GameSceneManager.LoadNext();
        }
        
        /// <summary>
        /// Quit game (can be assigned directly to Button.onClick)
        /// </summary>
        public void Quit()
        {
            QuitGame();
        }
        
        #endregion
        
        void OnDestroy()
        {
            if (button != null && loadOnClick)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }
    }
}
