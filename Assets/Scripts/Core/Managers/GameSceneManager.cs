using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Custom GameSceneManager for handling scene transitions with drag-and-drop scene configuration.
/// Provides easy scene loading through Inspector configuration and button integration.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneReference
    {
        [SerializeField] private Object sceneAsset;
        [SerializeField] private string sceneName;
        
        public string SceneName
        {
            get
            {
                // If scene asset is assigned, get name from it
                if (sceneAsset != null)
                {
                    return sceneAsset.name;
                }
                // Otherwise use manually entered scene name
                return sceneName;
            }
        }
        
        // Constructor for direct scene name assignment
        public SceneReference(string name)
        {
            sceneName = name;
        }
        
        // Default constructor
        public SceneReference() { }
        
        /// <summary>
        /// Check if this scene reference is valid
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SceneName);
        }
        
        /// <summary>
        /// Check if the scene exists in build settings
        /// </summary>
        public bool ExistsInBuildSettings()
        {
            if (!IsValid()) return false;
            
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                string buildSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (buildSceneName == SceneName)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [Header("Scene Configuration")]
    [SerializeField] private SceneReference nextScene;
    [SerializeField] private SceneReference previousScene;
    [SerializeField] private SceneReference mainMenuScene;
    [SerializeField] private SceneReference gameplayScene;
    
    [Header("Scene Collection")]
    [SerializeField] private SceneReference[] availableScenes;
    
    [Header("Loading Settings")]
    [SerializeField] private bool useAsyncLoading = true;
    [SerializeField] private bool showLoadingProgress = true;
    [Range(0f, 5f)]
    [SerializeField] private float minimumLoadingTime = 1f;
    [SerializeField] private bool allowSceneActivation = true;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Events
    public System.Action<string> OnSceneLoadStarted;
    public System.Action<string, float> OnSceneLoadProgress;
    public System.Action<string> OnSceneLoadCompleted;
    public System.Action<string> OnSceneLoadFailed;
    
    // Static instance for easy access
    private static GameSceneManager instance;
    public static GameSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameSceneManager>();
                if (instance == null)
                {
                    GameObject sceneManagerGO = new GameObject("GameSceneManager");
                    instance = sceneManagerGO.AddComponent<GameSceneManager>();
                    DontDestroyOnLoad(sceneManagerGO);
                }
            }
            return instance;
        }
    }
    
    // Current loading operation
    private AsyncOperation currentLoadOperation;
    private bool isLoading = false;
    
    private void Awake()
    {
        // Implement singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSceneManager();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeSceneManager()
    {
        if (enableDebugLogs)
        {
            Debug.Log("GameSceneManager initialized");
        }
        
        // Validate scenes in build settings
        ValidateScenes();
    }
    
    /// <summary>
    /// Validate all configured scenes exist in build settings
    /// </summary>
    private void ValidateScenes()
    {
        if (enableDebugLogs)
        {
            Debug.Log("Validating scenes in build settings...");
        }
        
        ValidateSceneReference(nextScene, "Next Scene");
        ValidateSceneReference(previousScene, "Previous Scene");
        ValidateSceneReference(mainMenuScene, "Main Menu Scene");
        ValidateSceneReference(gameplayScene, "Gameplay Scene");
        
        if (availableScenes != null)
        {
            for (int i = 0; i < availableScenes.Length; i++)
            {
                ValidateSceneReference(availableScenes[i], $"Available Scene [{i}]");
            }
        }
    }
    
    private void ValidateSceneReference(SceneReference sceneRef, string label)
    {
        if (sceneRef != null && sceneRef.IsValid())
        {
            if (!sceneRef.ExistsInBuildSettings())
            {
                Debug.LogWarning($"[GameSceneManager] {label} '{sceneRef.SceneName}' is not found in build settings!");
            }
            else if (enableDebugLogs)
            {
                Debug.Log($"[GameSceneManager] {label} '{sceneRef.SceneName}' validated successfully");
            }
        }
    }
    
    #region Scene Loading Methods
    
    /// <summary>
    /// Load the configured next scene
    /// </summary>
    public void LoadNextScene()
    {
        if (nextScene != null && nextScene.IsValid())
        {
            LoadScene(nextScene.SceneName);
        }
        else
        {
            Debug.LogWarning("[GameSceneManager] Next scene is not configured or invalid!");
        }
    }
    
    /// <summary>
    /// Load the configured previous scene
    /// </summary>
    public void LoadPreviousScene()
    {
        if (previousScene != null && previousScene.IsValid())
        {
            LoadScene(previousScene.SceneName);
        }
        else
        {
            Debug.LogWarning("[GameSceneManager] Previous scene is not configured or invalid!");
        }
    }
    
    /// <summary>
    /// Load the main menu scene
    /// </summary>
    public void LoadMainMenu()
    {
        if (mainMenuScene != null && mainMenuScene.IsValid())
        {
            LoadScene(mainMenuScene.SceneName);
        }
        else
        {
            Debug.LogWarning("[GameSceneManager] Main menu scene is not configured or invalid!");
        }
    }
    
    /// <summary>
    /// Load the gameplay scene
    /// </summary>
    public void LoadGameplay()
    {
        if (gameplayScene != null && gameplayScene.IsValid())
        {
            LoadScene(gameplayScene.SceneName);
        }
        else
        {
            Debug.LogWarning("[GameSceneManager] Gameplay scene is not configured or invalid!");
        }
    }
    
    /// <summary>
    /// Load a scene by name
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[GameSceneManager] Scene name is null or empty!");
            OnSceneLoadFailed?.Invoke(sceneName);
            return;
        }
        
        if (isLoading)
        {
            Debug.LogWarning($"[GameSceneManager] Already loading a scene. Cannot load '{sceneName}'");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GameSceneManager] Loading scene: {sceneName}");
        }
        
        OnSceneLoadStarted?.Invoke(sceneName);
        
        if (useAsyncLoading)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        else
        {
            LoadSceneImmediate(sceneName);
        }
    }
    
    /// <summary>
    /// Load a scene from the available scenes array by index
    /// </summary>
    public void LoadSceneByIndex(int index)
    {
        if (availableScenes == null || index < 0 || index >= availableScenes.Length)
        {
            Debug.LogError($"[GameSceneManager] Invalid scene index: {index}");
            return;
        }
        
        SceneReference sceneRef = availableScenes[index];
        if (sceneRef != null && sceneRef.IsValid())
        {
            LoadScene(sceneRef.SceneName);
        }
        else
        {
            Debug.LogError($"[GameSceneManager] Scene at index {index} is invalid!");
        }
    }
    
    /// <summary>
    /// Immediate scene loading (synchronous)
    /// </summary>
    private void LoadSceneImmediate(string sceneName)
    {
        try
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            OnSceneLoadCompleted?.Invoke(sceneName);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[GameSceneManager] Scene '{sceneName}' loaded successfully");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameSceneManager] Failed to load scene '{sceneName}': {e.Message}");
            OnSceneLoadFailed?.Invoke(sceneName);
        }
    }
    
    /// <summary>
    /// Asynchronous scene loading with progress tracking
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        isLoading = true;
        float startTime = Time.time;
        
        // Start async loading
        currentLoadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        
        if (currentLoadOperation == null)
        {
            Debug.LogError($"[GameSceneManager] Failed to start async loading for scene '{sceneName}'");
            OnSceneLoadFailed?.Invoke(sceneName);
            isLoading = false;
            yield break;
        }
        
        // Prevent automatic scene activation if needed
        currentLoadOperation.allowSceneActivation = allowSceneActivation;
        
        // Track loading progress
        while (!currentLoadOperation.isDone)
        {
            float progress = Mathf.Clamp01(currentLoadOperation.progress / 0.9f);
            
            if (showLoadingProgress)
            {
                OnSceneLoadProgress?.Invoke(sceneName, progress);
            }
            
            // If scene is ready but we want to wait for minimum loading time
            if (currentLoadOperation.progress >= 0.9f)
            {
                float elapsedTime = Time.time - startTime;
                if (elapsedTime >= minimumLoadingTime)
                {
                    currentLoadOperation.allowSceneActivation = true;
                }
            }
            
            yield return null;
        }
        
        // Ensure minimum loading time has passed
        float totalElapsedTime = Time.time - startTime;
        if (totalElapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - totalElapsedTime);
        }
        
        isLoading = false;
        currentLoadOperation = null;
        OnSceneLoadCompleted?.Invoke(sceneName);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GameSceneManager] Scene '{sceneName}' loaded successfully (Async)");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Get the current scene name
    /// </summary>
    public string GetCurrentSceneName()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
    
    /// <summary>
    /// Check if currently loading a scene
    /// </summary>
    public bool IsLoading()
    {
        return isLoading;
    }
    
    /// <summary>
    /// Get loading progress (0-1)
    /// </summary>
    public float GetLoadingProgress()
    {
        if (currentLoadOperation != null)
        {
            return Mathf.Clamp01(currentLoadOperation.progress / 0.9f);
        }
        return isLoading ? 0f : 1f;
    }
    
    /// <summary>
    /// Set the next scene to load
    /// </summary>
    public void SetNextScene(string sceneName)
    {
        if (nextScene == null)
            nextScene = new SceneReference();
        
        nextScene = new SceneReference(sceneName);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GameSceneManager] Next scene set to: {sceneName}");
        }
    }
    
    /// <summary>
    /// Check if a scene exists in build settings
    /// </summary>
    public bool SceneExistsInBuildSettings(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string buildSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (buildSceneName == sceneName)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get all scene names from available scenes
    /// </summary>
    public string[] GetAvailableSceneNames()
    {
        if (availableScenes == null) return new string[0];
        
        List<string> sceneNames = new List<string>();
        foreach (var sceneRef in availableScenes)
        {
            if (sceneRef != null && sceneRef.IsValid())
            {
                sceneNames.Add(sceneRef.SceneName);
            }
        }
        return sceneNames.ToArray();
    }
    
    #endregion
    
    #region Static Methods for Easy Access
    
    /// <summary>
    /// Static method to load next scene
    /// </summary>
    public static void LoadNext()
    {
        Instance.LoadNextScene();
    }
    
    /// <summary>
    /// Static method to load previous scene
    /// </summary>
    public static void LoadPrevious()
    {
        Instance.LoadPreviousScene();
    }
    
    /// <summary>
    /// Static method to load main menu
    /// </summary>
    public static void GoToMainMenu()
    {
        Instance.LoadMainMenu();
    }
    
    /// <summary>
    /// Static method to load gameplay
    /// </summary>
    public static void StartGameplay()
    {
        Instance.LoadGameplay();
    }
    
    /// <summary>
    /// Static method to load scene by name
    /// </summary>
    public static void Load(string sceneName)
    {
        Instance.LoadScene(sceneName);
    }
    
    /// <summary>
    /// Static method to reload current scene
    /// </summary>
    public static void Reload()
    {
        Instance.ReloadCurrentScene();
    }
    
    #endregion
    
    #region Scene Reload
    
    /// <summary>
    /// Reload the current scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentScene = GetCurrentSceneName();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[GameSceneManager] Reloading current scene: {currentScene}");
        }
        
        LoadScene(currentScene);
    }
    
    #endregion
}