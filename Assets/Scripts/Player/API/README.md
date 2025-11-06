# Player API SDK Documentation

SDK untuk menghubungkan Unity dengan REST API Player (ASP.NET Core)

## 📁 Struktur File

```
Assets/Scripts/Player/API/
├── PlayerApiDTOs.cs          # Data Transfer Objects (Request & Response)
├── PlayerApiClient.cs        # HTTP Client untuk API calls
├── PlayerApiConfig.cs        # ScriptableObject untuk konfigurasi
├── PlayerDataManager.cs      # Manager utama (Singleton)
├── Examples/
│   └── PlayerApiExample.cs   # Contoh penggunaan
└── README.md                 # Dokumentasi ini
```

## 🚀 Setup Awal

### 1. Buat Config Asset

1. Di Unity Editor: **Assets → Create → Crimson Sanctum → Player API Config**
2. Atur `Base URL` sesuai dengan server Anda (contoh: `http://localhost:5000`)
3. Atur `Auto Save Interval` (default: 30 detik)

### 2. Setup Scene

1. Buat GameObject baru: **Create → Empty GameObject**
2. Rename menjadi "PlayerDataManager"
3. Attach component: `PlayerDataManager.cs`
4. Drag & drop `PlayerApiConfig` yang sudah dibuat ke field `Api Config`
5. Enable `Enable Debug Logs` untuk testing

## 📖 Cara Penggunaan

### Basic Usage

```csharp
using Assets.Scripts.Player.API;

public class MyGameScript : MonoBehaviour
{
    private void Start()
    {
        // Login atau buat player baru
        PlayerDataManager.Instance.LoginOrCreatePlayer(
            nickname: "PlayerName",
            onSuccess: () => {
                Debug.Log("Login sukses!");
            },
            onError: (error) => {
                Debug.LogError($"Login gagal: {error}");
            }
        );
    }
}
```

### Mengupdate Score dan Coin

```csharp
// Tambah score
PlayerDataManager.Instance.AddScore(100);

// Update score langsung
PlayerDataManager.Instance.UpdateScore(500);

// Tambah coin
PlayerDataManager.Instance.AddCoin(50);

// Kurangi coin (dengan validasi)
bool success = PlayerDataManager.Instance.SpendCoin(100);
if (success) {
    Debug.Log("Coin berhasil dikurangi");
}
```

### Menyimpan Data

```csharp
// Save ke local (PlayerPrefs) saja
PlayerDataManager.Instance.SaveToLocal();

// Save ke server saja
PlayerDataManager.Instance.SaveToServer(
    onSuccess: () => Debug.Log("Saved to server"),
    onError: (error) => Debug.LogError(error)
);

// Save ke local DAN server
PlayerDataManager.Instance.SaveToLocalAndServer(
    onSuccess: () => Debug.Log("Saved successfully"),
    onError: (error) => Debug.LogError(error)
);
```

### Mendapatkan Leaderboard

```csharp
PlayerDataManager.Instance.GetLeaderboard(
    top: 10,
    onSuccess: (entries) => {
        foreach (var entry in entries) {
            Debug.Log($"#{entry.rank} - {entry.nickname}: {entry.score}");
        }
    },
    onError: (error) => {
        Debug.LogError($"Gagal load leaderboard: {error}");
    }
);
```

### Cek Status Login

```csharp
if (PlayerDataManager.Instance.IsLoggedIn) {
    string playerId = PlayerDataManager.Instance.PlayerId;
    string nickname = PlayerDataManager.Instance.Nickname;
    int score = PlayerDataManager.Instance.Score;
    int coin = PlayerDataManager.Instance.Coin;

    Debug.Log($"{nickname} - Score: {score}, Coin: {coin}");
}
```

### Logout

```csharp
PlayerDataManager.Instance.Logout();
```

## 🎮 Contoh Use Case dalam Game

### Saat Player Mulai Game

```csharp
void OnGameStart() {
    // Load existing player atau buat baru
    PlayerDataManager.Instance.LoginOrCreatePlayer(
        PlayerPrefs.GetString("LastNickname", "Player"),
        onSuccess: () => {
            StartGameplay();
        }
    );
}
```

### Saat Mengumpulkan Koin di Gameplay

```csharp
void OnCoinCollected(int amount) {
    PlayerDataManager.Instance.AddCoin(amount);
    // Auto-save akan berjalan setiap X detik (sesuai config)
}
```

### Saat Game Over

```csharp
void OnGameOver(int finalScore) {
    // Update score jika lebih tinggi
    if (finalScore > PlayerDataManager.Instance.Score) {
        PlayerDataManager.Instance.UpdateScore(finalScore);
    }

    // Save ke server
    PlayerDataManager.Instance.SaveToLocalAndServer(
        onSuccess: () => {
            ShowGameOverScreen();
        }
    );
}
```

### Menampilkan Leaderboard di UI

```csharp
void ShowLeaderboard() {
    PlayerDataManager.Instance.GetLeaderboard(
        top: 20,
        onSuccess: (entries) => {
            foreach (var entry in entries) {
                // Update UI dengan data leaderboard
                AddLeaderboardEntry(entry.rank, entry.nickname, entry.score);
            }
        }
    );
}
```

## 🔧 Fitur Auto-Save

SDK sudah dilengkapi dengan auto-save yang berjalan di background:

- **Auto-save interval** dapat diatur di `PlayerApiConfig`
- Auto-save hanya jalan jika player sudah login
- Data juga otomatis disimpan saat:
  - Aplikasi di-pause (background)
  - Aplikasi ditutup (quit)

## 📝 API Endpoints yang Tersedia

| Method | Endpoint                        | Fungsi                                |
| ------ | ------------------------------- | ------------------------------------- |
| POST   | /api/player/new                 | Membuat player baru                   |
| POST   | /api/player/load                | Load data player berdasarkan nickname |
| POST   | /api/player/save                | Save data player ke server            |
| GET    | /api/player/leaderboard?top={n} | Mendapatkan top N leaderboard         |

## ⚠️ Important Notes

1. **Singleton Pattern**: `PlayerDataManager` menggunakan singleton, akses dengan `PlayerDataManager.Instance`
2. **DontDestroyOnLoad**: Manager tidak akan hilang saat pindah scene
3. **PlayerPrefs**: Data disimpan lokal menggunakan PlayerPrefs sebagai cache
4. **Async Operations**: Semua API calls adalah async (menggunakan coroutines)
5. **Error Handling**: Selalu provide callback untuk handle error

## 🐛 Debugging

Enable `Enable Debug Logs` di `PlayerDataManager` component untuk melihat log detail:

- Login/Create player
- Load/Save operations
- Score/Coin updates
- API responses

## 📦 Dependencies

- **Unity 2019.4+** atau lebih baru
- **UnityWebRequest** (built-in)
- **JsonUtility** (built-in)

## 🔐 Security Notes

- Jangan expose `PlayerApiConfig` asset di production build
- Pertimbangkan enkripsi PlayerPrefs untuk data sensitif
- Implementasikan authentication token jika diperlukan di masa depan

## 📞 Support

Untuk pertanyaan atau issue, cek:

- Example scene di `Examples/PlayerApiExample.cs`
- Debug logs di Unity Console
- API documentation dari backend team

---

**Version**: 1.0.0  
**Last Updated**: November 6, 2025
