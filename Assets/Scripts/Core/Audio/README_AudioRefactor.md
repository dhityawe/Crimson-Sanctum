# Audio System Refactoring Guide

## 🎯 **Overview**

The original 1100+ line `AudioManager` has been refactored into a **modular, SRP-compliant architecture**:

```
AudioManager (Coordinator - 350 lines)
├── BGMManager (Background Music - 350 lines)
├── SFXManager (Sound Effects - 500 lines) 
├── CrossFadeHandler (Transitions - 300 lines)
├── AudioConfig (ScriptableObject Settings)
└── AudioEvents (Event System)
```

## 🔧 **Setup Instructions**

### 1. Create AudioConfig ScriptableObject
```csharp
// Right-click in Project → Create → Crimson Sanctum → Audio → Audio Config
// Assign this to your AudioManager's audioConfig field
```

### 2. Replace Old AudioManager
- Rename old `AudioManager.cs` to `AudioManagerOLD.cs`
- Add new `AudioManagerRefactored.cs` to your scene
- Assign the AudioConfig ScriptableObject

### 3. Migration Script (Optional)
```csharp
// Copy BGM configuration from old AudioManager to new BGMManager
// SFX clips can be configured in the new SFXManager
```

## 📋 **API Migration Guide**

### BGM Controls
```csharp
// OLD
AudioManager.PlayBGM();
AudioManager.StopBGM();
AudioManager.SetVolume(0.5f);

// NEW - Same static API works!
AudioManager.PlayBGM();
AudioManager.StopBGMStatic();
AudioManager.SetMusicVolumeStatic(0.5f);

// NEW - Instance API (recommended)
AudioManager.Instance.PlayCurrentSceneBGM();
AudioManager.Instance.StopBGM();
AudioManager.Instance.SetMusicVolume(0.5f);
```

### SFX Controls (NEW!)
```csharp
// Play 2D SFX
int sfxID = AudioManager.Instance.PlaySFX("jumpSound");
int sfxID = AudioManager.Instance.PlaySFX(audioClip);

// Play 3D positioned SFX
AudioManager.Instance.PlaySFX3D("explosionSound", transform.position);

// Stop specific SFX
AudioManager.Instance.StopSFX("jumpSound");
AudioManager.Instance.StopAllSFX();

// Static API also available
AudioManager.PlaySFXStatic("jumpSound");
AudioManager.PlaySFX3DStatic("explosion", playerPos);
```

### Volume Controls
```csharp
// Separate music and SFX volumes
AudioManager.Instance.SetMusicVolume(0.7f);
AudioManager.Instance.SetSFXVolume(0.8f);

float musicVol = AudioManager.Instance.GetMusicVolume();
float sfxVol = AudioManager.Instance.GetSFXVolume();
```

### CrossFade Controls
```csharp
// Enable/disable crossfade
AudioManager.Instance.SetCrossFadeEnabled(true);
AudioManager.Instance.SetCrossFadeDuration(3.0f);

// Check status
bool isCrossFading = AudioManager.Instance.IsCrossFading();
AudioManager.Instance.StopCrossFade();
```

## 🎵 **BGM Configuration**

### In BGMManager Inspector:
```csharp
[System.Serializable]
public class BGMTrack
{
    public string trackName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = true;
    [Range(0f, 5f)] public float fadeInSeconds = 1f;
    [Range(0f, 5f)] public float fadeOutSeconds = 1f;
}

[System.Serializable]
public class SceneBGM
{
    public string sceneName;
    public BGMTrack[] bgmTracks;
    public bool randomizePlayback = false;
    public bool shuffleOnSceneLoad = false;
}
```

## 🔊 **SFX Configuration**

### In SFXManager Inspector:
```csharp
[System.Serializable]
public class SFXClip
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool randomizePitch = false;
    [Range(0f, 0.5f)] public float pitchVariation = 0.1f;
    public bool is3D = false;
    [Range(0f, 500f)] public float maxDistance = 50f;
}
```

## ⚙️ **AudioConfig Settings**

Create and configure AudioConfig ScriptableObject:

```csharp
[CreateAssetMenu(fileName = "AudioConfig", menuName = "Crimson Sanctum/Audio/Audio Config")]
public class AudioConfig : ScriptableObject
{
    [Header("Global Volume Settings")]
    public float globalMusicVolume = 1f;
    public float globalSFXVolume = 1f;
    
    [Header("CrossFade Settings")]
    public bool enableCrossFade = true;
    public float crossFadeDuration = 2f;
    
    [Header("SFX Settings")]
    public int maxConcurrentSFX = 16;
    public bool enableSFXPooling = true;
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
}
```

## 🎮 **Usage Examples**

### Basic Game Audio
```csharp
public class PlayerController : MonoBehaviour
{
    void Jump()
    {
        // Play jump SFX
        AudioManager.Instance.PlaySFX("jumpSound");
    }
    
    void TakeDamage()
    {
        // Play 3D damage sound at player position
        AudioManager.Instance.PlaySFX3D("damageSound", transform.position);
    }
}

public class GameManager : MonoBehaviour
{
    void StartLevel()
    {
        // Play BGM for current scene
        AudioManager.Instance.PlayCurrentSceneBGM();
    }
    
    void PauseGame()
    {
        AudioManager.Instance.PauseBGM();
    }
    
    void ResumeGame()
    {
        AudioManager.Instance.ResumeBGM();
    }
}
```

### Settings Menu
```csharp
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    
    void Start()
    {
        musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
        
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }
    
    void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }
    
    void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
        // Play test SFX
        AudioManager.Instance.PlaySFX("buttonClick");
    }
}
```

## 🔄 **Event System Usage**

For advanced users who want to use the event system directly:

```csharp
// Subscribe to audio events
AudioEvents.OnBGMTrackChanged += OnBGMChanged;
AudioEvents.OnSFXRequested += OnSFXRequested;

// Trigger events
AudioEvents.OnSceneBGMRequested?.Invoke("MainMenu");
AudioEvents.OnMusicVolumeChanged?.Invoke(0.5f);

void OnBGMChanged(string trackName)
{
    Debug.Log($"Now playing: {trackName}");
}
```

## 🚀 **Benefits of New Architecture**

### **Single Responsibility Principle**
- ✅ **BGMManager**: Only handles background music
- ✅ **SFXManager**: Only handles sound effects  
- ✅ **CrossFadeHandler**: Only handles transitions
- ✅ **AudioManager**: Only coordinates subsystems

### **Improved Performance**
- ✅ **Object pooling** for AudioSources
- ✅ **Automatic cleanup** of finished sounds
- ✅ **Concurrent SFX limiting** prevents audio overflow
- ✅ **Event-driven** communication reduces coupling

### **Better Maintainability**
- ✅ **350 lines vs 1100+** in main manager
- ✅ **Modular components** easy to test and modify
- ✅ **ScriptableObject config** for designer-friendly settings
- ✅ **Clear separation** of concerns

### **Enhanced Features**
- ✅ **Dedicated SFX system** with AudioSource support
- ✅ **3D positioned audio** for immersive sound
- ✅ **Pitch randomization** for variety
- ✅ **Volume per-clip** control
- ✅ **Named SFX tracking** for stopping specific sounds

## ⚠️ **Migration Notes**

1. **Backup your project** before switching
2. **Test all audio functionality** after migration
3. **Update any custom scripts** that directly accessed old AudioManager internals
4. **Configure SFX clips** in the new SFXManager
5. **Assign AudioConfig** ScriptableObject to AudioManager

## 🐛 **Troubleshooting**

### Common Issues:

**No BGM playing:**
- Check if AudioConfig is assigned
- Verify BGM tracks are configured in BGMManager
- Check scene name matches BGM configuration

**SFX not working:**
- Ensure SFX clips are configured in SFXManager
- Check if AudioSource prefab is assigned (optional)
- Verify maxConcurrentSFX isn't set too low

**Volume issues:**
- Check both global and individual clip volumes
- Ensure EazySoundManager volumes are set correctly
- Verify AudioConfig volume settings

The new system provides **much better architecture**, **improved performance**, and **enhanced features** while maintaining **backward compatibility** for most use cases!