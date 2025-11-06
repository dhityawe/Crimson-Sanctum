using UnityEngine;

namespace Assets.Scripts.Player.API.Examples
{
    /// <summary>
    /// Contoh integrasi sederhana untuk game script Anda
    /// Copy-paste method yang diperlukan ke script game Anda
    /// </summary>
    public class SimpleGameIntegration : MonoBehaviour
    {
        // ========================================
        // CONTOH 1: Login saat game start
        // ========================================
        
        void LoginExample()
        {
            string savedNickname = PlayerPrefs.GetString("PlayerNickname", "");
            
            if (string.IsNullOrEmpty(savedNickname))
            {
                // Tampilkan UI untuk input nickname
                ShowNicknameInputUI();
            }
            else
            {
                // Auto-login dengan nickname tersimpan
                PlayerDataManager.Instance.LoginOrCreatePlayer(
                    savedNickname,
                    onSuccess: () => {
                        Debug.Log("Auto-login berhasil!");
                        StartGame();
                    },
                    onError: (error) => {
                        Debug.LogError($"Auto-login gagal: {error}");
                        ShowNicknameInputUI();
                    }
                );
            }
        }
        
        void OnNicknameSubmitted(string nickname)
        {
            // Simpan nickname untuk auto-login berikutnya
            PlayerPrefs.SetString("PlayerNickname", nickname);
            PlayerPrefs.Save();
            
            // Login atau buat player baru
            PlayerDataManager.Instance.LoginOrCreatePlayer(
                nickname,
                onSuccess: () => {
                    Debug.Log("Login berhasil!");
                    StartGame();
                },
                onError: (error) => {
                    Debug.LogError($"Login gagal: {error}");
                }
            );
        }
        
        // ========================================
        // CONTOH 2: Collect coins di gameplay
        // ========================================
        
        void OnCoinPickup(int coinValue)
        {
            // Tambah coin ke player data
            PlayerDataManager.Instance.AddCoin(coinValue);
            
            // Update UI (ambil nilai terbaru)
            UpdateCoinUI(PlayerDataManager.Instance.Coin);
            
            // Auto-save akan berjalan otomatis sesuai interval
        }
        
        // ========================================
        // CONTOH 3: Update score saat game over
        // ========================================
        
        void OnGameOver(int finalScore)
        {
            // Cek apakah ini high score baru
            int currentHighScore = PlayerDataManager.Instance.Score;
            bool isNewHighScore = finalScore > currentHighScore;
            
            if (isNewHighScore)
            {
                PlayerDataManager.Instance.UpdateScore(finalScore);
                ShowNewHighScoreMessage();
            }
            
            // Save ke server
            PlayerDataManager.Instance.SaveToLocalAndServer(
                onSuccess: () => {
                    Debug.Log("Progress saved!");
                    ShowGameOverScreen(finalScore, isNewHighScore);
                },
                onError: (error) => {
                    Debug.LogWarning($"Gagal save ke server: {error}");
                    // Tetap tampilkan game over screen meski save gagal
                    ShowGameOverScreen(finalScore, isNewHighScore);
                }
            );
        }
        
        // ========================================
        // CONTOH 4: Beli item dengan coin
        // ========================================
        
        void OnPurchaseItem(int itemCost)
        {
            // Cek dan kurangi coin
            bool purchaseSuccess = PlayerDataManager.Instance.SpendCoin(itemCost);
            
            if (purchaseSuccess)
            {
                Debug.Log($"Item dibeli! Sisa coin: {PlayerDataManager.Instance.Coin}");
                
                // Unlock item
                UnlockItem();
                
                // Update UI
                UpdateCoinUI(PlayerDataManager.Instance.Coin);
                
                // Save progress
                PlayerDataManager.Instance.SaveToLocalAndServer();
            }
            else
            {
                Debug.Log("Coin tidak cukup!");
                ShowInsufficientCoinsMessage();
            }
        }
        
        // ========================================
        // CONTOH 5: Tampilkan leaderboard
        // ========================================
        
        void ShowLeaderboardScreen()
        {
            // Tampilkan loading
            ShowLoading(true);
            
            // Fetch leaderboard
            PlayerDataManager.Instance.GetLeaderboard(
                top: 20,
                onSuccess: (entries) => {
                    ShowLoading(false);
                    
                    // Display leaderboard entries
                    foreach (var entry in entries)
                    {
                        Debug.Log($"Rank {entry.rank}: {entry.nickname} - Score: {entry.score}");
                        // Tambahkan ke UI leaderboard
                        AddLeaderboardEntryToUI(entry);
                    }
                },
                onError: (error) => {
                    ShowLoading(false);
                    Debug.LogError($"Gagal load leaderboard: {error}");
                    ShowErrorMessage("Tidak bisa memuat leaderboard");
                }
            );
        }
        
        // ========================================
        // CONTOH 6: Display player info di UI
        // ========================================
        
        void UpdatePlayerUI()
        {
            if (PlayerDataManager.Instance.IsLoggedIn)
            {
                string nickname = PlayerDataManager.Instance.Nickname;
                int score = PlayerDataManager.Instance.Score;
                int coin = PlayerDataManager.Instance.Coin;
                
                // Update UI text
                Debug.Log($"Player: {nickname} | Score: {score} | Coin: {coin}");
                // UpdateUIText(nickname, score, coin);
            }
        }
        
        // ========================================
        // CONTOH 7: Manual save progress
        // ========================================
        
        void OnSaveButtonClicked()
        {
            if (!PlayerDataManager.Instance.IsLoggedIn)
            {
                Debug.LogWarning("Player belum login!");
                return;
            }
            
            ShowSavingIndicator(true);
            
            PlayerDataManager.Instance.SaveToLocalAndServer(
                onSuccess: () => {
                    ShowSavingIndicator(false);
                    ShowMessage("Progress tersimpan!");
                },
                onError: (error) => {
                    ShowSavingIndicator(false);
                    ShowMessage($"Gagal menyimpan: {error}");
                }
            );
        }
        
        // ========================================
        // CONTOH 8: Logout
        // ========================================
        
        void OnLogoutButtonClicked()
        {
            // Clear saved nickname
            PlayerPrefs.DeleteKey("PlayerNickname");
            PlayerPrefs.Save();
            
            // Logout dari manager
            PlayerDataManager.Instance.Logout();
            
            // Kembali ke login screen
            LoadLoginScene();
        }
        
        // ========================================
        // DUMMY METHODS (ganti dengan implementasi asli Anda)
        // ========================================
        
        private void ShowNicknameInputUI() { }
        private void StartGame() { }
        private void UpdateCoinUI(int coins) { }
        private void ShowNewHighScoreMessage() { }
        private void ShowGameOverScreen(int score, bool isHighScore) { }
        private void UnlockItem() { }
        private void ShowInsufficientCoinsMessage() { }
        private void ShowLoading(bool show) { }
        private void AddLeaderboardEntryToUI(LeaderboardEntry entry) { }
        private void ShowErrorMessage(string message) { }
        private void ShowSavingIndicator(bool show) { }
        private void ShowMessage(string message) { }
        private void LoadLoginScene() { }
    }
}

