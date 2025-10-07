using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CrimsonSanctum.Audio;

public abstract class ObstacleBase : MonoBehaviour
{
    // 🎨 Shared Config — Designer can assign in Inspector
    public ObstacleData data;

    // 🔊 Audio Management — Persistent AudioSources attached to obstacle
    protected Dictionary<string, AudioSource> activeAudioSources = new Dictionary<string, AudioSource>();
    protected AudioManager audioManager;

    // 🧠 Lifecycle — Called automatically
    protected virtual void Start()
    {
        audioManager = AudioManager.Instance;
        Initialize(); // Force all obstacles to init
    }
    
    protected virtual void OnDestroy()
    {
        CleanupAudioSources();
    }

    // 💥 Collision — Shared player hit logic
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnPlayerHit(); // Each obstacle defines what "hit" means
        }
    }

    // 🧭 ABSTRACT METHODS — Each obstacle MUST implement these
    protected abstract void Initialize();
    protected abstract void OnPlayerHit();

    // 🎇 SHARED UTILITIES — Reusable by all obstacles
    protected void PlayHitEffect()
    {
        if (data?.hitSFX != null)
            // SoundManager.Instance?.Play(data.hitSFX);

        StartCoroutine(FlashColor(data?.hitFlashColor ?? Color.red, data?.hitFlashDuration ?? 0.2f));
    }

    protected IEnumerator FlashColor(Color color, float duration)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color original = sr.color;
            sr.color = color;
            yield return new WaitForSeconds(duration);
            sr.color = original;
        }
    }
    
    // 🔊 AUDIO UTILITIES — Persistent AudioSource management for obstacles
    
    /// <summary>
    /// Creates a persistent AudioSource attached to this obstacle.
    /// The AudioSource will remain active until explicitly stopped or obstacle is destroyed.
    /// </summary>
    /// <param name="sourceName">Unique identifier for this audio source</param>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="volume">Volume (0-1), will be multiplied by global SFX volume</param>
    /// <param name="loop">Whether the audio should loop</param>
    /// <param name="pitch">Pitch adjustment (default 1.0)</param>
    /// <param name="spatialBlend">0 = 2D, 1 = 3D (default 0)</param>
    /// <returns>The created AudioSource component</returns>
    protected AudioSource CreateObstacleAudioSource(string sourceName, AudioClip clip, float volume = 1f, bool loop = false, float pitch = 1f, float spatialBlend = 0f)
    {
        if (clip == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Attempted to create AudioSource '{sourceName}' with null clip");
            return null;
        }
        
        // Stop existing audio source with same name if it exists
        StopObstacleAudioSource(sourceName);
        
        // Create audio source as child object
        GameObject audioObject = new GameObject($"SFX_{sourceName}");
        audioObject.transform.SetParent(transform);
        audioObject.transform.localPosition = Vector3.zero;
        
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.volume = volume * GetSFXVolume();
        source.pitch = pitch;
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        
        // Track the audio source
        activeAudioSources[sourceName] = source;
        
        // Auto-cleanup for non-looping sounds
        if (!loop && clip.length > 0)
        {
            StartCoroutine(AutoCleanupAudioSource(sourceName, clip.length));
        }
        
        return source;
    }
    
    /// <summary>
    /// Stops and removes a specific AudioSource by name.
    /// </summary>
    protected void StopObstacleAudioSource(string sourceName, bool fadeOut = false, float fadeTime = 0.2f)
    {
        if (activeAudioSources.TryGetValue(sourceName, out AudioSource source))
        {
            if (source != null)
            {
                if (fadeOut && source.isPlaying)
                {
                    StartCoroutine(FadeOutAndStop(source, fadeTime));
                }
                else
                {
                    source.Stop();
                    Destroy(source.gameObject);
                }
            }
            
            activeAudioSources.Remove(sourceName);
        }
    }
    
    /// <summary>
    /// Stops all AudioSources attached to this obstacle.
    /// Call this in OnDestroy() or when deactivating the obstacle.
    /// </summary>
    protected void CleanupAudioSources()
    {
        foreach (var kvp in activeAudioSources)
        {
            if (kvp.Value != null)
            {
                kvp.Value.Stop();
                Destroy(kvp.Value.gameObject);
            }
        }
        
        activeAudioSources.Clear();
    }
    
    /// <summary>
    /// Gets the global SFX volume from AudioManager.
    /// </summary>
    protected float GetSFXVolume()
    {
        return audioManager?.GetSFXVolume() ?? 1f;
    }
    
    /// <summary>
    /// Checks if an AudioSource is currently playing.
    /// </summary>
    protected bool IsAudioSourcePlaying(string sourceName)
    {
        if (activeAudioSources.TryGetValue(sourceName, out AudioSource source))
        {
            return source != null && source.isPlaying;
        }
        return false;
    }
    
    /// <summary>
    /// Updates the volume of an active AudioSource (useful when global volume changes).
    /// </summary>
    protected void UpdateAudioSourceVolume(string sourceName, float baseVolume)
    {
        if (activeAudioSources.TryGetValue(sourceName, out AudioSource source))
        {
            if (source != null)
            {
                source.volume = baseVolume * GetSFXVolume();
            }
        }
    }
    
    // Auto-cleanup coroutine for one-shot sounds
    private IEnumerator AutoCleanupAudioSource(string sourceName, float duration)
    {
        yield return new WaitForSeconds(duration + 0.1f);
        
        if (activeAudioSources.TryGetValue(sourceName, out AudioSource source))
        {
            if (source != null && !source.isPlaying)
            {
                Destroy(source.gameObject);
                activeAudioSources.Remove(sourceName);
            }
        }
    }
    
    // Fade out coroutine
    private IEnumerator FadeOutAndStop(AudioSource source, float fadeTime)
    {
        float startVolume = source.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeTime && source != null)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
            yield return null;
        }
        
        if (source != null)
        {
            source.Stop();
            Destroy(source.gameObject);
        }
    }
}