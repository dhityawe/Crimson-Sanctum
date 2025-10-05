using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Hellmade.Sound;

namespace CrimsonSanctum.Audio
{
    /// <summary>
    /// Responsible for Background Music management.
    /// Handles scene-based BGM, track switching, and EazySoundManager integration.
    /// </summary>
    public class BGMManager : MonoBehaviour
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

        private AudioConfig config;
        private Dictionary<string, SceneBGM> sceneBGMContainer;
        
        // Current playing music info
        private int currentMusicID = -1;
        private string currentSceneName;
        private BGMTrack currentTrack;
        private int currentTrackIndex = 0;
        private Hellmade.Sound.Audio currentAudio; // Cache the current audio reference
        
        // Fade-in related variables
        private bool isFadingIn = false;
        private Coroutine currentFadeInCoroutine;
        
        // Start delay related variables
        private bool hasGameStarted = false;
        private Coroutine currentStartDelayCoroutine;

        public BGMTrack CurrentTrack => currentTrack;
        public bool IsPlaying => currentAudio?.IsPlaying == true;
        public bool IsPaused => currentAudio?.Paused == true;

        public void Initialize(AudioConfig audioConfig)
        {
            config = audioConfig;
            InitializeBGMContainer();
            
            // Set global music volume
            EazySoundManager.GlobalMusicVolume = config.globalMusicVolume;
            
            // Subscribe to events
            AudioEvents.OnSceneBGMRequested += PlaySceneBGM;
            AudioEvents.OnBGMStopped += StopCurrentBGM;
            AudioEvents.OnBGMPaused += PauseCurrentBGM;
            AudioEvents.OnBGMResumed += ResumeCurrentBGM;
            AudioEvents.OnMusicVolumeChanged += SetBGMVolume;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            AudioEvents.OnSceneBGMRequested -= PlaySceneBGM;
            AudioEvents.OnBGMStopped -= StopCurrentBGM;
            AudioEvents.OnBGMPaused -= PauseCurrentBGM;
            AudioEvents.OnBGMResumed -= ResumeCurrentBGM;
            AudioEvents.OnMusicVolumeChanged -= SetBGMVolume;
        }

        private void InitializeBGMContainer()
        {
            sceneBGMContainer = new Dictionary<string, SceneBGM>();
            
            foreach (SceneBGM sceneBGM in sceneBGMList)
            {
                if (!string.IsNullOrEmpty(sceneBGM.sceneName))
                {
                    sceneBGMContainer[sceneBGM.sceneName] = sceneBGM;
                }
            }
            
        }

        public void HandleSceneLoaded(string sceneName)
        {
            currentSceneName = sceneName;
            
            if (config.enableStartDelay && config.startDelayOnlyOnGameStart && hasGameStarted)
            {
                PlaySceneBGM(currentSceneName);
            }
            else if (config.enableStartDelay && !config.startDelayOnlyOnGameStart)
            {
                StartDelayedBGM(currentSceneName, false);
            }
            else
            {
                PlaySceneBGM(currentSceneName);
            }
            
            hasGameStarted = true;
        }

        public void StartInitialBGM(string sceneName)
        {
            currentSceneName = sceneName;
            
            if (config.enableStartDelay)
            {
                StartDelayedBGM(currentSceneName, true);
            }
            else
            {
                PlaySceneBGM(currentSceneName);
            }
        }

        public void PlaySceneBGM(string sceneName)
        {
            CancelStartDelay();

            if (!sceneBGMContainer.ContainsKey(sceneName))
            {
                return;
            }

            SceneBGM sceneBGM = sceneBGMContainer[sceneName];
            
            if (sceneBGM.bgmTracks == null || sceneBGM.bgmTracks.Length == 0)
            {
                Debug.Log("");
                return;
            }

            BGMTrack trackToPlay = SelectTrackToPlay(sceneBGM);
            PlayBGMTrack(trackToPlay);
        }

        private BGMTrack SelectTrackToPlay(SceneBGM sceneBGM)
        {
            if (sceneBGM.randomizePlayback)
            {
                currentTrackIndex = Random.Range(0, sceneBGM.bgmTracks.Length);
                return sceneBGM.bgmTracks[currentTrackIndex];
            }
            else
            {
                currentTrackIndex = 0;
                return sceneBGM.bgmTracks[currentTrackIndex];
            }
        }

        private void PlayBGMTrack(BGMTrack track)
        {
            // Stop current music if playing
            if (currentMusicID != -1)
            {
                StopCurrentBGM();
            }

            currentTrack = track;
            
            bool shouldFadeIn = config.alwaysFadeInFromZero;
            float fadeInDuration = shouldFadeIn ? (track.fadeInSeconds > 0 ? track.fadeInSeconds : config.defaultFadeInDuration) : 0f;
            float startVolume = shouldFadeIn ? 0f : track.volume;
            
            currentMusicID = EazySoundManager.PlayMusic(
                track.audioClip,
                startVolume,
                track.loop,
                config.persistAcrossScenes,
                0f,
                track.fadeOutSeconds
            );

            // Cache the audio reference for efficient access
            currentAudio = EazySoundManager.GetMusicAudio(currentMusicID);
            
            if (shouldFadeIn && fadeInDuration > 0f)
            {
                StartFadeIn(track.volume, fadeInDuration);
            }
            
            if (config.fadeInOnLoopRestart && track.loop)
            {
                StartCoroutine(MonitorLoopRestart());
            }

            AudioEvents.OnBGMTrackChanged?.Invoke(track.trackName);
        }

