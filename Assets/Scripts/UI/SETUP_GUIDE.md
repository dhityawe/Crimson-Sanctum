# Game Over Effect - Manual Setup Guide

## Quick Manual Setup

Since we've optimized for performance by removing automatic UI creation, here's how to set up the Game Over effect manually:

### 1. Create GameOverManager GameObject
1. Create empty GameObject in your scene
2. Name it "GameOverManager" 
3. Add `GameOverManager` component
4. Check "Don't Destroy On Load" if needed

### 2. Create Game Over Canvas
1. Create UI Canvas (Right-click → UI → Canvas)
2. Set Canvas to "Screen Space - Overlay"
3. Set Sorting Order to 1000 (high priority)
4. Make it child of GameOverManager

### 3. Add Basic UI Elements
1. **Black Background** (Optional):
   - Add UI Image to Canvas
   - Set anchors to stretch (0,0,1,1)
   - Set color to black
   - Set alpha to 0 initially

2. **Game Over Text**:
   - Add TextMeshPro text
   - Set text to "GAME OVER"
   - Position in center-upper area
   - Set alpha to 0 initially

3. **Restart Prompt** (Optional):
   - Add TextMeshPro text  
   - Set text to "Press R to Restart"
   - Position in center-lower area
   - Set alpha to 0 initially

### 4. Advanced: Add Character Masking (Optional)
1. Add child GameObject with `MaskedFadeOverlay` component
2. Assign the MaskedFade shader to the Image component
3. Configure mask radius and softness

### 5. Assign References
In GameOverManager inspector:
- **Game Over Canvas**: Your canvas
- **Masked Overlay**: Your MaskedFadeOverlay (if using)
- **Game Over Title**: Your title text
- **Restart Prompt**: Your restart text

### 6. Configure Timing
Adjust animation timing in GameOverManager:
- Fade In Duration: 2.5s
- Title Appear Delay: 1.5s  
- Title Fade Duration: 1s
- Prompt Appear Delay: 2.5s

## Performance Benefits
✅ **No runtime UI creation** - better performance  
✅ **Pre-built UI** - no instantiation overhead  
✅ **Manual control** - customize exactly what you need  
✅ **Cleaner code** - no fallback complexity  

## Result
When player dies → Game over effect triggers with your custom UI setup!