# Camera Zoom Effect - Configuration Guide

## Overview
The camera zoom effect creates a dramatic Hades-style focus on the dying character by zooming in and centering the camera on them during the game over sequence.

**✨ Now supports both regular Camera and Cinemachine!** The system automatically detects which camera system you're using and applies the appropriate zoom method.

## Cinemachine vs Regular Camera

### Cinemachine (Recommended)
- **Auto-detected**: System automatically finds CinemachineCamera
- **Smooth following**: Uses Cinemachine's built-in smooth follow
- **Professional results**: Leverages Cinemachine's advanced camera features
- **Zoom method**: Adjusts Lens.OrthographicSize
- **Centering**: Changes Follow target to player transform

### Regular Camera
- **Fallback option**: Used if no Cinemachine camera found
- **Direct control**: Manually animates camera transform and size
- **Simple setup**: No additional components needed
- **Zoom method**: Animates orthographicSize with DOTween
- **Centering**: Moves camera transform to player position

## Configuration Parameters

### Enable Camera Zoom
```csharp
[SerializeField] private bool enableCameraZoom = true;
```
- **true**: Camera will zoom and center on character
- **false**: No camera movement (just UI effects)

### Zoom Amount
```csharp
[SerializeField] private float zoomAmount = 0.7f;
```
- **Range**: 0.1 to 1.0
- **0.5**: Strong zoom (50% of original view)
- **0.7**: Medium zoom (70% of original view) - **Recommended**
- **0.9**: Subtle zoom (90% of original view)
- **1.0**: No zoom at all
- Lower values = more dramatic zoom

### Zoom Duration
```csharp
[SerializeField] private float zoomDuration = 2f;
```
- How long the zoom animation takes in seconds
- **1.5s**: Quick zoom
- **2.0s**: Smooth zoom - **Recommended**
- **3.0s**: Slow, dramatic zoom

### Center On Character
```csharp
[SerializeField] private bool centerOnCharacter = true;
```
- **true**: Camera moves to center on character position
- **false**: Camera only zooms, doesn't move

### Center Offset
```csharp
[SerializeField] private Vector3 centerOffset = Vector3.zero;
```
- Adjusts the centering position relative to character
- **Vector3.zero**: Perfectly centered
- **Vector3.up * 2**: Center 2 units above character
- Useful for framing the character better

### Center Duration
```csharp
[SerializeField] private float centerDuration = 1.5f;
```
- How long the camera takes to move to center
- Can be different from zoom duration for layered effects

### Zoom Ease
```csharp
[SerializeField] private Ease zoomEase = Ease.InOutQuad;
```
Available easing options:
- **Ease.InOutQuad**: Smooth acceleration and deceleration - **Recommended**
- **Ease.InQuad**: Starts slow, speeds up
- **Ease.OutQuad**: Starts fast, slows down
- **Ease.Linear**: Constant speed
- **Ease.InOutCubic**: More dramatic curve
- **Ease.InOutSine**: Very smooth, organic feel

## Recommended Presets

### Dramatic (Hades-style)
```
Enable Camera Zoom: true
Zoom Amount: 0.6
Zoom Duration: 2.5s
Center On Character: true
Center Offset: (0, 1, 0) // Slightly above character
Center Duration: 2s
Zoom Ease: InOutQuad
```

### Subtle
```
Enable Camera Zoom: true
Zoom Amount: 0.85
Zoom Duration: 1.5s
Center On Character: true
Center Offset: (0, 0, 0)
Center Duration: 1.2s
Zoom Ease: InOutSine
```

### Cinematic
```
Enable Camera Zoom: true
Zoom Amount: 0.5
Zoom Duration: 3s
Center On Character: true
Center Offset: (0, 2, 0)
Center Duration: 2.5s
Zoom Ease: InOutCubic
```

### No Zoom (Just UI)
```
Enable Camera Zoom: false
```

## Technical Notes

- **Auto-Detection**: Automatically detects if you're using Cinemachine or regular Camera
- **Cinemachine Integration**: If Cinemachine is detected, it uses `Follow` target and `Lens.OrthographicSize`
- **Automatic Reset**: Camera automatically resets to original settings on scene restart
- **Unscaled Time**: Zoom uses unscaled time so it works even if game is paused
- **Thread Safe**: Safely kills ongoing animations if triggered multiple times
- **Performance**: Very lightweight, uses DOTween for smooth interpolation
- **No Manual Setup**: Just works with your existing camera setup!

## Tips

1. **Cinemachine Users**: The system will automatically use your Cinemachine camera - no extra setup needed!
2. **Center Offset**: With Cinemachine, offset is handled by the Follow component settings
3. **Balance with Fade**: Adjust zoom duration to match your fade-in duration for synchronized effects
4. **Character Framing**: Use centerOffset (regular camera) or Cinemachine's Tracking settings
5. **Testing**: Test with different character sizes and death locations
6. **Easing**: Experiment with different ease functions to find your preferred feel
7. **Coordination**: Time the zoom with your death animation for maximum impact
8. **Smooth Follow**: If using Cinemachine, its damping settings will affect how smoothly it centers

## Troubleshooting

### Camera Not Centering
- **If using Cinemachine**: Make sure your CinemachineCamera has a Follow target set initially
- **Check the logs**: GameOverManager will log whether it's using Cinemachine or regular camera
- The system automatically switches the Follow target to the player on game over

### Zoom Not Working
- Verify `enableCameraZoom` is set to `true`
- Check that a camera exists in the scene
- Look for debug logs in the console

### Cinemachine Not Detected
- Ensure you have a `CinemachineCamera` component in your scene
- The system uses `FindFirstObjectByType<CinemachineCamera>()`
- Check that Cinemachine is properly imported

---

*The camera zoom creates that iconic "moment of defeat" feeling, drawing focus to the character's final moments!*