        private void StartFadeIn(float targetVolume, float fadeInDuration)
        {
            if (currentFadeInCoroutine != null)
            {
                StopCoroutine(currentFadeInCoroutine);
            }
            
            currentFadeInCoroutine = StartCoroutine(FadeInCoroutine(targetVolume, fadeInDuration));
        }
        
        private IEnumerator FadeInCoroutine(float targetVolume, float fadeInDuration)
        {
            isFadingIn = true;
            
            if (currentAudio == null)
            {
                isFadingIn = false;
                yield break;
            }
            
            float elapsedTime = 0f;
            float startVolume = 0f;
            
            currentAudio.SetVolume(startVolume, 0f);
            
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
            
            if (currentAudio != null)
            {
                currentAudio.SetVolume(targetVolume, 0f);
            }
            
            isFadingIn = false;
            currentFadeInCoroutine = null;
        }

        private IEnumerator MonitorLoopRestart()
        {
            if (currentAudio == null || currentTrack == null) yield break;
            
            float lastPlaybackTime = 0f;
            
            while (currentAudio != null && currentAudio.IsPlaying && currentTrack != null)
            {
                if (currentAudio.AudioSource != null)
                {
                    float currentTime = currentAudio.AudioSource.time;
                    
                    if (currentTime < lastPlaybackTime - 0.5f)
                    {                     
                        if (config.fadeInOnLoopRestart && !isFadingIn)
                        {
                            float fadeInDuration = currentTrack.fadeInSeconds > 0 ? currentTrack.fadeInSeconds : config.defaultFadeInDuration;
                            StartFadeIn(currentTrack.volume, fadeInDuration);
                        }
                    }
                    
                    lastPlaybackTime = currentTime;
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void StartDelayedBGM(string sceneName, bool isGameStart)
        {
            if (currentStartDelayCoroutine != null)
            {
                StopCoroutine(currentStartDelayCoroutine);
            }
            currentStartDelayCoroutine = StartCoroutine(StartDelayedBGMCoroutine(sceneName, isGameStart));
        }

        private IEnumerator StartDelayedBGMCoroutine(string sceneName, bool isGameStart)
        {
            yield return new WaitForSeconds(config.startDelayDuration);
            
            if (currentStartDelayCoroutine != null)
            {
                PlaySceneBGM(sceneName);
                currentStartDelayCoroutine = null;
            }
        }

        private void CancelStartDelay()
        {
            if (currentStartDelayCoroutine != null)
            {
                StopCoroutine(currentStartDelayCoroutine);
                currentStartDelayCoroutine = null;
            }
        }

        public void StopCurrentBGM()
        {
            CancelStartDelay();
            
            if (currentFadeInCoroutine != null)
            {
                StopCoroutine(currentFadeInCoroutine);
                currentFadeInCoroutine = null;
                isFadingIn = false;
            }
            
            if (currentMusicID != -1)
            {
                if (currentAudio != null)
                {
                    currentAudio.Stop();
                }
                currentMusicID = -1;
                currentTrack = null;
                currentAudio = null; // Clear the cached reference
            }
        }

        public void PauseCurrentBGM()
        {
            if (currentAudio != null)
            {
                currentAudio.Pause();
            }
        }

        public void ResumeCurrentBGM()
        {
            if (currentAudio != null)
            {
                currentAudio.Resume();
            }
        }

        public void SetBGMVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            
            if (currentAudio != null)
            {
                currentAudio.SetVolume(volume, 0f);
            }
        }

        public void PlayNextTrack()
        {
            if (!sceneBGMContainer.ContainsKey(currentSceneName))
                return;

            SceneBGM sceneBGM = sceneBGMContainer[currentSceneName];
            
            if (sceneBGM.bgmTracks == null || sceneBGM.bgmTracks.Length <= 1)
                return;

            currentTrackIndex = (currentTrackIndex + 1) % sceneBGM.bgmTracks.Length;
            BGMTrack nextTrack = sceneBGM.bgmTracks[currentTrackIndex];
            
            PlayBGMTrack(nextTrack);
        }

        public void PlayPreviousTrack()
        {
            if (!sceneBGMContainer.ContainsKey(currentSceneName))
                return;

            SceneBGM sceneBGM = sceneBGMContainer[currentSceneName];
            
            if (sceneBGM.bgmTracks == null || sceneBGM.bgmTracks.Length <= 1)
                return;

            currentTrackIndex = (currentTrackIndex - 1 + sceneBGM.bgmTracks.Length) % sceneBGM.bgmTracks.Length;
            BGMTrack previousTrack = sceneBGM.bgmTracks[currentTrackIndex];
            
            PlayBGMTrack(previousTrack);
        }

        public void AddBGMToScene(string sceneName, BGMTrack newTrack)
        {
            if (!sceneBGMContainer.ContainsKey(sceneName))
            {
                sceneBGMContainer[sceneName] = new SceneBGM { sceneName = sceneName, bgmTracks = new BGMTrack[] { newTrack } };
            }
            else
            {
                var existingBGM = sceneBGMContainer[sceneName];
                var newTracks = new BGMTrack[existingBGM.bgmTracks.Length + 1];
                existingBGM.bgmTracks.CopyTo(newTracks, 0);
                newTracks[newTracks.Length - 1] = newTrack;
                existingBGM.bgmTracks = newTracks;
            }
        }

        public bool HasBGMForScene(string sceneName)
        {
            return sceneBGMContainer.ContainsKey(sceneName) && 
                   sceneBGMContainer[sceneName].bgmTracks != null && 
                   sceneBGMContainer[sceneName].bgmTracks.Length > 0;
        }
    }
}