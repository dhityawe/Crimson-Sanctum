# Hades-Style Game Over Effect

This system creates a Hades-inspired game over effect where the screen fades to black while keeping the character visible during their death animation, complete with dramatic "GAME OVER" text.

## Features

✨ **Character Mask**: Player remains visible while everything else fades to black  
🎭 **Shader-Based**: Uses custom UI shader for smooth masking effect  
⚡ **DOTween Integration**: Smooth animations with customizable timing  
🎮 **Easy Integration**: Works with existing player death system  
🔧 **Auto Setup**: Helper script for quick scene setup  

## Quick Setup

### Simple Setup (Recommended)
1. The `GameOverManager` will automatically create all necessary UI when triggered
2. No manual setup required - everything happens automatically on player death
3. Uses basic black overlay for clean, simple game over effect

### Advanced Setup (Optional)
1. Create a Canvas with `GameOverManager` component  
2. Add child GameObject with `MaskedFadeOverlay` component for Hades-style character masking
3. Add TextMeshPro elements for custom "Game Over" title and restart prompt
4. Assign references in the GameOverManager inspector

## Components

### GameOverManager
Main controller that orchestrates the entire effect.
- Manages fade timing and animations
- Coordinates with player death events
- Singleton pattern for easy access
- Auto-creates simple UI if none exists

### MaskedFadeOverlay (Optional)
Handles the advanced fade effect with character exclusion.
- Uses custom shader for circular masking
- Automatically follows player position  
- Configurable mask radius and softness
- Only needed for Hades-style character exclusion

## Customization

### Timing Adjustments
```csharp
[Header("Animation Timing")]
[SerializeField] private float fadeInDuration = 2.5f;        // How long the fade takes
[SerializeField] private float titleAppearDelay = 1.5f;     // When title appears
[SerializeField] private float titleFadeDuration = 1f;      // Title fade speed
```

### Mask Settings
```csharp
[Header("Character Masking")]
[SerializeField] private float characterMaskRadius = 120f;   // Size of clear area around character
[SerializeField] private float characterMaskSoftness = 80f;  // Softness of mask edge
```

## Integration with Existing Systems

The effect automatically integrates with your existing player death system:

1. **Player Death**: `PlayerMove.HandleDeath()` triggers `PlayerEvents.OnPlayerDeath`
2. **State Change**: `PlayingState.OnPlayerDeathNew()` calls `GameManager.ChangeToGameOverState()`
3. **Visual Effect**: `GameOverState.EnterState()` triggers `GameOverManager.TriggerGameOver()`

## Shader Details

The `MaskedFade.shader` creates the character exclusion effect:
- **Circular Mask**: Creates transparent area around character
- **Soft Edges**: Smooth transition from transparent to opaque
- **Screen Space**: Automatically handles different screen resolutions
- **UI Compatible**: Works with Unity's UI system and Canvas

## Troubleshooting

### Character Not Excluded from Fade
- Check that player GameObject has "Player" tag
- Verify `MaskedFadeOverlay.SetTarget()` is being called
- Ensure camera reference is valid

### Fade Effect Not Appearing
- Verify Canvas sort order is high enough (recommended: 1000+)
- Check that `GameOverManager.Instance` exists in scene
- Ensure shader is properly assigned to overlay material

### Animation Timing Issues
- Adjust timing values in `GameOverManager` inspector
- Check that DOTween is properly imported
- Verify no other scripts are interfering with UI elements

## Performance Notes

- Shader uses minimal calculations for optimal performance
- Material instances are created per overlay to avoid shared state
- DOTween sequences are properly cleaned up to prevent memory leaks

## Example Usage

```csharp
// Trigger game over effect manually
if (GameOverManager.Instance != null)
{
    GameOverManager.Instance.TriggerGameOver(playerTransform);
}

// Check if effect is active
bool isGameOverActive = GameOverManager.Instance.IsGameOverActive();

// Hide effect
GameOverManager.Instance.HideGameOver();
```

## Dependencies

- **Unity 2022.3+**: Core functionality
- **DOTween**: Animation system
- **TextMeshPro**: Text rendering
- **Universal Render Pipeline** (Optional): Enhanced shader support

---

*This effect recreates the dramatic game over sequence from Hades, where Zagreus remains visible as the underworld fades around him. Perfect for adding cinematic flair to player death sequences!*