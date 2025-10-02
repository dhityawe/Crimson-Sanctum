# Game Over Button Navigation Setup Guide

## 📋 Complete Setup for Game Over Screen with Button Navigation

### 🎯 Overview
This guide shows you how to set up a complete Game Over screen with keyboard navigation, using:
- **ButtonSelector** - Handles keyboard/gamepad navigation
- **SceneLoaderButton** - Connects buttons to scene loading
- **GameSceneManager** - Manages scene transitions
- **GameOverManager** - Controls the game over sequence

---

## 🏗️ UI Hierarchy Setup

```
GameOverCanvas
├─ MaskedFadeOverlay (optional - for Hades-style effect)
├─ GameOverTitle (TextMeshProUGUI) - "GAME OVER"
└─ RestartPrompt (GameObject) ← Parent container
    ├─ VerticalLayoutGroup
    ├─ ButtonSelector ← Add this component
    └─ Buttons:
        ├─ RestartButton (Button)
        │   ├─ SceneLoaderButton ← Add this component
        │   └─ Text (TextMeshProUGUI) "Restart"
        ├─ MainMenuButton (Button)
        │   ├─ SceneLoaderButton ← Add this component
        │   └─ Text (TextMeshProUGUI) "Main Menu"
        └─ QuitButton (Button)
            ├─ SceneLoaderButton ← Add this component
            └─ Text (TextMeshProUGUI) "Quit"
```

---

## ⚙️ Component Configuration

### 1. **RestartPrompt (Parent GameObject)**

Add **ButtonSelector** component:
```
Button Parent: Self (the RestartPrompt GameObject)
Starting Index: 0

Visual Feedback:
├─ Selected Color: Yellow (FFFF00)
├─ Pressed Color: Red (FF0000)
└─ Normal Color: White (FFFFFF)

Fade Animation:
├─ Enable Fade Animation: ✓
├─ Fade Min Alpha: 0.5
├─ Fade Max Alpha: 1.0
├─ Fade Duration: 1.2
└─ Fade Ease: InOutSine
```

### 2. **RestartButton**

Add **SceneLoaderButton** component:
```
Scene Loading Settings:
├─ Load Action: ReloadCurrent
└─ Scene Name: (leave empty for reload)

Options:
├─ Load On Click: ✓
└─ Hide Game Over On Load: ✓
```

### 3. **MainMenuButton**

Add **SceneLoaderButton** component:
```
Scene Loading Settings:
├─ Load Action: LoadMainMenu
└─ Scene Name: (leave empty)

Options:
├─ Load On Click: ✓
└─ Hide Game Over On Load: ✓
```

### 4. **QuitButton**

Add **SceneLoaderButton** component:
```
Scene Loading Settings:
├─ Load Action: QuitGame
└─ Scene Name: (leave empty)

Options:
├─ Load On Click: ✓
└─ Hide Game Over On Load: ✓
```

---

## 🎮 Input Controls

### Keyboard Navigation:
- **Vertical Layout:**
  - Arrow Up / W - Move up
  - Arrow Down / S - Move down
  
- **Horizontal Layout:**
  - Arrow Left / A - Move left
  - Arrow Right / D - Move right

- **Select Button:**
  - Enter / Space - Click selected button

### Features:
- ✅ Wraps around (bottom → top, right → left)
- ✅ Visual feedback (color changes)
- ✅ Breathing fade animation on selected button
- ✅ Pressed color flash when clicking
- ✅ Mouse click support (updates selection)

---

## 🔧 GameSceneManager Setup

Make sure you have **GameSceneManager** in your scene:

1. **Create GameObject** named "GameSceneManager"
2. **Add GameSceneManager component**
3. **Configure scenes:**

```
Scene Configuration:
├─ Main Menu Scene: Drag your MainMenu scene
├─ Gameplay Scene: Drag your Gameplay scene
└─ (Optional) Next/Previous scenes

Available Scenes:
└─ Add all scenes you want to load by name
```

---

## 🎯 Best Practices

### ✅ **Method 1: Using SceneLoaderButton (Recommended)**
**Best for:** Most use cases, clean Inspector setup

```
Button
├─ SceneLoaderButton (Component)
│   └─ Configure load action in Inspector
└─ ButtonSelector handles navigation automatically
```

