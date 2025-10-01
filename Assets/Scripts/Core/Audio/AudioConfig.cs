using UnityEngine;

namespace CrimsonSanctum.Audio
{
    /// <summary>
    /// ScriptableObject configuration for all audio settings.
    /// Centralizes audio configuration and makes it easily tweakable.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Crimson Sanctum/Audio/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Global Volume Settings")]
        [Range(0f, 1f)]
        public float globalMusicVolume = 1f;
        [Range(0f, 1f)]
        public float globalSFXVolume = 1f;
        
        [Header("Persistence Settings")]
        public bool persistAcrossScenes = true;
        
        [Header("CrossFade Settings")]
        public bool enableCrossFade = true;
        [Range(0.1f, 10f)]
        public float crossFadeDuration = 2f;
        public bool crossFadeOnSceneChange = true;
        public bool crossFadeOnTrackChange = true;
        
        [Header("Fade-In Settings")]
        public bool alwaysFadeInFromZero = true;
        public bool fadeInOnLoopRestart = true;
        [Range(0.1f, 10f)]
        public float defaultFadeInDuration = 1.5f;
        
        [Header("Start Delay Settings")]
        public bool enableStartDelay = true;
        [Range(0f, 10f)]
        public float startDelayDuration = 2f;
        public bool startDelayOnlyOnGameStart = true;
        
        [Header("SFX Settings")]
        [Range(1, 32)]
        public int maxConcurrentSFX = 16;
        public bool enableSFXPooling = true;
        [Range(0.1f, 5f)]
        public float sfxCleanupInterval = 2f;
        [Tooltip("Stop any currently playing instances of the same SFX before playing a new one (prevents overlap)")]
        public bool preventSFXOverlap = true;
        
        [Header("Debug Settings")]
        public bool enableDebugLogs = true;
    }
}