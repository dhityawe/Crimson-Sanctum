# 🚀 Quick Setup Guide - Player API SDK

Panduan cepat untuk setup dan mulai menggunakan SDK dalam 5 menit!

## ✅ Step 1: Buat Config Asset (1 menit)

1. Di Unity Editor, klik kanan di Project window
2. **Assets → Create → Crimson Sanctum → Player API Config**
3. Rename menjadi "PlayerApiConfig"
4. Klik config tersebut, di Inspector atur:
   - **Base Url**: `http://localhost:5000` (atau URL API server Anda)
   - **Auto Save Interval**: `30` (auto-save setiap 30 detik)
   - **Max Retries**: `3`
   - **Retry Delay**: `2`

## ✅ Step 2: Setup Manager di Scene (2 menit)

1. Di Hierarchy, klik kanan → **Create Empty**
2. Rename menjadi "PlayerDataManager"
3. Dengan GameObject tersebut terpilih, di Inspector:
   - **Add Component** → ketik "PlayerDataManager" → pilih
4. Drag **PlayerApiConfig** yang tadi dibuat ke field **Api Config**
5. Centang **Enable Debug Logs** untuk testing

> 💡 **Tip**: GameObject ini akan otomatis `DontDestroyOnLoad`, jadi cukup buat di scene pertama (main menu/splash screen)

## ✅ Step 3: Test SDK (2 menit)

Buat script baru `TestPlayerApi.cs`:

```csharp
using UnityEngine;
using Assets.Scripts.Player.API;

public class TestPlayerApi : MonoBehaviour
{
    void Start()
    {
        // Test 1: Login atau buat player baru
        TestLoginOrCreate();
    }
    
    void TestLoginOrCreate()
    {
        PlayerDataManager.Instance.LoginOrCreatePlayer(
            "TestPlayer123",
            onSuccess: () => {
                Debug.Log("✓ Login berhasil!");
                TestAddScoreAndCoin();
            },
            onError: (error) => {
                Debug.LogError($"✗ Login gagal: {error}");
            }
        );
    }
    
    void TestAddScoreAndCoin()
    {
        // Test 2: Tambah score dan coin
        PlayerDataManager.Instance.AddScore(100);
        PlayerDataManager.Instance.AddCoin(50);
        
        Debug.Log($"Score: {PlayerDataManager.Instance.Score}");
        Debug.Log($"Coin: {PlayerDataManager.Instance.Coin}");
        
        // Test 3: Save ke server
        PlayerDataManager.Instance.SaveToLocalAndServer(
            onSuccess: () => {
                Debug.Log("✓ Data tersimpan!");
                TestLeaderboard();
            },
            onError: (error) => {
                Debug.LogError($"✗ Save gagal: {error}");
            }
        );
    }
    
    void TestLeaderboard()
    {
        // Test 4: Get leaderboard
        PlayerDataManager.Instance.GetLeaderboard(
            top: 10,
            onSuccess: (entries) => {
                Debug.Log($"✓ Leaderboard loaded - {entries.Length} entries");
                foreach (var entry in entries)
                {
                    Debug.Log($"Rank {entry.rank}: {entry.nickname} - {entry.score} points");
                }
            },
            onError: (error) => {
                Debug.LogError($"✗ Leaderboard gagal: {error}");
            }
        );
    }
}
```

Attach script ini ke GameObject manapun dan Run!

## 📋 Checklist

Pastikan semuanya sudah setup:

- [ ] PlayerApiConfig asset sudah dibuat dan Base URL sudah diatur
- [ ] GameObject PlayerDataManager sudah ada di scene dengan component attached
- [ ] Api Config sudah di-drag ke field di PlayerDataManager component
- [ ] REST API server sudah running dan accessible
- [ ] Test script berhasil dijalankan tanpa error

## 🎮 Cara Pakai di Game Script Anda

### Contoh 1: Login saat Start Game

```csharp
void Start()
{
    PlayerDataManager.Instance.LoginOrCreatePlayer(
        "NamaPlayer",
        onSuccess: () => {
            // Lanjut ke game
        }
    );
}
```

### Contoh 2: Collect Coin

```csharp
void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Coin"))
    {
        PlayerDataManager.Instance.AddCoin(10);
        Destroy(other.gameObject);
    }
}
```

### Contoh 3: Update Score

```csharp
void OnEnemyKilled()
{
    PlayerDataManager.Instance.AddScore(50);
}
```

### Contoh 4: Save Progress

```csharp
void OnLevelComplete()
{
    PlayerDataManager.Instance.SaveToLocalAndServer(
        onSuccess: () => {
            LoadNextLevel();
        }
    );
}
```

### Contoh 5: Display Player Info

```csharp
void UpdateUI()
{
    scoreText.text = $"Score: {PlayerDataManager.Instance.Score}";
    coinText.text = $"Coins: {PlayerDataManager.Instance.Coin}";
    nameText.text = PlayerDataManager.Instance.Nickname;
}
```

## 🔍 Troubleshooting

### "Instance is null" Error
- Pastikan GameObject PlayerDataManager ada di scene
- Pastikan script PlayerDataManager sudah di-attach ke GameObject
- Pastikan Api Config sudah di-assign

### "Base URL not set" Warning
- Buka PlayerApiConfig asset
- Isi field Base Url dengan URL server Anda

### API Call Gagal (404, 500, etc)
- Pastikan REST API server Anda sudah running
- Cek Base URL sudah benar
- Cek endpoint API tersedia
- Lihat Console log untuk detail error

### Data Tidak Tersimpan
- Cek apakah `SaveToServer()` atau `SaveToLocalAndServer()` sudah dipanggil
- Cek Console untuk error message
- Verifikasi player sudah login (`IsLoggedIn == true`)

## 📚 Referensi Lengkap

Untuk dokumentasi lengkap, lihat: **README.md**

Untuk contoh lengkap, lihat:
- `Examples/PlayerApiExample.cs` - Contoh dengan UI
- `Examples/SimpleGameIntegration.cs` - Contoh integrasi game

## 🎯 API Methods Tersedia

| Method | Fungsi |
|--------|--------|
| `LoginOrCreatePlayer(nickname, onSuccess, onError)` | Login atau buat player baru |
| `CreateNewPlayer(nickname, onSuccess, onError)` | Buat player baru |
| `LoadPlayerFromServer(nickname, onSuccess, onError)` | Load player dari server |
| `AddScore(amount)` | Tambah score |
| `UpdateScore(newScore)` | Set score |
| `AddCoin(amount)` | Tambah coin |
| `UpdateCoin(newCoin)` | Set coin |
| `SpendCoin(amount)` | Kurangi coin (dengan validasi) |
| `SaveToLocal()` | Save ke PlayerPrefs |
| `SaveToServer(onSuccess, onError)` | Save ke API server |
| `SaveToLocalAndServer(onSuccess, onError)` | Save ke keduanya |
| `GetLeaderboard(top, onSuccess, onError)` | Get leaderboard |
| `Logout()` | Logout dan clear data |

## 🔥 Pro Tips

1. **Auto-Save**: SDK sudah auto-save setiap X detik, Anda tidak perlu manual save terus-menerus
2. **Local Cache**: Data tersimpan di PlayerPrefs sebagai cache, jadi player bisa main offline
3. **Error Handling**: Selalu provide callback onError untuk handle network issues
4. **Debug Logs**: Enable debug logs saat development, disable saat production
5. **Performance**: API calls adalah async, tidak block main thread

---

Selamat coding! 🚀

Jika ada pertanyaan, cek file README.md atau contoh-contoh di folder Examples.