**Pros:**
- Easy to configure in Inspector
- No code needed
- Automatic GameOver hiding
- Dropdown menu for actions

### ✅ **Method 2: Direct Static Calls**
**Best for:** Custom scripts, complex logic

```csharp
using UnityEngine;
using UnityEngine.UI;

public class CustomButton : MonoBehaviour
{
    private Button button;
    
    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }
    
    void OnClick()
    {
        // Hide game over
        GameOverManager.Instance?.HideGameOver();
        
        // Load scene
        GameSceneManager.Reload();
    }
}
```

### ✅ **Method 3: Inspector Button.onClick**
**Best for:** Simple one-off buttons

1. Select Button in Inspector
2. In **Button.onClick**, click **+**
3. Drag **SceneLoaderButton** component
4. Select method: `ReloadScene()` or `LoadMainMenu()` etc.

---

## 📝 Available Static Methods

```csharp
// GameSceneManager static methods
GameSceneManager.Load(string sceneName);   // Load any scene
GameSceneManager.Reload();                 // Reload current scene
GameSceneManager.LoadNext();               // Load next scene
GameSceneManager.LoadPrevious();           // Load previous scene
GameSceneManager.GoToMainMenu();           // Load main menu
GameSceneManager.StartGameplay();          // Load gameplay scene

// GameOverManager
GameOverManager.Instance.HideGameOver();   // Hide game over screen
GameOverManager.Instance.TriggerGameOver(); // Show game over screen
```

---

## 🐛 Troubleshooting

### Buttons don't navigate?
- ✓ Check ButtonSelector's `Button Parent` is assigned
- ✓ Ensure buttons have `Button` component
- ✓ Buttons must be **interactable** (not disabled)
- ✓ Check VerticalLayoutGroup/HorizontalLayoutGroup exists

### Scene doesn't load?
- ✓ Scene must be in **Build Settings**
- ✓ Check GameSceneManager has scene configured
- ✓ Enable Debug Logs in GameSceneManager to see errors

### Fade animation not working?
- ✓ `Enable Fade Animation` checked in ButtonSelector
- ✓ Button must have TextMeshProUGUI child
- ✓ Check fadeMinAlpha != fadeMaxAlpha

### Enter key doesn't work?
- ✓ ButtonSelector must be enabled
- ✓ Parent GameObject must be active
- ✓ Check no other script is consuming input

---

## 🎨 Customization Tips

### Change Navigation Layout:
- **Vertical:** Add `VerticalLayoutGroup` component
- **Horizontal:** Add `HorizontalLayoutGroup` component
- **Grid:** Use Vertical layout, ButtonSelector auto-detects

### Custom Colors:
```csharp
// In ButtonSelector Inspector
Selected Color: Your theme color
Pressed Color: Complementary color
Normal Color: Usually white or gray
```

### Add Sound Effects:
```csharp
// In ButtonSelector
Navigate Sound: Assign audio clip for navigation
Select Sound: Assign audio clip for button press
```

---

## 📦 Complete Example Setup

1. **Create UI:**
   - Canvas → RestartPrompt (GameObject)
   - Add VerticalLayoutGroup to RestartPrompt
   - Add 3 Button children with TextMeshProUGUI

2. **Add Components:**
   - ButtonSelector → RestartPrompt
   - SceneLoaderButton → Each button

3. **Configure:**
   - Button 1: Load Action = ReloadCurrent
   - Button 2: Load Action = LoadMainMenu  
   - Button 3: Load Action = QuitGame

4. **Test:**
   - Trigger game over
   - Use Arrow keys to navigate
   - Press Enter to select

**Done!** 🎉 You now have a fully functional game over screen with keyboard navigation!

---

## 🔗 Component Relationships

```
Player Dies
    ↓
GameOverManager.TriggerGameOver()
    ↓
RestartPrompt.SetActive(true)
    ↓
ButtonSelector.OnEnable() → Starts navigation
    ↓
User presses Arrow keys → ButtonSelector updates selection
    ↓
User presses Enter → ButtonSelector invokes Button.onClick
    ↓
SceneLoaderButton.ExecuteLoadAction()
    ↓
GameSceneManager.Reload() (or other action)
    ↓
Scene reloads
```

This architecture ensures clean separation of concerns and maximum flexibility! 🚀
