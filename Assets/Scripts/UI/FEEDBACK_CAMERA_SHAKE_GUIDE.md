# FeedbackManager - Camera Shake Documentation

## Overview
The FeedbackManager now includes Cinemachine-based camera shake that triggers on player damage, creating impactful visual feedback.

## Camera Shake Features

### 1. **Cinemachine Integration**
- Uses `CinemachineBasicMultiChannelPerlin` for procedural shake
- Automatic camera detection
- Smooth shake decay with DOTween

### 2. **Configurable Parameters**
All shake values adjustable in Inspector:
- Shake intensity (amplitude)
- Shake speed (frequency)
- Shake duration
- Enable/disable toggle

### 3. **Smart Initialization**
- Auto-finds CinemachineCamera if not assigned
- Graceful fallback if Cinemachine not available
- Warning messages for missing components

## Inspector Configuration

```
╔═══════════════════════════════════════════════╗
║  Feedback Manager (Script)                    ║
╠═══════════════════════════════════════════════╣
║                                               ║
║  Panel Reference                              ║
║  ├─ Color Panel          [Image] 🔗           ║
║  └─ Feedback Panel       [Image] 🔗           ║
║                                               ║
║  Feedback Configs                             ║
║  ├─ Feedback Duration         0.5             ║
║  ├─ Feedback Panel Max Alpha  1.0   (0-1)     ║
║  ├─ Color Panel Max Alpha     0.12  (0-1)     ║
║  ├─ Hurt Color                🔴 Red          ║
║  ├─ Fade In Ease              OutQuad         ║
║  └─ Fade Out Ease             InQuad          ║
║                                               ║
║  Camera Shake                                 ║
║  ├─ Enable Camera Shake       ☑️              ║
║  ├─ Cinemachine Camera        [Optional] 🔗   ║
║  ├─ Shake Amplitude           2.0             ║
║  ├─ Shake Frequency           3.0             ║
║  └─ Shake Duration            0.3             ║
║                                               ║
║  Animator Reference                           ║
║  └─ Feedback Panel Anim  [UISpriteAnimator]  ║
║                                               ║
╚═══════════════════════════════════════════════╝
```

## Camera Shake Parameters

### Enable Camera Shake
**Type:** `bool`  
**Default:** `true`  
**Description:** Master toggle for camera shake effect

**When to Disable:**
- Testing other feedback elements
- Accessibility reasons (motion sensitivity)
- Cinematic sequences
- Cutscenes

---

### Cinemachine Camera
**Type:** `CinemachineCamera`  
**Default:** `null` (auto-finds)  
**Description:** Reference to your Cinemachine camera

**Setup Options:**

**Option 1: Auto-Detection (Recommended)**
- Leave empty in Inspector
- System automatically finds first CinemachineCamera in scene
- Perfect for single camera setups

**Option 2: Manual Assignment**
- Drag your CinemachineCamera from scene
- Use when you have multiple cameras
- Guarantees correct camera is used

---

### Shake Amplitude
**Type:** `float`  
**Default:** `2.0`  
**Range:** `0.0 - 10.0+`  
**Description:** Intensity/strength of camera shake

**Visual Guide:**
```
0.5  = Subtle vibration
1.0  = Noticeable shake
2.0  = Standard impact (Default)
3.0  = Strong hit
5.0  = Heavy impact
10.0 = Massive explosion
```

**Recommended Values:**
- Small damage: `0.5 - 1.0`
- Normal damage: `1.5 - 2.5`
- Critical hit: `3.0 - 5.0`
- Boss attacks: `5.0 - 8.0`

---

### Shake Frequency
**Type:** `float`  
**Default:** `3.0`  
**Range:** `0.0 - 10.0+`  
**Description:** Speed of camera oscillation

**Visual Guide:**
```
0.5  = Slow, smooth wobble
1.0  = Gentle shake
2.0  = Moderate shake
3.0  = Fast shake (Default)
5.0  = Rapid vibration
10.0 = Intense jitter
```

**Recommended Combinations:**
- **High Amplitude + Low Frequency** = Heavy, impactful hits
- **Low Amplitude + High Frequency** = Rapid, jittery effects
- **Balanced (2-3 both)** = Standard game feel

---

### Shake Duration
**Type:** `float`  
**Default:** `0.3` seconds  
**Description:** How long the shake lasts before fading to 0

**Timing Guide:**
```
0.1s = Quick snap
0.2s = Fast reaction
0.3s = Standard (Default)
0.5s = Noticeable impact
1.0s = Prolonged effect
```

**Best Practices:**
- Should be ≤ `feedbackDuration` for sync
- Shorter for frequent damage
- Longer for rare, impactful hits

---

## Cinemachine Setup Requirements

### Required Components on Camera

Your Cinemachine camera needs a noise component:

**Option 1: Add via Inspector**
1. Select your CinemachineCamera in scene
2. Click "Add Component"
3. Search for "Basic Multi Channel Perlin"
4. Add the component

