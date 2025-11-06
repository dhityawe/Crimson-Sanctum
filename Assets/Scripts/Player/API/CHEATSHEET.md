# 📝 Player API SDK - Cheatsheet

Quick reference untuk method-method yang paling sering digunakan.

---

## 🔐 Authentication

### Login atau Buat Player Baru (Recommended)
```csharp
PlayerDataManager.Instance.LoginOrCreatePlayer(
    "PlayerName",
    onSuccess: () => { /* Login berhasil */ },
    onError: (error) => { /* Handle error */ }
);
```

### Buat Player Baru (Manual)
```csharp
PlayerDataManager.Instance.CreateNewPlayer(
    "PlayerName",
    onSuccess: () => { /* Player dibuat */ },
    onError: (error) => { /* Handle error */ }
);
```

### Load Player dari Server
```csharp
PlayerDataManager.Instance.LoadPlayerFromServer(
    "PlayerName",
    onSuccess: () => { /* Player loaded */ },
    onError: (error) => { /* Handle error */ }
);
```

### Logout
```csharp
PlayerDataManager.Instance.Logout();
```

---

## 💰 Manage Coins

### Tambah Coin
```csharp
PlayerDataManager.Instance.AddCoin(50);
```

### Set Coin
```csharp
PlayerDataManager.Instance.UpdateCoin(100);
```

### Kurangi Coin (dengan validasi)
```csharp
bool success = PlayerDataManager.Instance.SpendCoin(50);
if (success) {
    // Coin berhasil dikurangi
} else {
    // Coin tidak cukup
}
```

### Cek Jumlah Coin
```csharp
int currentCoins = PlayerDataManager.Instance.Coin;
```

### Cek Apakah Coin Cukup (Extension Method)
```csharp
using Assets.Scripts.Player.API;

if (PlayerDataManager.Instance.HasEnoughCoins(100)) {
    // Coin cukup
}
```

---

## 🏆 Manage Score

### Tambah Score
```csharp
PlayerDataManager.Instance.AddScore(100);
```

### Set Score
```csharp
PlayerDataManager.Instance.UpdateScore(500);
```

### Cek Score Sekarang
```csharp
int currentScore = PlayerDataManager.Instance.Score;
```

---

## 💾 Save & Load

### Save ke Local (PlayerPrefs) Saja
```csharp
PlayerDataManager.Instance.SaveToLocal();
```

### Save ke Server Saja
```csharp
PlayerDataManager.Instance.SaveToServer(
    onSuccess: () => { /* Saved */ },
    onError: (error) => { /* Error */ }
);
```

### Save ke Local DAN Server (Recommended)
```csharp
PlayerDataManager.Instance.SaveToLocalAndServer(
    onSuccess: () => { /* Saved */ },
    onError: (error) => { /* Error */ }
);
```

### Load dari Local
```csharp
PlayerDataManager.Instance.LoadFromLocal();
```

---

## 🏅 Leaderboard

### Get Leaderboard
```csharp
PlayerDataManager.Instance.GetLeaderboard(
    top: 10,
    onSuccess: (entries) => {
        foreach (var entry in entries) {
            Debug.Log($"#{entry.rank} - {entry.nickname}: {entry.score}");
        }
    },
    onError: (error) => {
        Debug.LogError(error);
    }
);
```

### Display Leaderboard (dengan Extension)
```csharp
using Assets.Scripts.Player.API;

PlayerDataManager.Instance.GetLeaderboard(10,
    onSuccess: (entries) => {
        foreach (var entry in entries) {
            string formatted = entry.GetFormattedEntry();
            // formatted = "🥇 PlayerName - 1,000 points"
        }
    }
);
```

---

## ℹ️ Get Player Info

### Cek Login Status
```csharp
if (PlayerDataManager.Instance.IsLoggedIn) {
    // Player sudah login
}
```

### Get Player ID
```csharp
string playerId = PlayerDataManager.Instance.PlayerId;
```

### Get Nickname
```csharp
string nickname = PlayerDataManager.Instance.Nickname;
```

### Get All Info (Extension Method)
```csharp
using Assets.Scripts.Player.API;

string info = PlayerDataManager.Instance.GetPlayerInfoString();
// Returns formatted string dengan semua info
```

---

## 🔧 Helper Functions

### Auto-Login dengan Last Nickname
```csharp
using Assets.Scripts.Player.API;

if (PlayerPrefsHelper.ShouldAutoLogin()) {
    string lastNickname = PlayerPrefsHelper.GetLastNickname();
    PlayerDataManager.Instance.LoginOrCreatePlayer(lastNickname,
        onSuccess: () => { /* Success */ },
        onError: (error) => { /* Error */ }
    );
}
```

