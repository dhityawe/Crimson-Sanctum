using UnityEngine;
using System.Collections;
using Hellmade.Sound;

namespace CrimsonSanctum.Audio
{
    /// <summary>
    /// Responsible for handling crossfade transitions between audio tracks.
    /// Provides smooth transitions for BGM and other audio elements.
    /// </summary>
    public class CrossFadeHandler : MonoBehaviour
    {
        private AudioConfig config;
        private bool isCrossFading = false;
        private Coroutine currentCrossFadeCoroutine;

        public bool IsCrossFading => isCrossFading;

        public void Initialize(AudioConfig audioConfig)
        {
            config = audioConfig;
            
            // Subscribe to events
            AudioEvents.OnCrossFadeToggled += SetCrossFadeEnabled;
            AudioEvents.OnCrossFadeDurationChanged += SetCrossFadeDuration;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            AudioEvents.OnCrossFadeToggled -= SetCrossFadeEnabled;
            AudioEvents.OnCrossFadeDurationChanged -= SetCrossFadeDuration;
            
            // Stop any ongoing crossfade
            StopCrossFade();
        }

        /// <summary>
        /// Crossfade from current BGM to a new BGM track using BGMManager
        /// </summary>
        public void CrossFadeToBGM(BGMManager.BGMTrack newTrack, int currentMusicID)
        {
            if (!config.enableCrossFade)
            {
                DebugLog("CrossFade is disabled, skipping crossfade");
                return;
            }

            if (newTrack == null || newTrack.audioClip == null)
            {
                DebugLog("Cannot crossfade to BGM track: track or audio clip is null", LogType.Warning);
                return;
            }

            if (isCrossFading)
            {
                StopCrossFade();
            }

            currentCrossFadeCoroutine = StartCoroutine(CrossFadeBGMCoroutine(newTrack, currentMusicID));
        }

        /// <summary>
        /// Crossfade between two audio IDs (EazySoundManager)
        /// </summary>
        public void CrossFadeAudio(int fromAudioID, int toAudioID, float duration = -1f)
        {
            if (!config.enableCrossFade)
            {
                DebugLog("CrossFade is disabled, skipping crossfade");
                return;
            }

            if (duration < 0)
                duration = config.crossFadeDuration;

            if (isCrossFading)
            {
                StopCrossFade();
            }

            currentCrossFadeCoroutine = StartCoroutine(CrossFadeAudioCoroutine(fromAudioID, toAudioID, duration));
        }

        /// <summary>
        /// Crossfade between AudioSources
        /// </summary>
        public void CrossFadeAudioSources(AudioSource fromSource, AudioSource toSource, float duration = -1f)
        {
            if (!config.enableCrossFade)
            {
                DebugLog("CrossFade is disabled, skipping crossfade");
                return;
            }

            if (duration < 0)
                duration = config.crossFadeDuration;

            if (isCrossFading)
            {
                StopCrossFade();
            }

            currentCrossFadeCoroutine = StartCoroutine(CrossFadeAudioSourcesCoroutine(fromSource, toSource, duration));
        }

        private IEnumerator CrossFadeBGMCoroutine(BGMManager.BGMTrack newTrack, int previousMusicID)
        {
            isCrossFading = true;
            
            // Start playing the new track at volume 0
            int newMusicID = EazySoundManager.PlayMusic(
                newTrack.audioClip,
                0f,
                newTrack.loop,
                config.persistAcrossScenes,
                0f,
                newTrack.fadeOutSeconds
            );

            DebugLog($"CrossFading BGM to: {newTrack.trackName}");

            // Get references to both audio objects
            Hellmade.Sound.Audio previousAudio = EazySoundManager.GetMusicAudio(previousMusicID);
            Hellmade.Sound.Audio newAudio = EazySoundManager.GetMusicAudio(newMusicID);

            if (newAudio == null)
            {
                DebugLog("Failed to create new audio for crossfade", LogType.Error);
                isCrossFading = false;
                yield break;
            }

            float elapsedTime = 0f;
            float previousStartVolume = previousAudio?.Volume ?? 0f;
            float newTargetVolume = newTrack.volume;

            // Perform the crossfade
            while (elapsedTime < config.crossFadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float fadeProgress = elapsedTime / config.crossFadeDuration;

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
            isCrossFading = false;
            currentCrossFadeCoroutine = null;

            DebugLog($"CrossFade completed to: {newTrack.trackName}");
        }

        private IEnumerator CrossFadeAudioCoroutine(int fromAudioID, int toAudioID, float duration)
        {
            isCrossFading = true;

            Hellmade.Sound.Audio fromAudio = EazySoundManager.GetAudio(fromAudioID);
            Hellmade.Sound.Audio toAudio = EazySoundManager.GetAudio(toAudioID);

            if (fromAudio == null || toAudio == null)
            {
                DebugLog("Failed to get audio references for crossfade", LogType.Error);
                isCrossFading = false;
                yield break;
            }

            float elapsedTime = 0f;
            float fromStartVolume = fromAudio.Volume;
            float toTargetVolume = toAudio.Volume;

            // Start 'to' audio at 0 volume
            toAudio.SetVolume(0f, 0f);

            DebugLog($"CrossFading between audio IDs: {fromAudioID} -> {toAudioID}");

            // Perform the crossfade
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float fadeProgress = elapsedTime / duration;

                // Fade out the 'from' audio
                if (fromAudio != null)
                {
                    float fromVolume = Mathf.Lerp(fromStartVolume, 0f, fadeProgress);
                    fromAudio.SetVolume(fromVolume, 0f);
                }

                // Fade in the 'to' audio
                if (toAudio != null)
                {
                    float toVolume = Mathf.Lerp(0f, toTargetVolume, fadeProgress);
                    toAudio.SetVolume(toVolume, 0f);
                }

                yield return null;
            }

            // Ensure final volumes are set correctly
            if (fromAudio != null)
            {
                fromAudio.Stop();
            }
            
            if (toAudio != null)
            {
                toAudio.SetVolume(toTargetVolume, 0f);
            }

            // Clean up
            isCrossFading = false;
            currentCrossFadeCoroutine = null;

            DebugLog("CrossFade completed between audio IDs");
        }