**Option 2: Create New Camera**
```
1. GameObject → Cinemachine → Camera
2. In the Camera component list:
   - Add "Cinemachine Basic Multi Channel Perlin"
```

### Noise Profile Setup

The `CinemachineBasicMultiChannelPerlin` component needs:
- **Noise Profile**: Optional (can use default)
- **Amplitude Gain**: Will be controlled by FeedbackManager (start at 0)
- **Frequency Gain**: Will be controlled by FeedbackManager (start at 0)

**Default Configuration:**
```
Basic Multi Channel Perlin
├─ Noise Profile:     [6D Shake] (optional)
├─ Pivot Offset:      (0, 0, 0)
├─ Amplitude Gain:    0  ← Controlled by script
└─ Frequency Gain:    0  ← Controlled by script
```

---

## How It Works

### Initialization (Start)
```csharp
private void InitializeCinemachine()
{
    // 1. Auto-find camera if not assigned
    if (cinemachineCamera == null)
        cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
    
    // 2. Get noise component
    noiseComponent = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
    
    // 3. Reset shake to 0
    noiseComponent.AmplitudeGain = 0f;
    noiseComponent.FrequencyGain = 0f;
}
```

### Shake Trigger (Hurt)
```csharp
private void ShakeCamera()
{
    // 1. Set shake parameters
    noiseComponent.AmplitudeGain = shakeAmplitude;
    noiseComponent.FrequencyGain = shakeFrequency;
    
    // 2. Smoothly fade to 0 over shakeDuration
    DOTween.To(() => noiseComponent.AmplitudeGain, 
               x => noiseComponent.AmplitudeGain = x, 
               0f, shakeDuration)
           .SetEase(Ease.OutQuad);
    
    DOTween.To(() => noiseComponent.FrequencyGain, 
               x => noiseComponent.FrequencyGain = x, 
               0f, shakeDuration)
           .SetEase(Ease.OutQuad);
}
```

### Shake Timeline
```
Time:     0s          0.15s        0.3s
          │            │            │
Amplitude:  2.0 ═══════════════════► 0
Frequency:  3.0 ═══════════════════► 0
          
          │◄────── Smooth decay ───►│
          
Easing:   OutQuad (smooth deceleration)
```

---

## Combined Effect Timeline

```
Time:     0s          0.25s        0.5s
          ├───────────┼───────────┤
          
Feedback: 0 ══════► 255 ══════► 0
Color:    0 ══════► 30  ══════► 0
Camera:   
Amplitude: 2.0 ════════════► 0
          │◄─ 0.3s shake ──►│
          
Sprite:   "Hurt" animation plays
```

---

## Preset Configurations

### Preset 1: Subtle Impact
```
Camera Shake Settings:
├─ Enable Camera Shake:  ☑️
├─ Shake Amplitude:      1.0
├─ Shake Frequency:      2.0
└─ Shake Duration:       0.2s

Use Case: Frequent minor damage
Feel: Light bump, subtle feedback
```

---

### Preset 2: Standard Hit (Default)
```
Camera Shake Settings:
├─ Enable Camera Shake:  ☑️
├─ Shake Amplitude:      2.0
├─ Shake Frequency:      3.0
└─ Shake Duration:       0.3s

Use Case: Normal gameplay damage
Feel: Noticeable impact, game feel
```

---

### Preset 3: Heavy Impact
```
Camera Shake Settings:
├─ Enable Camera Shake:  ☑️
├─ Shake Amplitude:      4.0
├─ Shake Frequency:      4.0
└─ Shake Duration:       0.5s

Use Case: Boss attacks, critical hits
Feel: Strong, impactful shake
```

---

### Preset 4: Explosion
```
Camera Shake Settings:
├─ Enable Camera Shake:  ☑️
├─ Shake Amplitude:      6.0
├─ Shake Frequency:      5.0
└─ Shake Duration:       0.8s

Use Case: Massive damage, explosions
Feel: Screen-shaking intensity
```

---

### Preset 5: Rumble
```
Camera Shake Settings:
├─ Enable Camera Shake:  ☑️
├─ Shake Amplitude:      1.5
├─ Shake Frequency:      8.0
└─ Shake Duration:       0.4s

Use Case: Electric shock, rapid hits
Feel: Vibrating, jittery effect
```

---

## Advanced Techniques

### Dynamic Shake Based on Damage

```csharp
public void Hurt(float damageAmount)
{
    // Scale shake intensity with damage
    float damagePercent = damageAmount / maxHealth;
    
    shakeAmplitude = 1f + (damagePercent * 5f);  // 1-6 range
    shakeDuration = 0.2f + (damagePercent * 0.4f); // 0.2-0.6 range
    
    // ... rest of Hurt() code
}
```

### Damage Type Variations

```csharp
public void Hurt(DamageType type)
{
    switch (type)
    {
        case DamageType.Impact:
            shakeAmplitude = 3f;
            shakeFrequency = 2f;
            break;
            
        case DamageType.Explosion:
            shakeAmplitude = 6f;
            shakeFrequency = 4f;
            break;
            
        case DamageType.Electric:
            shakeAmplitude = 1.5f;
            shakeFrequency = 10f; // High frequency
            break;
    }
    
    // ... rest of Hurt() code
}
```