### Save Last Nickname untuk Auto-Login
```csharp
using Assets.Scripts.Player.API;

PlayerPrefsHelper.SaveLastNickname("PlayerName");
```

### Clear Login Data
```csharp
using Assets.Scripts.Player.API;

PlayerPrefsHelper.ClearLoginData();
PlayerDataManager.Instance.Logout();
```

---

## 🎯 Common Patterns

### Pattern 1: Game Start
```csharp
void Start() {
    if (PlayerPrefsHelper.ShouldAutoLogin()) {
        string nickname = PlayerPrefsHelper.GetLastNickname();
        PlayerDataManager.Instance.LoginOrCreatePlayer(nickname,
            () => StartGame(),
            (error) => ShowLoginScreen()
        );
    } else {
        ShowLoginScreen();
    }
}
```

### Pattern 2: Collect Coin
```csharp
void OnCoinCollected(int value) {
    PlayerDataManager.Instance.AddCoin(value);
    UpdateUI();
    // Auto-save akan berjalan otomatis
}
```

### Pattern 3: Game Over dengan High Score
```csharp
void OnGameOver(int finalScore) {
    if (finalScore > PlayerDataManager.Instance.Score) {
        PlayerDataManager.Instance.UpdateScore(finalScore);
        ShowNewHighScore();
    }
    
    PlayerDataManager.Instance.SaveToLocalAndServer(
        () => ShowGameOverScreen(),
        (error) => ShowGameOverScreen() // Show anyway
    );
}
```

### Pattern 4: Buy Item
```csharp
void BuyItem(int cost) {
    if (PlayerDataManager.Instance.SpendCoin(cost)) {
        UnlockItem();
        UpdateUI();
        PlayerDataManager.Instance.SaveToLocalAndServer();
    } else {
        ShowInsufficientCoinsMessage();
    }
}
```

### Pattern 5: Update UI Continuously
```csharp
void Update() {
    scoreText.text = $"Score: {PlayerDataManager.Instance.Score:N0}";
    coinText.text = $"{PlayerDataManager.Instance.Coin}";
}
```

---

## ⚙️ Configuration

### Set Base URL di Inspector
1. Buka `PlayerApiConfig` asset
2. Set `Base Url` (contoh: `http://localhost:5000`)

### Set Auto-Save Interval
1. Buka `PlayerApiConfig` asset
2. Set `Auto Save Interval` (dalam detik, 0 = disabled)

### Enable/Disable Debug Logs
1. Select GameObject `PlayerDataManager` di scene
2. Toggle `Enable Debug Logs` di Inspector

---

## 🐛 Debug

### Print Player Info
```csharp
Debug.Log(PlayerDataManager.Instance.GetPlayerInfoString());
```

### Check Manager Status
```csharp
if (PlayerDataManager.Instance == null) {
    Debug.LogError("PlayerDataManager not initialized!");
}
```

### Manual Test API Connection
```csharp
// Test dengan membuat player baru
PlayerDataManager.Instance.CreateNewPlayer("TestPlayer",
    () => Debug.Log("✓ API Connected"),
    (error) => Debug.LogError($"✗ API Error: {error}")
);
```

---

## 📋 Properties Reference

| Property | Type | Description |
|----------|------|-------------|
| `IsLoggedIn` | bool | Player sudah login atau belum |
| `PlayerId` | string | UUID player dari server |
| `Nickname` | string | Nickname player |
| `Score` | int | Score player |
| `Coin` | int | Coin player |

---

## 🎨 Extension Methods

```csharp
using Assets.Scripts.Player.API;

// Cek coin cukup
bool hasEnough = manager.HasEnoughCoins(100);

// Get formatted player info
string info = manager.GetPlayerInfoString();

// Format leaderboard entry
string formatted = entry.GetFormattedEntry();
```

---

## 🔥 Pro Tips

1. **Gunakan `LoginOrCreatePlayer()`** - Lebih praktis daripada manual check
2. **Auto-save sudah built-in** - Tidak perlu save manual setiap perubahan
3. **Always handle errors** - Network bisa gagal kapan saja
4. **Use extensions** - Lebih readable dan maintainable
5. **Cache UI updates** - Jangan query manager setiap frame jika tidak perlu

---

**Quick Access**
- Full Documentation: `README.md`
- Setup Guide: `SETUP_GUIDE.md`
- Examples: `Examples/` folder

