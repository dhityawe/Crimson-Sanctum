using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrimsonSanctum.Audio
{
    /// <summary>
    /// Main AudioManager that coordinates all audio subsystems.
    /// Acts as a simple facade and coordinator following SRP.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AudioConfig audioConfig;
        
        [Header("Managers")]
        [SerializeField] private BGMManager bgmManager;
        [SerializeField] private SFXManager sfxManager;
        [SerializeField] private CrossFadeHandler crossFadeHandler;

        // Singleton pattern
        private static AudioManager instance;
        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<AudioManager>();
                    if (instance == null)
                    {
                        GameObject audioManagerGO = new GameObject("AudioManager");
                        instance = audioManagerGO.AddComponent<AudioManager>();
                        DontDestroyOnLoad(audioManagerGO);
                    }
                }
                return instance;
            }
        }

        // Public properties for easy access
        public BGMManager BGM => bgmManager;
        public SFXManager SFX => sfxManager;
        public CrossFadeHandler CrossFade => crossFadeHandler;
        public AudioConfig Config => audioConfig;

        private void Awake()
        {
            // Implement singleton pattern
            if (instance == null)
            {
                instance = this;
                if (audioConfig != null && audioConfig.persistAcrossScenes)
                {
                    DontDestroyOnLoad(gameObject);
                }
                InitializeAudioSystem();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                AudioEvents.ClearAllEvents();
            }
        }

        /// <summary>
        /// Initialize the entire audio system
        /// </summary>
        private void InitializeAudioSystem()
        {
            // Validate configuration
            if (audioConfig == null)
            {
                Debug.LogError("[AudioManager] AudioConfig is null! Please assign an AudioConfig ScriptableObject.");
                return;
            }

            // Auto-create managers if not assigned
            CreateManagersIfNeeded();

            // Initialize all subsystems
            InitializeManagers();

            // Handle initial scene BGM
            string currentSceneName = SceneManager.GetActiveScene().name;
            bgmManager.StartInitialBGM(currentSceneName);

            Debug.Log($"[AudioManager] Audio system initialized for scene: {currentSceneName}");
            AudioEvents.OnSceneAudioInitialized?.Invoke(currentSceneName);
        }

        private void CreateManagersIfNeeded()
        {
            // Create BGM Manager if not assigned
            if (bgmManager == null)
            {
                GameObject bgmGO = new GameObject("BGM Manager");
                bgmGO.transform.SetParent(transform);
                bgmManager = bgmGO.AddComponent<BGMManager>();
            }

            // Create SFX Manager if not assigned
            if (sfxManager == null)
            {
                GameObject sfxGO = new GameObject("SFX Manager");
                sfxGO.transform.SetParent(transform);
                sfxManager = sfxGO.AddComponent<SFXManager>();
            }

            // Create CrossFade Handler if not assigned
            if (crossFadeHandler == null)
            {
                GameObject crossFadeGO = new GameObject("CrossFade Handler");
                crossFadeGO.transform.SetParent(transform);
                crossFadeHandler = crossFadeGO.AddComponent<CrossFadeHandler>();
            }
        }

        private void InitializeManagers()
        {
            // Initialize in order of dependency
            crossFadeHandler.Initialize(audioConfig);
            bgmManager.Initialize(audioConfig);
            sfxManager.Initialize(audioConfig);
        }

        /// <summary>
        /// Called when a new scene is loaded
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string sceneName = scene.name;
            Debug.Log($"[AudioManager] Scene loaded: {sceneName}");
            
            // Let BGM Manager handle scene-specific logic
            bgmManager.HandleSceneLoaded(sceneName);
            
            // Notify other systems
            AudioEvents.OnSceneAudioInitialized?.Invoke(sceneName);
        }

        #region Public API - BGM Controls

        /// <summary>
        /// Play BGM for the specified scene
        /// </summary>
        public void PlaySceneBGM(string sceneName)
        {
            AudioEvents.OnSceneBGMRequested?.Invoke(sceneName);
        }

        /// <summary>
        /// Play BGM for the current scene
        /// </summary>
        public void PlayCurrentSceneBGM()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            PlaySceneBGM(currentScene);
        }

        /// <summary>
        /// Stop the currently playing BGM
        /// </summary>
        public void StopBGM()
        {
            AudioEvents.OnBGMStopped?.Invoke();
        }

        /// <summary>
        /// Pause the currently playing BGM
        /// </summary>
        public void PauseBGM()
        {
            AudioEvents.OnBGMPaused?.Invoke();
        }

        /// <summary>
        /// Resume the currently paused BGM
        /// </summary>
        public void ResumeBGM()
        {
            AudioEvents.OnBGMResumed?.Invoke();
        }

        /// <summary>
        /// Play next BGM track in the current scene
        /// </summary>
        public void PlayNextBGMTrack()
        {
            bgmManager.PlayNextTrack();
        }

        /// <summary>
        /// Play previous BGM track in the current scene
        /// </summary>
        public void PlayPreviousBGMTrack()
        {
            bgmManager.PlayPreviousTrack();
        }

        #endregion

        #region Public API - SFX Controls

        /// <summary>
        /// Play a sound effect by name
        /// </summary>
        public int PlaySFX(string sfxName, float volumeMultiplier = 1f)
        {
            return sfxManager.PlaySFX(sfxName, volumeMultiplier);
        }

        /// <summary>
        /// Play a sound effect by AudioClip
        /// </summary>
        public int PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            return sfxManager.PlaySFX(clip, volumeMultiplier);
        }

        /// <summary>
        /// Play a 3D positioned sound effect
        /// </summary>
        public int PlaySFX3D(string sfxName, Vector3 position, float volumeMultiplier = 1f)
        {
            return sfxManager.PlaySFX(sfxName, volumeMultiplier, position);
        }

        /// <summary>
        /// Play a 3D positioned sound effect by AudioClip
        /// </summary>
        public int PlaySFX3D(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
        {
            return sfxManager.PlaySFX(clip, volumeMultiplier, position);
        }

        /// <summary>
        /// Stop all instances of a named SFX
        /// </summary>
        public void StopSFX(string sfxName)
        {
            AudioEvents.OnSFXStopped?.Invoke(sfxName);
        }

        /// <summary>
        /// Stop all sound effects
        /// </summary>
        public void StopAllSFX()
        {
            AudioEvents.OnAllSFXStopped?.Invoke();
        }

        #endregion

        #region Public API - Volume Controls

        /// <summary>
        /// Set the global music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            audioConfig.globalMusicVolume = volume;
            AudioEvents.OnMusicVolumeChanged?.Invoke(volume);
        }

        /// <summary>
        /// Set the global SFX volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            audioConfig.globalSFXVolume = volume;
            AudioEvents.OnSFXVolumeChanged?.Invoke(volume);
        }

        /// <summary>
        /// Get the current music volume
        /// </summary>
        public float GetMusicVolume()
        {
            return audioConfig != null ? audioConfig.globalMusicVolume : 1f;
        }

        /// <summary>
        /// Get the current SFX volume
        /// </summary>
        public float GetSFXVolume()
        {
            return audioConfig != null ? audioConfig.globalSFXVolume : 1f;
        }

        #endregion

        #region Public API - CrossFade Controls

        /// <summary>
        /// Enable or disable crossfade functionality
        /// </summary>
        public void SetCrossFadeEnabled(bool enabled)
        {
            AudioEvents.OnCrossFadeToggled?.Invoke(enabled);
        }

        /// <summary>
        /// Set the crossfade duration
        /// </summary>
        public void SetCrossFadeDuration(float duration)
        {
            AudioEvents.OnCrossFadeDurationChanged?.Invoke(duration);
        }

        /// <summary>
        /// Check if crossfade is currently active
        /// </summary>
        public bool IsCrossFading()
        {
            return crossFadeHandler != null && crossFadeHandler.IsCrossFading;
        }

        /// <summary>
        /// Stop any ongoing crossfade
        /// </summary>
        public void StopCrossFade()
        {
            crossFadeHandler?.StopCrossFade();
        }

        #endregion

        #region Public API - Status Queries

        /// <summary>
        /// Check if BGM is currently playing
        /// </summary>
        public bool IsBGMPlaying()
        {
            return bgmManager != null && bgmManager.IsPlaying;
        }

        /// <summary>
        /// Check if BGM is currently paused
        /// </summary>
        public bool IsBGMPaused()
        {
            return bgmManager != null && bgmManager.IsPaused;
        }

        /// <summary>
        /// Get the currently playing BGM track
        /// </summary>
        public BGMManager.BGMTrack GetCurrentBGMTrack()
        {
            return bgmManager?.CurrentTrack;
        }

        /// <summary>
        /// Get the current number of active SFX
        /// </summary>
        public int GetActiveSFXCount()
        {
            return sfxManager != null ? sfxManager.ActiveSFXCount : 0;
        }

        #endregion

        #region Static API for Easy Access

        /// <summary>
        /// Static method to play BGM for current scene
        /// </summary>
        public static void PlayBGM()
        {
            Instance?.PlayCurrentSceneBGM();
        }

        /// <summary>
        /// Static method to stop BGM
        /// </summary>
        public static void StopBGMStatic()
        {
            Instance?.StopBGM();
        }

        /// <summary>
        /// Static method to pause BGM
        /// </summary>
        public static void PauseBGMStatic()
        {
            Instance?.PauseBGM();
        }

        /// <summary>
        /// Static method to resume BGM
        /// </summary>
        public static void ResumeBGMStatic()
        {
            Instance?.ResumeBGM();
        }

        /// <summary>
        /// Static method to play SFX
        /// </summary>
        public static int PlaySFXStatic(string sfxName, float volume = 1f)
        {
            return Instance?.PlaySFX(sfxName, volume) ?? -1;
        }

        /// <summary>
        /// Static method to play 3D SFX
        /// </summary>
        public static int PlaySFX3DStatic(string sfxName, Vector3 position, float volume = 1f)
        {
            return Instance?.PlaySFX3D(sfxName, position, volume) ?? -1;
        }

        /// <summary>
        /// Static method to set music volume
        /// </summary>
        public static void SetMusicVolumeStatic(float volume)
        {
            Instance?.SetMusicVolume(volume);
        }

        /// <summary>
        /// Static method to set SFX volume
        /// </summary>
        public static void SetSFXVolumeStatic(float volume)
        {
            Instance?.SetSFXVolume(volume);
        }

        #endregion
    }
}