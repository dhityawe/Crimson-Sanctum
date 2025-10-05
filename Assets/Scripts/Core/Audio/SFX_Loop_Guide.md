# SFX Loop Functionality - Quick Reference

## Overview
The AudioManager now supports looped sound effects through dedicated methods and SFXClip configuration.

## How to Play Looped SFX

### Method 1: Direct AudioClip with Loop Parameter
```csharp
// Play a looped sound effect
int loopID = AudioManager.Instance.PlayLoopedSFX(audioClip, volumeMultiplier);

// Play a looped 3D sound effect
int loop3DID = AudioManager.Instance.PlayLoopedSFX3D(audioClip, position, volumeMultiplier);
```

### Method 2: SFXClip Configuration (Recommended)
1. Create or edit an SFXClip in the Inspector
2. Check the "Loop" checkbox
3. Play normally using PlaySFX - it will automatically loop

```csharp
// If SFXClip has loop = true, this will loop automatically
int sfxID = AudioManager.Instance.PlaySFX("ambientSound");
```

### Method 3: Static Methods (Convenience)
```csharp
// Static methods for global access
int loopID = AudioManager.PlayLoopedSFXStatic(audioClip, volume);
int loop3DID = AudioManager.PlayLoopedSFX3DStatic(audioClip, position, volume);
```

## How to Stop Looped SFX

### Stop by ID (Recommended for Loops)
```csharp
// Store the ID when starting the loop
int loopID = AudioManager.Instance.PlayLoopedSFX(audioClip, 0.5f);

// Stop the specific loop later
AudioManager.Instance.StopSFX(loopID);

// Or using static method
AudioManager.StopSFXStatic(loopID);
```

### Stop by Name (Stops All Instances)
```csharp
// Stops all instances of this named SFX
AudioManager.Instance.StopSFX("ambientSound");
```

## Configuration in Inspector

### SFXClip Settings
- **Loop**: Check this box to make the sound loop automatically
- **Volume**: Base volume for the sound
- **Pitch**: Pitch adjustment
- **Is3D**: Enable 3D positioning
- **Max Distance**: Maximum hearing distance for 3D sounds

### AudioConfig Settings
- **SFX Pool Size**: Number of AudioSources available for SFX
- **Global SFX Volume**: Master volume control for all SFX

## Best Practices

### 1. Track Loop IDs
```csharp
public class MyScript : MonoBehaviour
{
    private int ambientLoopID = -1;
    
    void StartAmbient()
    {
        if (ambientLoopID == -1)
        {
            ambientLoopID = AudioManager.Instance.PlayLoopedSFX(ambientClip, 0.5f);
        }
    }
    
    void StopAmbient()
    {
        if (ambientLoopID != -1)
        {
            AudioManager.Instance.StopSFX(ambientLoopID);
            ambientLoopID = -1;
        }
    }
}
```

### 2. Clean Up on Disable
```csharp
void OnDisable()
{
    if (ambientLoopID != -1)
    {
        AudioManager.Instance.StopSFX(ambientLoopID);
        ambientLoopID = -1;
    }
}
```

### 3. Use 3D Loops for Positional Audio
```csharp
// For environmental sounds tied to specific locations
Vector3 waterfallPosition = waterfallObject.transform.position;
int waterfallID = AudioManager.Instance.PlayLoopedSFX3D(waterfallSound, waterfallPosition, 0.8f);
```

## Common Use Cases

### Ambient Environment Sounds
- Background music layers
- Environmental loops (wind, water, etc.)
- Crowd noise

### Gameplay Loops
- Engine sounds (vehicles)
- Machinery sounds
- Continuous spell effects
- Heartbeat/breathing sounds

### UI Loops
- Loading sounds
- Alarm/warning sounds
- Continuous feedback sounds

## Performance Notes

- **AudioSource Pooling**: Looped sounds use the AudioSource pool efficiently
- **Automatic Cleanup**: Non-looped sounds clean up automatically
- **Manual Cleanup**: Looped sounds require manual stopping
- **Memory Usage**: Looped sounds stay in memory until stopped

## Troubleshooting

### Loop Not Playing
1. Check if the AudioClip is assigned
2. Verify the loop property is set to true
3. Ensure volume is above 0
4. Check AudioConfig settings

### Loop Won't Stop
1. Verify you're using the correct ID returned from PlayLoopedSFX
2. Check if the ID is still valid (not -1)
3. Use StopAllSFX() as a fallback

### Performance Issues
1. Limit the number of simultaneous loops
2. Use 2D audio for non-positional loops
3. Consider using lower quality audio for background loops
4. Monitor the AudioSource pool size in AudioConfig