### Multi-Hit Combo

```csharp
private int comboCount = 0;

public void Hurt()
{
    comboCount++;
    
    // Increase shake with combo
    shakeAmplitude = Mathf.Min(2f + (comboCount * 0.5f), 6f);
    
    // Reset combo after delay
    DOVirtual.DelayedCall(1f, () => comboCount = 0);
    
    // ... rest of Hurt() code
}
```

---

## Troubleshooting

### Issue: Camera Doesn't Shake

**Solution 1: Check Component**
```
1. Select your CinemachineCamera
2. Verify "Basic Multi Channel Perlin" component exists
3. Check component is enabled
```

**Solution 2: Check Reference**
```
1. In FeedbackManager Inspector
2. Verify "Cinemachine Camera" is assigned or found
3. Check console for warning messages
```

**Solution 3: Check Enable Flag**
```
1. In FeedbackManager Inspector
2. Ensure "Enable Camera Shake" is checked ☑️
```

---

### Issue: Shake Too Subtle/Intense

**Solution:**
```
Adjust in Inspector:
- Shake Amplitude (intensity)
- Shake Frequency (speed)
- Shake Duration (length)

Test with preset values first
```

---

### Issue: Shake Doesn't Stop

**Solution:**
```
Check DOTween is installed correctly
Verify OnDestroy() is being called
Manually call StopCameraShake() if needed
```

---

### Issue: Multiple Cameras Conflict

**Solution:**
```
Manually assign correct camera:
1. Drag your gameplay camera to Inspector
2. Don't rely on auto-detection
3. Ensure only one camera has noise component
```

---

## Performance Notes

- ✅ **Lightweight**: Only tweens 2 float values
- ✅ **Efficient**: Uses Cinemachine's built-in noise
- ✅ **No GC**: Reuses noise component
- ✅ **Smooth**: Hardware-accelerated by Cinemachine

---

## Best Practices

### 1. **Sync with Feedback Duration**
```
shakeDuration ≤ feedbackDuration
```
Ensures shake ends before or with visual feedback.

### 2. **Balance Amplitude & Frequency**
```
Low Amplitude + High Frequency = Jitter
High Amplitude + Low Frequency = Impact
Balanced = Standard game feel
```

### 3. **Consider Player Comfort**
```
- Don't shake too frequently (causes motion sickness)
- Provide accessibility option to disable
- Test with different players
```

### 4. **Layer with Other Effects**
```
Camera Shake + Color Flash + Screen Vignette = Maximum Impact
```

### 5. **Match Game Genre**
```
Action Games:     Strong shake (3-5 amplitude)
Puzzle Games:     Minimal shake (0.5-1 amplitude)
Horror Games:     Subtle shake (1-2 amplitude)
Arcade Games:     Intense shake (4-8 amplitude)
```

---

## Accessibility Considerations

### Motion Sensitivity
Some players experience discomfort from camera shake:

**Option 1: Disable Toggle**
```csharp
[Header("Accessibility")]
[SerializeField] private bool reduceMotion = false;

private void Hurt()
{
    if (!reduceMotion && enableCameraShake)
        ShakeCamera();
    
    // ... rest of code
}
```

**Option 2: Intensity Reduction**
```csharp
float accessibilityMultiplier = reduceMotion ? 0.3f : 1.0f;
noiseComponent.AmplitudeGain = shakeAmplitude * accessibilityMultiplier;
```

**Option 3: Settings Integration**
```csharp
// In game settings
public void SetReduceMotion(bool value)
{
    reduceMotion = value;
    PlayerPrefs.SetInt("ReduceMotion", value ? 1 : 0);
}
```

---

## Full Effect Integration

When player takes damage, ALL effects trigger:

1. ✅ **Sprite Animation** - "Hurt" animation plays
2. ✅ **Feedback Panel** - Fades 0 → 255 → 0
3. ✅ **Color Panel** - Red tint 0 → 30 → 0
4. ✅ **Camera Shake** - Shake with decay
5. 🎯 **Optional**: Sound effect, controller rumble

This creates a **cohesive, impactful hurt feedback** that players will clearly notice!

---

## Testing Checklist

```
□ Camera shake triggers on damage
□ Shake intensity feels appropriate
□ Shake duration matches feedback
□ No performance issues
□ Shake stops properly
□ No conflicts with other camera effects
□ Works with your Cinemachine setup
□ Accessibility options considered
```

---

## Summary

The camera shake system provides:
- ✅ **Automatic**: Auto-finds Cinemachine camera
- ✅ **Configurable**: All parameters in Inspector
- ✅ **Smooth**: DOTween-powered decay
- ✅ **Safe**: Proper cleanup and fallbacks
- ✅ **Impactful**: Clear visual feedback
- ✅ **Professional**: Uses industry-standard Cinemachine

Combined with color flash and panel animations, creates a powerful damage feedback system!
