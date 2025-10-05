using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Hellmade.Sound;

namespace CrimsonSanctum.Audio
{
    /// <summary>
    /// Responsible for Sound Effects management.
    /// Handles 2D/3D sound effects, object pooling, and AudioSource management.
    /// </summary>
    public class SFXManager : MonoBehaviour
    {
        [System.Serializable]
        public class SFXClip
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)]
            public float volume = 1f;
            [Range(0.1f, 3f)]
            public float pitch = 1f;
            public bool randomizePitch = false;
            [Range(0f, 0.5f)]
            public float pitchVariation = 0.1f;
            public bool loop = false;
            public bool is3D = false;
            [Range(0f, 500f)]
            public float maxDistance = 50f;
        }

        [Header("SFX Configuration")]
        [SerializeField] private SFXClip[] sfxClips;

        [Header("AudioSource Settings")]
        [SerializeField] private AudioSource sfxAudioSourcePrefab;
        [SerializeField] private Transform audioSourceParent;

        private AudioConfig config;
        private Dictionary<string, SFXClip> sfxContainer;
        private Queue<AudioSource> audioSourcePool;
        private List<AudioSource> activeAudioSources;
        private Dictionary<string, List<int>> namedSFXTracking;
        
        private Coroutine cleanupCoroutine;

        public int ActiveSFXCount => activeAudioSources.Count;
        public int PooledSourcesCount => audioSourcePool.Count;

        public void Initialize(AudioConfig audioConfig)
        {
            config = audioConfig;
            InitializeSFXContainer();
            InitializeAudioSourcePool();
            
            // Set global SFX volume
            EazySoundManager.GlobalSoundsVolume = config.globalSFXVolume;
            
            // Subscribe to events
            AudioEvents.OnSFXRequested += HandleSFXRequest;
            AudioEvents.OnSFXStopped += StopSFX;
            AudioEvents.OnAllSFXStopped += StopAllSFX;
            AudioEvents.OnSFXVolumeChanged += SetSFXVolume;
            
            // Start cleanup coroutine
            if (config.enableSFXPooling)
            {
                cleanupCoroutine = StartCoroutine(CleanupInactiveSources());
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            AudioEvents.OnSFXRequested -= HandleSFXRequest;
            AudioEvents.OnSFXStopped -= StopSFX;
            AudioEvents.OnAllSFXStopped -= StopAllSFX;
            AudioEvents.OnSFXVolumeChanged -= SetSFXVolume;
            
            if (cleanupCoroutine != null)
            {
                StopCoroutine(cleanupCoroutine);
            }
        }

        private void InitializeSFXContainer()
        {
            sfxContainer = new Dictionary<string, SFXClip>();
            namedSFXTracking = new Dictionary<string, List<int>>();
            
            foreach (SFXClip sfxClip in sfxClips)
            {
                if (!string.IsNullOrEmpty(sfxClip.name) && sfxClip.clip != null)
                {
                    sfxContainer[sfxClip.name] = sfxClip;
                }
            }
        }

        private void InitializeAudioSourcePool()
        {
            audioSourcePool = new Queue<AudioSource>();
            activeAudioSources = new List<AudioSource>();
            
            // Create parent if not assigned
            if (audioSourceParent == null)
            {
                GameObject parent = new GameObject("SFX AudioSources");
                parent.transform.SetParent(transform);
                audioSourceParent = parent.transform;
            }
            
            // Pre-populate pool
            for (int i = 0; i < config.maxConcurrentSFX / 2; i++)
            {
                CreatePooledAudioSource();
            }
        }

        private AudioSource CreatePooledAudioSource()
        {
            AudioSource newSource;
            
            if (sfxAudioSourcePrefab != null)
            {
                newSource = Instantiate(sfxAudioSourcePrefab, audioSourceParent);
            }
            else
            {
                GameObject sourceGO = new GameObject("SFX AudioSource");
                sourceGO.transform.SetParent(audioSourceParent);
                newSource = sourceGO.AddComponent<AudioSource>();
                
                // Set default 2D settings
                newSource.playOnAwake = false;
                newSource.spatialBlend = 0f; // 2D by default
            }
            
            newSource.gameObject.SetActive(false);
            audioSourcePool.Enqueue(newSource);
            
            return newSource;
        }

        private AudioSource GetPooledAudioSource()
        {
            AudioSource source = null;
            
            // Try to get from pool
            if (audioSourcePool.Count > 0)
            {
                source = audioSourcePool.Dequeue();
            }
            // Create new if pool is empty and under limit
            else if (activeAudioSources.Count < config.maxConcurrentSFX)
            {
                source = CreatePooledAudioSource();
                if (audioSourcePool.Count > 0)
                {
                    source = audioSourcePool.Dequeue();
                }
            }
            // Steal oldest active source if at limit
            else if (activeAudioSources.Count > 0)
            {
                source = activeAudioSources[0];
                activeAudioSources.RemoveAt(0);
                source.Stop();
            }
            
            if (source != null)
            {
                source.gameObject.SetActive(true);
                activeAudioSources.Add(source);
            }
            
            return source;
        }

        private void ReturnAudioSourceToPool(AudioSource source)
        {
            if (source == null) return;
            
            activeAudioSources.Remove(source);
            source.clip = null;
            source.gameObject.SetActive(false);
            
            if (config.enableSFXPooling)
            {
                audioSourcePool.Enqueue(source);
            }
            else
            {
                Destroy(source.gameObject);
            }
        }

        public int PlaySFX(string sfxName, float volumeMultiplier = 1f, Vector3 position = default)
        {
            if (!sfxContainer.ContainsKey(sfxName))
            {
                return -1;
            }

            // Stop any existing instances of this named SFX if overlap prevention is enabled
            if (config.preventSFXOverlap)
            {
                StopSFX(sfxName);
            }

            SFXClip sfxClip = sfxContainer[sfxName];
            return PlaySFX(sfxClip, volumeMultiplier, position);
        }

        public int PlaySFX(AudioClip clip, float volumeMultiplier = 1f, Vector3 position = default, bool loop = false)
        {
            if (clip == null)
            {
                return -1;
            }

            // Stop any existing instances of this audio clip if overlap prevention is enabled
            if (config.preventSFXOverlap)
            {
                StopSFXByClip(clip);
            }

            SFXClip tempClip = new SFXClip
            {
                name = clip.name,
                clip = clip,
                volume = 1f,
                pitch = 1f,
                loop = loop,
                is3D = position != default
            };

            return PlaySFX(tempClip, volumeMultiplier, position);
        }

        /// <summary>
        /// Play SFX with explicit overlap control (overrides config setting)
        /// </summary>
        public int PlaySFX(AudioClip clip, float volumeMultiplier, Vector3 position, bool loop, bool preventOverlap)
        {
            if (clip == null)
            {
                return -1;
            }

            // Stop any existing instances if explicitly requested
            if (preventOverlap)
            {
                StopSFXByClip(clip);
            }

            SFXClip tempClip = new SFXClip
            {
                name = clip.name,
                clip = clip,
                volume = 1f,
                pitch = 1f,
                loop = loop,
                is3D = position != default
            };

            return PlaySFX(tempClip, volumeMultiplier, position);
        }

        /// <summary>
        /// Play named SFX with explicit overlap control (overrides config setting)
        /// </summary>
        public int PlaySFX(string sfxName, float volumeMultiplier, Vector3 position, bool preventOverlap)
        {
            if (!sfxContainer.ContainsKey(sfxName))
            {
                return -1;
            }

            // Stop any existing instances if explicitly requested
            if (preventOverlap)
            {
                StopSFX(sfxName);
            }

            SFXClip sfxClip = sfxContainer[sfxName];
            return PlaySFX(sfxClip, volumeMultiplier, position);
        }

        private int PlaySFX(SFXClip sfxClip, float volumeMultiplier, Vector3 position)
        {
            // Use AudioSource method for better control
            if (config.enableSFXPooling)
            {
                return PlaySFXWithAudioSource(sfxClip, volumeMultiplier, position);
            }
            // Fallback to EazySoundManager
            else
            {
                return PlaySFXWithEazySound(sfxClip, volumeMultiplier, position);
            }
        }

        private int PlaySFXWithAudioSource(SFXClip sfxClip, float volumeMultiplier, Vector3 position)
        {
            AudioSource source = GetPooledAudioSource();
            if (source == null)
            {
                return -1;
            }

            // Configure AudioSource
            source.clip = sfxClip.clip;
            source.volume = sfxClip.volume * volumeMultiplier * config.globalSFXVolume;
            source.loop = sfxClip.loop; // Set loop from SFXClip
            
            // Handle pitch
            if (sfxClip.randomizePitch)
            {
                float pitchVariation = Random.Range(-sfxClip.pitchVariation, sfxClip.pitchVariation);
                source.pitch = sfxClip.pitch + pitchVariation;
            }
            else
            {
                source.pitch = sfxClip.pitch;
            }

            // Handle 3D positioning
            if (sfxClip.is3D && position != default)
            {
                source.transform.position = position;
                source.spatialBlend = 1f; // 3D
                source.maxDistance = sfxClip.maxDistance;
                source.rolloffMode = AudioRolloffMode.Linear;
            }
            else
            {
                source.spatialBlend = 0f; // 2D
            }

            // Play and setup cleanup
            source.Play();
            
            // Only setup auto-cleanup for non-looping sounds
            if (!sfxClip.loop)
            {
                StartCoroutine(ReturnSourceWhenFinished(source));
            }

            int audioID = source.GetInstanceID();
            
            // Track named SFX
            if (!string.IsNullOrEmpty(sfxClip.name))
            {
                if (!namedSFXTracking.ContainsKey(sfxClip.name))
                {
                    namedSFXTracking[sfxClip.name] = new List<int>();
                }
                namedSFXTracking[sfxClip.name].Add(audioID);
            }
            return audioID;
        }

        private int PlaySFXWithEazySound(SFXClip sfxClip, float volumeMultiplier, Vector3 position)
        {
            float volume = sfxClip.volume * volumeMultiplier;
            bool is3D = sfxClip.is3D && position != default;
            
            Transform sourceTransform = null;
            if (is3D)
            {
                // Create temporary transform for 3D positioning
                GameObject tempGO = new GameObject($"SFX_3D_{sfxClip.name}");
                tempGO.transform.position = position;
                sourceTransform = tempGO.transform;
                
                // Destroy after clip finishes
                Destroy(tempGO, sfxClip.clip.length + 1f);
            }

            int audioID = EazySoundManager.PlaySound(sfxClip.clip, volume, sfxClip.loop, sourceTransform);
            
            // Track named SFX
            if (!string.IsNullOrEmpty(sfxClip.name))
            {
                if (!namedSFXTracking.ContainsKey(sfxClip.name))
                {
                    namedSFXTracking[sfxClip.name] = new List<int>();
                }
                namedSFXTracking[sfxClip.name].Add(audioID);
            }
            return audioID;
        }

        private IEnumerator ReturnSourceWhenFinished(AudioSource source)
        {
            while (source != null && source.isPlaying)
            {
                yield return null;
            }
            
            if (source != null)
            {
                ReturnAudioSourceToPool(source);
            }
        }

        public void StopSFX(string sfxName)
        {
            if (!namedSFXTracking.ContainsKey(sfxName))
                return;

            List<int> audioIDs = namedSFXTracking[sfxName];
            
            for (int i = audioIDs.Count - 1; i >= 0; i--)
            {
                int audioID = audioIDs[i];
                
                // Try AudioSource first
                AudioSource source = activeAudioSources.Find(s => s.GetInstanceID() == audioID);
                if (source != null)
                {
                    source.Stop();
                    ReturnAudioSourceToPool(source);
                }
                else
                {
                    // Try EazySoundManager
                    Hellmade.Sound.Audio audio = EazySoundManager.GetSoundAudio(audioID);
                    if (audio != null)
                    {
                        audio.Stop();
                    }
                }
                
                audioIDs.RemoveAt(i);
            }
            
            if (audioIDs.Count == 0)
            {
                namedSFXTracking.Remove(sfxName);
            }
            
        }

        public void StopSFX(int audioID)
        {
            // Try AudioSource first
            AudioSource source = activeAudioSources.Find(s => s.GetInstanceID() == audioID);
            if (source != null)
            {
                source.Stop();
                ReturnAudioSourceToPool(source);
                return;
            }

            // Try EazySoundManager
            Hellmade.Sound.Audio audio = EazySoundManager.GetSoundAudio(audioID);
            if (audio != null)
            {
                audio.Stop();
                return;
            }

        }

        public void StopSFXByClip(AudioClip clip)
        {
            if (clip == null) return;

            // Stop all AudioSource instances playing this clip
            for (int i = activeAudioSources.Count - 1; i >= 0; i--)
            {
                AudioSource source = activeAudioSources[i];
                if (source != null && source.clip == clip)
                {
                    source.Stop();
                    ReturnAudioSourceToPool(source);
                }
            }

            // Stop all EazySoundManager instances playing this clip
            // Note: EazySoundManager doesn't provide an easy way to stop by clip
            // So we'll check all tracked named SFX that might use this clip
            var trackingKeys = new List<string>(namedSFXTracking.Keys);
            foreach (string sfxName in trackingKeys)
            {
                if (sfxContainer.ContainsKey(sfxName) && sfxContainer[sfxName].clip == clip)
                {
                    StopSFX(sfxName);
                }
            }

        }

        public void StopAllSFX()
        {
            // Stop all AudioSource-based SFX
            foreach (AudioSource source in activeAudioSources.ToArray())
            {
                if (source != null)
                {
                    source.Stop();
                    ReturnAudioSourceToPool(source);
                }
            }
            
            // Stop all EazySoundManager SFX
            EazySoundManager.StopAllSounds();
            
            // Clear tracking
            namedSFXTracking.Clear();
        }

        public void SetSFXVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            EazySoundManager.GlobalSoundsVolume = volume;
            
            // Update active AudioSources
            foreach (AudioSource source in activeAudioSources)
            {
                if (source != null)
                {
                    // This is a simplified approach; ideally you'd track original volumes
                    source.volume *= volume;
                }
            }
            
        }

        private IEnumerator CleanupInactiveSources()
        {
            while (true)
            {
                yield return new WaitForSeconds(config.sfxCleanupInterval);
                
                // Remove finished AudioSources
                for (int i = activeAudioSources.Count - 1; i >= 0; i--)
                {
                    AudioSource source = activeAudioSources[i];
                    if (source == null || !source.isPlaying)
                    {
                        if (source != null)
                        {
                            ReturnAudioSourceToPool(source);
                        }
                        else
                        {
                            activeAudioSources.RemoveAt(i);
                        }
                    }
                }
                
                // Clean up named tracking
                var keysToRemove = new List<string>();
                foreach (var kvp in namedSFXTracking)
                {
                    kvp.Value.RemoveAll(id => {
                        AudioSource source = activeAudioSources.Find(s => s.GetInstanceID() == id);
                        Hellmade.Sound.Audio audio = EazySoundManager.GetSoundAudio(id);
                        return source == null && audio == null;
                    });
                    
                    if (kvp.Value.Count == 0)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (string key in keysToRemove)
                {
                    namedSFXTracking.Remove(key);
                }
            }
        }

        public void AddSFXClip(SFXClip newClip)
        {
            if (!string.IsNullOrEmpty(newClip.name) && newClip.clip != null)
            {
                sfxContainer[newClip.name] = newClip;
            }
        }

        public bool HasSFX(string sfxName)
        {
            return sfxContainer.ContainsKey(sfxName);
        }

        public SFXClip GetSFXClip(string sfxName)
        {
            return sfxContainer.TryGetValue(sfxName, out SFXClip clip) ? clip : null;
        }

        // Event handler for AudioEvents.OnSFXRequested
        private void HandleSFXRequest(AudioClip clip, float volume, Vector3 position)
        {
            PlaySFX(clip, volume, position);
        }


    }
}