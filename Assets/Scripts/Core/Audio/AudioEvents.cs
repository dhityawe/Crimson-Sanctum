using UnityEngine;

namespace CrimsonSanctum.Audio
{
    /// <summary>
    /// Audio event system for decoupled communication between audio components.
    /// </summary>
    public static class AudioEvents
    {
        // BGM Events
        public static System.Action<string> OnSceneBGMRequested;
        public static System.Action<string> OnBGMTrackChanged;
        public static System.Action OnBGMStopped;
        public static System.Action OnBGMPaused;
        public static System.Action OnBGMResumed;
        
        // SFX Events
        public static System.Action<AudioClip, float, Vector3> OnSFXRequested;
        public static System.Action<string> OnSFXStopped;
        public static System.Action OnAllSFXStopped;
        
        // Volume Events
        public static System.Action<float> OnMusicVolumeChanged;
        public static System.Action<float> OnSFXVolumeChanged;
        
        // Fade Events
        public static System.Action<bool> OnCrossFadeToggled;
        public static System.Action<float> OnCrossFadeDurationChanged;
        
        // Scene Events
        public static System.Action<string> OnSceneAudioInitialized;
        
        /// <summary>
        /// Clear all event subscriptions (useful for cleanup).
        /// </summary>
        public static void ClearAllEvents()
        {
            OnSceneBGMRequested = null;
            OnBGMTrackChanged = null;
            OnBGMStopped = null;
            OnBGMPaused = null;
            OnBGMResumed = null;
            
            OnSFXRequested = null;
            OnSFXStopped = null;
            OnAllSFXStopped = null;
            
            OnMusicVolumeChanged = null;
            OnSFXVolumeChanged = null;
            
            OnCrossFadeToggled = null;
            OnCrossFadeDurationChanged = null;
            
            OnSceneAudioInitialized = null;
        }
    }
}