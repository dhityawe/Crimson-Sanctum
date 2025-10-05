# Audio System Setup Guide

## 📋 **Step-by-Step Setup:**

### **1. Create AudioConfig ScriptableObject**
1. Right-click in your Project window
2. Navigate to: `Create → Crimson Sanctum → Audio → Audio Config`
3. Name it something like `MainAudioConfig`
4. Configure the settings in the Inspector:

```
Global Volume Settings:
- Music Volume: 0.7
- SFX Volume: 0.8

CrossFade Settings:
- Enable CrossFade: ✓
- CrossFade Duration: 2.0s

SFX Settings:
- Max Concurrent SFX: 16
- Enable SFX Pooling: ✓
- Cleanup Interval: 2.0s

Debug Settings:
- Enable Debug Logs: ✓ (during development)
```

### **2. Set up AudioManager in Scene**
1. Create empty GameObject named "AudioManager"
2. Add the `AudioManagerRefactored` component
3. Assign your `MainAudioConfig` to the audioConfig field
4. The system will auto-create BGMManager, SFXManager, and CrossFadeHandler as children

### **3. Configure BGM (Background Music)**
1. Select the BGMManager child object
2. In the BGM Configuration section, set up your scenes:

```
Scene BGM List:
├── Element 0:
│   ├── Scene Name: "MainMenu"
│   ├── BGM Tracks:
│   │   ├── Track 0: Main Menu Theme
│   │   │   ├── Audio Clip: [Your BGM file]
│   │   │   ├── Volume: 0.8
│   │   │   ├── Loop: ✓
│   │   │   ├── Fade In: 2.0s
│   │   │   └── Fade Out: 1.5s
├── Element 1:
│   ├── Scene Name: "Gameplay"
│   ├── BGM Tracks: [Battle themes]
└── Element 2:
    ├── Scene Name: "GameOver"
    └── BGM Tracks: [Sad music]
```

### **4. Configure SFX (Sound Effects)**
1. Select the SFXManager child object
2. Set up your sound effects library:

```
SFX Configuration:
├── Element 0: UI Sounds
│   ├── Name: "buttonClick"
│   ├── Clip: [UI click sound]
│   ├── Volume: 0.6
│   ├── Pitch: 1.0
│   └── 3D: ✗
├── Element 1: Player Actions
│   ├── Name: "jumpSound"
│   ├── Clip: [Jump sound]
│   ├── Volume: 0.8
│   ├── Randomize Pitch: ✓
│   ├── Pitch Variation: 0.1
│   └── 3D: ✓
├── Element 2: Environment
│   ├── Name: "explosionSound"
│   ├── Clip: [Explosion]
│   ├── Volume: 1.0
│   ├── 3D: ✓
│   └── Max Distance: 50
```