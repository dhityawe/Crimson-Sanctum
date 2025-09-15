using UnityEngine;
using System.Collections.Generic;
using Hellmade.Sound;

/// <summary>
/// AudioManager that handles BGM (Background Music) management across different scenes.
/// Built as a wrapper around EazySoundManager for scene-specific music control.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class BGMTrack
    {
        public string trackName;
        public AudioClip audioClip;
        [Range(0f, 1f)]
        public float volume = 1f;
        public bool loop = true;
        [Range(0f, 5f)]
        public float fadeInSeconds = 1f;
        [Range(0f, 5f)]
        public float fadeOutSeconds = 1f;
    }

    [System.Serializable]
    public class SceneBGM
    {
        public string sceneName;
        public BGMTrack[] bgmTracks;
        public bool randomizePlayback = false;
        public bool shuffleOnSceneLoad = false;
    }

    [Header("BGM Configuration")]
    [SerializeField] private SceneBGM[] sceneBGMList;
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float globalMusicVolume = 1f;
    [SerializeField] private bool persistAcrossScenes = true;
    
    [Header("CrossFade Settings")]
    [SerializeField] private bool enableCrossFade = true;
    [Range(0.1f, 10f)]
    [SerializeField] private float crossFadeDuration = 2f;
    [SerializeField] private bool crossFadeOnSceneChange = true;
    [SerializeField] private bool crossFadeOnTrackChange = true;
    
    [Header("Fade-In Settings")]
    [SerializeField] private bool alwaysFadeInFromZero = true;
    [SerializeField] private bool fadeInOnLoopRestart = true;
    [Range(0.1f, 10f)]
    [SerializeField] private float defaultFadeInDuration = 1.5f;
    
    [Header("Start Delay Settings")]
    [SerializeField] private bool enableStartDelay = true;
    [Range(0f, 10f)]
    [SerializeField] private float startDelayDuration = 2f;
    [SerializeField] private bool startDelayOnlyOnGameStart = true;

    // Static instance for singleton pattern
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

    // Container for BGM tracks by scene
    private Dictionary<string, SceneBGM> sceneBGMContainer;
    
    // Current playing music info
    private int currentMusicID = -1;
    private string currentSceneName;
    private BGMTrack currentTrack;
    private int currentTrackIndex = 0;
    
    // CrossFade related variables
    private int previousMusicID = -1;
    private bool isCrossFading = false;
    private System.Collections.IEnumerator currentCrossFadeCoroutine;
    
    // Fade-in related variables
    private bool isFadingIn = false;
    private System.Collections.IEnumerator currentFadeInCoroutine;
    
    // Start delay related variables
    private bool hasGameStarted = false;
    private System.Collections.IEnumerator currentStartDelayCoroutine;

    private void Awake()
    {
        // Implement singleton pattern
        if (instance == null)
        {
            instance = this;
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
            InitializeAudioManager();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Initialize the AudioManager and populate the BGM container
    /// </summary>
    private void InitializeAudioManager()
    {
        sceneBGMContainer = new Dictionary<string, SceneBGM>();
        
        // Populate the container from the serialized array
        foreach (SceneBGM sceneBGM in sceneBGMList)
        {
            if (!string.IsNullOrEmpty(sceneBGM.sceneName))
            {
                sceneBGMContainer[sceneBGM.sceneName] = sceneBGM;
            }
        }
        
        // Set global music volume
        EazySoundManager.GlobalMusicVolume = globalMusicVolume;
        
        // Get current scene name
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        Debug.Log($"AudioManager initialized for scene: {currentSceneName}");
        
        // Auto-play BGM for the initial scene (like MainMenu) with start delay and fade-in
        if (!string.IsNullOrEmpty(currentSceneName))
        {
            if (enableStartDelay)
            {
                // Store the coroutine reference
                currentStartDelayCoroutine = StartDelayedBGM(currentSceneName, true);
                StartCoroutine(currentStartDelayCoroutine);
            }
            else
            {
                PlaySceneBGM(currentSceneName);
            }
        }
    }

    /// <summary>
    /// Called when a new scene is loaded
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        Debug.Log($"Scene loaded: {currentSceneName}");
        
        // Auto-play BGM for the new scene with start delay (only if configured)
        if (enableStartDelay && startDelayOnlyOnGameStart && hasGameStarted)
        {
            // This is a subsequent scene load after game start, no start delay needed
            PlaySceneBGM(currentSceneName);
        }
        else if (enableStartDelay && !startDelayOnlyOnGameStart)
        {
            // Start delay on every scene change
            currentStartDelayCoroutine = StartDelayedBGM(currentSceneName, false);
            StartCoroutine(currentStartDelayCoroutine);
        }
        else if (enableStartDelay && startDelayOnlyOnGameStart && !hasGameStarted)
        {
            // This is the first scene load but we already have delay running from InitializeAudioManager
            // Do nothing, let the existing delay complete
            Debug.Log("Start delay already running from initialization, skipping scene load delay");
        }
        else
        {
            // No start delay configured
            PlaySceneBGM(currentSceneName);
        }
        
        // Mark that the game has started after the first scene load
        hasGameStarted = true;
    }

    /// <summary>
    /// Play BGM for the specified scene
    /// </summary>
    public void PlaySceneBGM(string sceneName)
    {
        // Cancel any ongoing start delay since we're manually starting BGM
        if (currentStartDelayCoroutine != null)
        {
            StopCoroutine(currentStartDelayCoroutine);
            currentStartDelayCoroutine = null;
            Debug.Log("Cancelled ongoing start delay due to manual BGM start");
        }

        if (!sceneBGMContainer.ContainsKey(sceneName))
        {
            Debug.LogWarning($"No BGM configuration found for scene: {sceneName}");
            return;
        }

        SceneBGM sceneBGM = sceneBGMContainer[sceneName];
        
        if (sceneBGM.bgmTracks == null || sceneBGM.bgmTracks.Length == 0)
        {
            Debug.LogWarning($"No BGM tracks configured for scene: {sceneName}");
            return;
        }

        // Select track to play
        BGMTrack trackToPlay;
        if (sceneBGM.randomizePlayback)
        {
            currentTrackIndex = Random.Range(0, sceneBGM.bgmTracks.Length);
            trackToPlay = sceneBGM.bgmTracks[currentTrackIndex];
        }
        else
        {
            currentTrackIndex = 0;
            trackToPlay = sceneBGM.bgmTracks[currentTrackIndex];
        }

        // Use CrossFade if enabled and there's currently playing music
        if (enableCrossFade && crossFadeOnSceneChange && currentMusicID != -1)
        {
            CrossFadeToBGMTrack(trackToPlay);
        }
        else
        {
            // Stop current music if playing
            if (currentMusicID != -1)
            {
                StopCurrentBGM();
            }
            PlayBGMTrack(trackToPlay);
        }
    }

    /// <summary>
    /// Play a specific BGM track
    /// </summary>
    private void PlayBGMTrack(BGMTrack track)
    {
        if (track == null || track.audioClip == null)
        {
            Debug.LogWarning("Cannot play BGM track: track or audio clip is null");
            return;
        }

        currentTrack = track;
        
        // Determine fade-in behavior
        bool shouldFadeIn = alwaysFadeInFromZero;
        float fadeInDuration = shouldFadeIn ? (track.fadeInSeconds > 0 ? track.fadeInSeconds : defaultFadeInDuration) : 0f;
        float startVolume = shouldFadeIn ? 0f : track.volume;
        
        // Use EazySoundManager to play the music
        currentMusicID = EazySoundManager.PlayMusic(
            track.audioClip,
            startVolume, // Start at 0 if fading in, otherwise target volume
            track.loop,
            persistAcrossScenes,
            0f, // We'll handle fade-in manually for better control
            track.fadeOutSeconds
        );

        Debug.Log($"Playing BGM: {track.trackName} (ID: {currentMusicID}) - FadeIn: {shouldFadeIn}");
        
        // Start fade-in if enabled
        if (shouldFadeIn && fadeInDuration > 0f)
        {
            StartFadeIn(track.volume, fadeInDuration);
        }
        
        // Set up loop monitoring if fade-in on loop restart is enabled
        if (fadeInOnLoopRestart && track.loop)
        {
            StartCoroutine(MonitorLoopRestart());
        }
    }

    /// <summary>
    /// CrossFade to a new BGM track
    /// </summary>
    private void CrossFadeToBGMTrack(BGMTrack newTrack)
    {
        if (newTrack == null || newTrack.audioClip == null)
        {
            Debug.LogWarning("Cannot crossfade to BGM track: track or audio clip is null");
            return;
        }

        if (isCrossFading)
        {
            // Stop current crossfade if one is already in progress
            if (currentCrossFadeCoroutine != null)
            {
                StopCoroutine(currentCrossFadeCoroutine);
            }
        }

        currentCrossFadeCoroutine = CrossFadeCoroutine(newTrack);
        StartCoroutine(currentCrossFadeCoroutine);
    }

    /// <summary>
    /// Coroutine that handles the crossfade transition
    /// </summary>
    private System.Collections.IEnumerator CrossFadeCoroutine(BGMTrack newTrack)
    {
        isCrossFading = true;
        previousMusicID = currentMusicID;

        // Start playing the new track at volume 0
        currentTrack = newTrack;
        currentMusicID = EazySoundManager.PlayMusic(
            newTrack.audioClip,
            0f, // Start at volume 0
            newTrack.loop,
            persistAcrossScenes,
            0f, // No fade in since we're handling it manually
            newTrack.fadeOutSeconds
        );

        Debug.Log($"CrossFading from {EazySoundManager.GetMusicAudio(previousMusicID)?.Clip?.name} to {newTrack.trackName}");

        // Get references to both audio objects
        Audio previousAudio = EazySoundManager.GetMusicAudio(previousMusicID);
        Audio newAudio = EazySoundManager.GetMusicAudio(currentMusicID);

        if (newAudio == null)
        {
            Debug.LogError("Failed to create new audio for crossfade");
            isCrossFading = false;
            yield break;
        }

        float elapsedTime = 0f;
        float previousStartVolume = previousAudio?.Volume ?? 0f;
        float newTargetVolume = newTrack.volume;

        // Perform the crossfade
        while (elapsedTime < crossFadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float fadeProgress = elapsedTime / crossFadeDuration;

            // Fade out the previous track
            if (previousAudio != null)
            {
                float previousVolume = Mathf.Lerp(previousStartVolume, 0f, fadeProgress);
                previousAudio.SetVolume(previousVolume, 0f);
            }

            // Fade in the new track
            if (newAudio != null)
            {
                float newVolume = Mathf.Lerp(0f, newTargetVolume, fadeProgress);
                newAudio.SetVolume(newVolume, 0f);
            }

            yield return null;
        }

        // Ensure final volumes are set correctly
        if (previousAudio != null)
        {
            previousAudio.Stop();
        }
        
        if (newAudio != null)
        {
            newAudio.SetVolume(newTargetVolume, 0f);
        }

        // Clean up
        previousMusicID = -1;
        isCrossFading = false;
        currentCrossFadeCoroutine = null;

        Debug.Log($"CrossFade completed to: {newTrack.trackName}");
    }

    /// <summary>
    /// Start a fade-in effect for the current track
    /// </summary>
    private void StartFadeIn(float targetVolume, float fadeInDuration)
    {
        // Stop any existing fade-in
        if (isFadingIn && currentFadeInCoroutine != null)
        {
            StopCoroutine(currentFadeInCoroutine);
        }
        
        currentFadeInCoroutine = FadeInCoroutine(targetVolume, fadeInDuration);
        StartCoroutine(currentFadeInCoroutine);
    }
    
    /// <summary>
    /// Coroutine that handles fade-in from 0 to target volume
    /// </summary>
    private System.Collections.IEnumerator FadeInCoroutine(float targetVolume, float fadeInDuration)
    {
        isFadingIn = true;
        Audio currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
        
        if (currentAudio == null)
        {
            Debug.LogError("Failed to get audio for fade-in");
            isFadingIn = false;
            yield break;
        }
        
        float elapsedTime = 0f;
        float startVolume = 0f;
        
        Debug.Log($"Starting fade-in: 0 -> {targetVolume} over {fadeInDuration} seconds");
        
        // Ensure we start at 0 volume
        currentAudio.SetVolume(startVolume, 0f);
        
        // Perform the fade-in
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float fadeProgress = elapsedTime / fadeInDuration;
            float currentVolume = Mathf.Lerp(startVolume, targetVolume, fadeProgress);
            
            if (currentAudio != null)
            {
                currentAudio.SetVolume(currentVolume, 0f);
            }
            
            yield return null;
        }
        
        // Ensure final volume is set correctly
        if (currentAudio != null)
        {
            currentAudio.SetVolume(targetVolume, 0f);
        }
        
        isFadingIn = false;
        currentFadeInCoroutine = null;
        
        Debug.Log($"Fade-in completed to volume: {targetVolume}");
    }
    
    /// <summary>
    /// Monitor for loop restarts and apply fade-in if enabled
    /// </summary>
    private System.Collections.IEnumerator MonitorLoopRestart()
    {
        Audio currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
        if (currentAudio == null || currentTrack == null) yield break;
        
        float trackLength = currentAudio.Clip.length;
        float lastPlaybackTime = 0f;
        
        while (currentAudio != null && currentAudio.IsPlaying && currentTrack != null)
        {
            if (currentAudio.AudioSource != null)
            {
                float currentTime = currentAudio.AudioSource.time;
                
                // Detect loop restart (time went backwards significantly)
                if (currentTime < lastPlaybackTime - 0.5f) // 0.5f tolerance for timing precision
                {
                    Debug.Log($"Loop restart detected for: {currentTrack.trackName}");
                    
                    // Apply fade-in on loop restart
                    if (fadeInOnLoopRestart && !isCrossFading)
                    {
                        float fadeInDuration = currentTrack.fadeInSeconds > 0 ? currentTrack.fadeInSeconds : defaultFadeInDuration;
                        StartFadeIn(currentTrack.volume, fadeInDuration);
                    }
                }
                
                lastPlaybackTime = currentTime;
            }
            
            yield return new WaitForSeconds(0.1f); // Check every 100ms
        }
    }

    /// <summary>
    /// Coroutine that handles start delay before playing BGM
    /// </summary>
    private System.Collections.IEnumerator StartDelayedBGM(string sceneName, bool isGameStart)
    {
        Debug.Log($"Start delay active: waiting {startDelayDuration} seconds before playing BGM for scene: {sceneName}");
        
        // Wait for the specified delay duration - complete silence until this finishes
        yield return new WaitForSeconds(startDelayDuration);
        
        // After delay, check if we should still play BGM
        if (currentStartDelayCoroutine != null)
        {
            // Only play if this coroutine wasn't cancelled
            Debug.Log($"Start delay completed, now playing BGM for scene: {sceneName}");
            
            // Don't call PlaySceneBGM as it would cancel this coroutine
            // Call the BGM logic directly
            if (sceneBGMContainer.ContainsKey(sceneName))
            {
                SceneBGM sceneBGM = sceneBGMContainer[sceneName];
                
                if (sceneBGM.bgmTracks != null && sceneBGM.bgmTracks.Length > 0)
                {
                    // Select track to play
                    BGMTrack trackToPlay;
                    if (sceneBGM.randomizePlayback)
                    {
                        currentTrackIndex = Random.Range(0, sceneBGM.bgmTracks.Length);
                        trackToPlay = sceneBGM.bgmTracks[currentTrackIndex];
                    }
                    else
                    {
                        currentTrackIndex = 0;
                        trackToPlay = sceneBGM.bgmTracks[currentTrackIndex];
                    }

                    // Start playing the track (no crossfade since we're starting from silence)
                    PlayBGMTrack(trackToPlay);
                }
                else
                {
                    Debug.LogWarning($"No BGM tracks configured for scene: {sceneName}");
                }
            }
            else
            {
                Debug.LogWarning($"No BGM configuration found for scene: {sceneName}");
            }
        }
        else
        {
            Debug.Log("Start delay coroutine was cancelled, not playing BGM");
        }
        
        // Clear the coroutine reference
        currentStartDelayCoroutine = null;
    }

    /// <summary>
    /// Stop the currently playing BGM
    /// </summary>
    public void StopCurrentBGM()
    {
        // Cancel any ongoing start delay
        if (currentStartDelayCoroutine != null)
        {
            StopCoroutine(currentStartDelayCoroutine);
            currentStartDelayCoroutine = null;
            Debug.Log("Cancelled ongoing start delay due to BGM stop");
        }

        // Stop any ongoing fade-in
        if (isFadingIn && currentFadeInCoroutine != null)
        {
            StopCoroutine(currentFadeInCoroutine);
            isFadingIn = false;
            currentFadeInCoroutine = null;
        }
        
        if (currentMusicID != -1)
        {
            Audio currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
            if (currentAudio != null)
            {
                currentAudio.Stop();
                Debug.Log($"Stopped BGM: {currentTrack?.trackName}");
            }
            currentMusicID = -1;
            currentTrack = null;
        }
    }

    /// <summary>
    /// Pause the currently playing BGM
    /// </summary>
    public void PauseCurrentBGM()
    {
        if (currentMusicID != -1)
        {
            Audio currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
            if (currentAudio != null)
            {
                currentAudio.Pause();
                Debug.Log($"Paused BGM: {currentTrack?.trackName}");
            }
        }
    }

    /// <summary>
    /// Resume the currently paused BGM
    /// </summary>
    public void ResumeCurrentBGM()
    {
        if (currentMusicID != -1)
        {
            Audio currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
            if (currentAudio != null)
            {
                currentAudio.Resume();
                Debug.Log($"Resumed BGM: {currentTrack?.trackName}");
            }
        }
    }

    /// <summary>
    /// Change the volume of the currently playing BGM
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        
        if (currentMusicID != -1)
        {
            Audio currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
            if (currentAudio != null)
            {
                currentAudio.SetVolume(volume);
            }
        }
    }

    /// <summary>
    /// Set the global music volume
    /// </summary>
    public void SetGlobalMusicVolume(float volume)
    {
        globalMusicVolume = Mathf.Clamp01(volume);
        EazySoundManager.GlobalMusicVolume = globalMusicVolume;
        Debug.Log($"Global music volume set to: {globalMusicVolume}");
    }

    /// <summary>
    /// Play next track in the current scene (if multiple tracks are available)
    /// </summary>
    public void PlayNextTrack()
    {
        if (!sceneBGMContainer.ContainsKey(currentSceneName))
            return;

        SceneBGM sceneBGM = sceneBGMContainer[currentSceneName];
        
        if (sceneBGM.bgmTracks == null || sceneBGM.bgmTracks.Length <= 1)
            return;

        // Move to next track
        currentTrackIndex = (currentTrackIndex + 1) % sceneBGM.bgmTracks.Length;
        BGMTrack nextTrack = sceneBGM.bgmTracks[currentTrackIndex];
        
        // Use CrossFade if enabled and there's currently playing music
        if (enableCrossFade && crossFadeOnTrackChange && currentMusicID != -1)
        {
            CrossFadeToBGMTrack(nextTrack);
        }
        else
        {
            // Stop current track
            StopCurrentBGM();
            PlayBGMTrack(nextTrack);
        }
    }

    /// <summary>
    /// Play previous track in the current scene (if multiple tracks are available)
    /// </summary>
    public void PlayPreviousTrack()
    {
        if (!sceneBGMContainer.ContainsKey(currentSceneName))
            return;

        SceneBGM sceneBGM = sceneBGMContainer[currentSceneName];
        
        if (sceneBGM.bgmTracks == null || sceneBGM.bgmTracks.Length <= 1)
            return;

        // Move to previous track
        currentTrackIndex = (currentTrackIndex - 1 + sceneBGM.bgmTracks.Length) % sceneBGM.bgmTracks.Length;
        BGMTrack previousTrack = sceneBGM.bgmTracks[currentTrackIndex];
        
        // Use CrossFade if enabled and there's currently playing music
        if (enableCrossFade && crossFadeOnTrackChange && currentMusicID != -1)
        {
            CrossFadeToBGMTrack(previousTrack);
        }
        else
        {
            // Stop current track
            StopCurrentBGM();
            PlayBGMTrack(previousTrack);
        }
    }

    /// <summary>
    /// Get information about the currently playing BGM
    /// </summary>
    public BGMTrack GetCurrentTrack()
    {
        return currentTrack;
    }

    /// <summary>
    /// Check if BGM is currently playing
    /// </summary>
    public bool IsBGMPlaying()
    {
        if (currentMusicID != -1)
        {
            Audio currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
            return currentAudio != null && currentAudio.IsPlaying;
        }
        return false;
    }

    /// <summary>
    /// Check if BGM is currently paused
    /// </summary>
    public bool IsBGMPaused()
    {
        if (currentMusicID != -1)
        {
            Audio currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
            return currentAudio != null && currentAudio.Paused;
        }
        return false;
    }

    /// <summary>
    /// Manually trigger BGM for current scene
    /// </summary>
    public void PlayCurrentSceneBGM()
    {
        PlaySceneBGM(currentSceneName);
    }

    /// <summary>
    /// Add a new BGM track to a scene at runtime
    /// </summary>
    public void AddBGMToScene(string sceneName, BGMTrack newTrack)
    {
        if (!sceneBGMContainer.ContainsKey(sceneName))
        {
            sceneBGMContainer[sceneName] = new SceneBGM { sceneName = sceneName };
        }

        // This would require expanding the existing array, which is more complex in runtime
        // For now, just log that this functionality would need additional implementation
        Debug.Log($"Adding BGM track '{newTrack.trackName}' to scene '{sceneName}' - Feature needs runtime array expansion implementation");
    }

    #region CrossFade Control Methods

    /// <summary>
    /// Enable or disable CrossFade functionality
    /// </summary>
    public void SetCrossFadeEnabled(bool enabled)
    {
        enableCrossFade = enabled;
        Debug.Log($"CrossFade {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Set the CrossFade duration
    /// </summary>
    public void SetCrossFadeDuration(float duration)
    {
        crossFadeDuration = Mathf.Clamp(duration, 0.1f, 10f);
        Debug.Log($"CrossFade duration set to: {crossFadeDuration} seconds");
    }

    /// <summary>
    /// Enable or disable CrossFade on scene changes
    /// </summary>
    public void SetCrossFadeOnSceneChange(bool enabled)
    {
        crossFadeOnSceneChange = enabled;
        Debug.Log($"CrossFade on scene change {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Enable or disable CrossFade on track changes
    /// </summary>
    public void SetCrossFadeOnTrackChange(bool enabled)
    {
        crossFadeOnTrackChange = enabled;
        Debug.Log($"CrossFade on track change {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Check if CrossFade is currently active
    /// </summary>
    public bool IsCrossFading()
    {
        return isCrossFading;
    }

    /// <summary>
    /// Force stop any ongoing CrossFade operation
    /// </summary>
    public void StopCrossFade()
    {
        if (isCrossFading && currentCrossFadeCoroutine != null)
        {
            StopCoroutine(currentCrossFadeCoroutine);
            
            // Stop the previous track if it exists
            if (previousMusicID != -1)
            {
                Audio previousAudio = EazySoundManager.GetMusicAudio(previousMusicID);
                if (previousAudio != null)
                {
                    previousAudio.Stop();
                }
                previousMusicID = -1;
            }
            
            // Ensure current track is at full volume
            if (currentMusicID != -1)
            {
                Audio currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
                if (currentAudio != null && currentTrack != null)
                {
                    currentAudio.SetVolume(currentTrack.volume, 0f);
                }
            }
            
            isCrossFading = false;
            currentCrossFadeCoroutine = null;
            Debug.Log("CrossFade operation force stopped");
        }
    }

    /// <summary>
    /// Enable or disable fade-in from zero volume
    /// </summary>
    public void SetAlwaysFadeInFromZero(bool enabled)
    {
        alwaysFadeInFromZero = enabled;
        Debug.Log($"Always fade-in from zero {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Enable or disable fade-in on loop restart
    /// </summary>
    public void SetFadeInOnLoopRestart(bool enabled)
    {
        fadeInOnLoopRestart = enabled;
        Debug.Log($"Fade-in on loop restart {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Set the default fade-in duration
    /// </summary>
    public void SetDefaultFadeInDuration(float duration)
    {
        defaultFadeInDuration = Mathf.Clamp(duration, 0.1f, 10f);
        Debug.Log($"Default fade-in duration set to: {defaultFadeInDuration} seconds");
    }

    /// <summary>
    /// Check if fade-in is currently active
    /// </summary>
    public bool IsFadingIn()
    {
        return isFadingIn;
    }

    /// <summary>
    /// Manually trigger fade-in for current track
    /// </summary>
    public void TriggerFadeIn()
    {
        if (currentMusicID != -1 && currentTrack != null)
        {
            float fadeInDuration = currentTrack.fadeInSeconds > 0 ? currentTrack.fadeInSeconds : defaultFadeInDuration;
            StartFadeIn(currentTrack.volume, fadeInDuration);
        }
    }

    /// <summary>
    /// Enable or disable start delay functionality
    /// </summary>
    public void SetStartDelayEnabled(bool enabled)
    {
        enableStartDelay = enabled;
        Debug.Log($"Start delay {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Set the start delay duration
    /// </summary>
    public void SetStartDelayDuration(float duration)
    {
        startDelayDuration = Mathf.Clamp(duration, 0f, 10f);
        Debug.Log($"Start delay duration set to: {startDelayDuration} seconds");
    }

    /// <summary>
    /// Enable or disable start delay only on game start (vs every scene)
    /// </summary>
    public void SetStartDelayOnlyOnGameStart(bool enabled)
    {
        startDelayOnlyOnGameStart = enabled;
        Debug.Log($"Start delay only on game start {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Check if start delay is currently active
    /// </summary>
    public bool IsStartDelayActive()
    {
        return currentStartDelayCoroutine != null;
    }

    /// <summary>
    /// Cancel any ongoing start delay and play BGM immediately
    /// </summary>
    public void CancelStartDelay()
    {
        if (currentStartDelayCoroutine != null)
        {
            StopCoroutine(currentStartDelayCoroutine);
            currentStartDelayCoroutine = null;
            Debug.Log("Start delay cancelled, playing BGM immediately");
            PlaySceneBGM(currentSceneName);
        }
    }

    #endregion

    #region Public Static Methods for Easy Access

    /// <summary>
    /// Static method to play BGM for current scene
    /// </summary>
    public static void PlayBGM()
    {
        Instance.PlayCurrentSceneBGM();
    }

    /// <summary>
    /// Static method to stop BGM
    /// </summary>
    public static void StopBGM()
    {
        Instance.StopCurrentBGM();
    }

    /// <summary>
    /// Static method to pause BGM
    /// </summary>
    public static void PauseBGM()
    {
        Instance.PauseCurrentBGM();
    }

    /// <summary>
    /// Static method to resume BGM
    /// </summary>
    public static void ResumeBGM()
    {
        Instance.ResumeCurrentBGM();
    }

    /// <summary>
    /// Static method to set BGM volume
    /// </summary>
    public static void SetVolume(float volume)
    {
        Instance.SetBGMVolume(volume);
    }

    /// <summary>
    /// Static method to enable/disable CrossFade
    /// </summary>
    public static void EnableCrossFade(bool enabled)
    {
        Instance.SetCrossFadeEnabled(enabled);
    }

    /// <summary>
    /// Static method to set CrossFade duration
    /// </summary>
    public static void SetCrossFadeTime(float duration)
    {
        Instance.SetCrossFadeDuration(duration);
    }

    /// <summary>
    /// Static method to check if CrossFade is active
    /// </summary>
    public static bool IsCurrentlyCrossFading()
    {
        return Instance.IsCrossFading();
    }

    /// <summary>
    /// Static method to enable/disable fade-in from zero
    /// </summary>
    public static void EnableFadeInFromZero(bool enabled)
    {
        Instance.SetAlwaysFadeInFromZero(enabled);
    }

    /// <summary>
    /// Static method to enable/disable fade-in on loop restart
    /// </summary>
    public static void EnableFadeInOnLoop(bool enabled)
    {
        Instance.SetFadeInOnLoopRestart(enabled);
    }

    /// <summary>
    /// Static method to set fade-in duration
    /// </summary>
    public static void SetFadeInTime(float duration)
    {
        Instance.SetDefaultFadeInDuration(duration);
    }

    /// <summary>
    /// Static method to check if fade-in is active
    /// </summary>
    public static bool IsCurrentlyFadingIn()
    {
        return Instance.IsFadingIn();
    }

    /// <summary>
    /// Static method to manually trigger fade-in
    /// </summary>
    public static void FadeIn()
    {
        Instance.TriggerFadeIn();
    }

    /// <summary>
    /// Static method to enable/disable start delay
    /// </summary>
    public static void EnableStartDelay(bool enabled)
    {
        Instance.SetStartDelayEnabled(enabled);
    }

    /// <summary>
    /// Static method to set start delay duration
    /// </summary>
    public static void SetStartDelayTime(float duration)
    {
        Instance.SetStartDelayDuration(duration);
    }

    /// <summary>
    /// Static method to enable/disable start delay only on game start
    /// </summary>
    public static void EnableStartDelayOnlyOnGameStart(bool enabled)
    {
        Instance.SetStartDelayOnlyOnGameStart(enabled);
    }

    /// <summary>
    /// Static method to check if start delay is active
    /// </summary>
    public static bool IsCurrentlyDelayingStart()
    {
        return Instance.IsStartDelayActive();
    }

    /// <summary>
    /// Static method to cancel start delay and play immediately
    /// </summary>
    public static void CancelDelayAndPlay()
    {
        Instance.CancelStartDelay();
    }

    #endregion
}