        private IEnumerator CrossFadeAudioSourcesCoroutine(AudioSource fromSource, AudioSource toSource, float duration)
        {
            isCrossFading = true;

            if (fromSource == null || toSource == null)
            {
                DebugLog("Failed to get AudioSource references for crossfade", LogType.Error);
                isCrossFading = false;
                yield break;
            }

            float elapsedTime = 0f;
            float fromStartVolume = fromSource.volume;
            float toTargetVolume = toSource.volume;

            // Start 'to' source at 0 volume and play
            toSource.volume = 0f;
            if (!toSource.isPlaying)
            {
                toSource.Play();
            }

            DebugLog($"CrossFading between AudioSources: {fromSource.name} -> {toSource.name}");

            // Perform the crossfade
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float fadeProgress = elapsedTime / duration;

                // Fade out the 'from' source
                if (fromSource != null)
                {
                    fromSource.volume = Mathf.Lerp(fromStartVolume, 0f, fadeProgress);
                }

                // Fade in the 'to' source
                if (toSource != null)
                {
                    toSource.volume = Mathf.Lerp(0f, toTargetVolume, fadeProgress);
                }

                yield return null;
            }

            // Ensure final volumes are set correctly
            if (fromSource != null)
            {
                fromSource.Stop();
                fromSource.volume = fromStartVolume; // Restore original volume
            }
            
            if (toSource != null)
            {
                toSource.volume = toTargetVolume;
            }

            // Clean up
            isCrossFading = false;
            currentCrossFadeCoroutine = null;

            DebugLog("CrossFade completed between AudioSources");
        }

        /// <summary>
        /// Stop any ongoing crossfade operation
        /// </summary>
        public void StopCrossFade()
        {
            if (isCrossFading && currentCrossFadeCoroutine != null)
            {
                StopCoroutine(currentCrossFadeCoroutine);
                currentCrossFadeCoroutine = null;
                isCrossFading = false;
                
                DebugLog("Forced stop of ongoing CrossFade operation");
            }
        }

        /// <summary>
        /// Enable or disable CrossFade functionality
        /// </summary>
        public void SetCrossFadeEnabled(bool enabled)
        {
            if (config != null)
            {
                config.enableCrossFade = enabled;
                DebugLog($"CrossFade enabled: {enabled}");
            }
        }

        /// <summary>
        /// Set the CrossFade duration
        /// </summary>
        public void SetCrossFadeDuration(float duration)
        {
            if (config != null)
            {
                config.crossFadeDuration = Mathf.Clamp(duration, 0.1f, 10f);
                DebugLog($"CrossFade duration set to: {config.crossFadeDuration}");
            }
        }

        /// <summary>
        /// Check if a crossfade can be performed based on current settings
        /// </summary>
        public bool CanCrossFade()
        {
            return config != null && config.enableCrossFade && !isCrossFading;
        }

        /// <summary>
        /// Get the remaining time for the current crossfade (if any)
        /// </summary>
        public float GetRemainingCrossFadeTime()
        {
            // This would require tracking elapsed time in the coroutines
            // For now, return the full duration if crossfading, 0 if not
            return isCrossFading ? config.crossFadeDuration : 0f;
        }

        private void DebugLog(string message, LogType logType = LogType.Log)
        {
            if (config != null && config.enableDebugLogs)
            {
                switch (logType)
                {
                    case LogType.Warning:
                        Debug.LogWarning($"[CrossFadeHandler] {message}");
                        break;
                    case LogType.Error:
                        Debug.LogError($"[CrossFadeHandler] {message}");
                        break;
                    default:
                        Debug.Log($"[CrossFadeHandler] {message}");
                        break;
                }
            }
        }
    }
}