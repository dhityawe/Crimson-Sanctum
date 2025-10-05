# Quick Fix: Manual Camera Assignment

## 🎯 **The Solution**

Instead of searching for the camera automatically (which can fail with DontDestroyOnLoad), simply **drag the camera manually** in the Inspector!

---

## 📋 **Setup Steps**

### **1. Find GameOverManager in Scene:**
- It should be in a GameObject (usually called "GameOverManager" or similar)
- Since it uses `DontDestroyOnLoad`, it persists between scenes

### **2. Assign Camera Reference:**

In the **GameOverManager** Inspector:

```
GameOverManager (Script)
├─ UI References
│   ├─ Game Over Canvas: [Assigned]
│   ├─ Masked Overlay: [Optional]
│   ├─ Game Over Title: [Assigned]
│   └─ Restart Prompt: [Assigned]
│
└─ Camera Reference
    └─ Camera Reference: ← **DRAG YOUR MAIN CAMERA HERE!**
```

### **3. How to Assign:**

**Option A: Drag from Hierarchy**
1. Open your main gameplay scene
2. Find "Main Camera" in Hierarchy
3. Drag it to the **Camera Reference** field in GameOverManager

**Option B: Click the Circle Icon**
1. Click the circle icon (⊙) next to Camera Reference
2. Select "Main Camera" from the popup

---

## ✅ **Benefits**

### **Manual Assignment (Recommended):**
- ✅ 100% reliable - no search needed
- ✅ Works with DontDestroyOnLoad
- ✅ No timing issues
- ✅ Faster (no search overhead)
- ✅ Clear in Inspector what camera is used

### **Automatic Fallback:**
If you don't assign a camera, it will still try to find it automatically:
1. Try Camera.main
2. Try FindGameObjectWithTag("MainCamera")
3. Try FindFirstObjectByType<Camera>

---

## 🎮 **Testing**

1. **Assign camera in Inspector**
2. **Play game**
3. **Check Console:** Should see green log:
   ```
   [GameOverManager] Using manually assigned camera: Main Camera
   ```
4. **Die → Camera centers** ✓
5. **Restart → Die again → Camera centers** ✓

---

## 🐛 **Troubleshooting**

### **Camera still NULL after assignment?**
- Make sure you assigned the camera in the GameOverManager that persists (DontDestroyOnLoad)
- Check if there are multiple GameOverManagers in the scene

### **Assignment gets cleared after scene reload?**
- This is normal! The camera reference is a **scene reference**, not a prefab reference
- The manual assignment works because GameOverManager persists and keeps the reference
- Make sure to assign it in the **first scene** where GameOverManager spawns

### **Want to change camera per scene?**
- Keep the automatic fallback - just don't assign anything
- Each scene will find its own camera automatically

---

## 💡 **Recommendation**

**For your use case (player instantiated each game):**
- ✅ **Assign the camera manually** in Inspector
- Camera persists, player respawns → perfect!
- No more NULL camera issues
- Clean, simple, reliable

Just drag the camera once and forget about it! 🎯✨
