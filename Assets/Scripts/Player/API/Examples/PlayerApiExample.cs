using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Player.API.Examples
{
    /// <summary>
    /// Contoh penggunaan Player API SDK
    /// Attach script ini ke GameObject dan hubungkan UI elements
    /// </summary>
    public class PlayerApiExample : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private InputField nicknameInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button addScoreButton;
        [SerializeField] private Button addCoinButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button logoutButton;
        
        [Header("Display")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text playerInfoText;
        [SerializeField] private Text leaderboardText;
        
        private void Start()
        {
            // Setup button listeners
            if (loginButton) loginButton.onClick.AddListener(OnLoginClicked);
            if (createButton) createButton.onClick.AddListener(OnCreateClicked);
            if (saveButton) saveButton.onClick.AddListener(OnSaveClicked);
            if (addScoreButton) addScoreButton.onClick.AddListener(OnAddScoreClicked);
            if (addCoinButton) addCoinButton.onClick.AddListener(OnAddCoinClicked);
            if (leaderboardButton) leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            if (logoutButton) logoutButton.onClick.AddListener(OnLogoutClicked);
            
            UpdateUI();
        }
        
        private void Update()
        {
            UpdateUI();
        }
        
        #region Button Handlers
        
        private void OnLoginClicked()
        {
            string nickname = nicknameInput ? nicknameInput.text : "TestPlayer";
            
            if (string.IsNullOrEmpty(nickname))
            {
                UpdateStatus("Nickname tidak boleh kosong!");
                return;
            }
            
            UpdateStatus("Loading player...");
            
            PlayerDataManager.Instance.LoadPlayerFromServer(
                nickname,
                onSuccess: () =>
                {
                    UpdateStatus($"Berhasil login sebagai {nickname}!");
                },
                onError: (error) =>
                {
                    UpdateStatus($"Gagal login: {error}");
                }
            );
        }
        
        private void OnCreateClicked()
        {
            string nickname = nicknameInput ? nicknameInput.text : "TestPlayer";
            
            if (string.IsNullOrEmpty(nickname))
            {
                UpdateStatus("Nickname tidak boleh kosong!");
                return;
            }
            
            UpdateStatus("Creating new player...");
            
            PlayerDataManager.Instance.CreateNewPlayer(
                nickname,
                onSuccess: () =>
                {
                    UpdateStatus($"Player baru berhasil dibuat: {nickname}!");
                },
                onError: (error) =>
                {
                    UpdateStatus($"Gagal membuat player: {error}");
                }
            );
        }
        
        private void OnSaveClicked()
        {
            if (!PlayerDataManager.Instance.IsLoggedIn)
            {
                UpdateStatus("Anda belum login!");
                return;
            }
            
            UpdateStatus("Saving to server...");
            
            PlayerDataManager.Instance.SaveToLocalAndServer(
                onSuccess: () =>
                {
                    UpdateStatus("Data berhasil disimpan!");
                },
                onError: (error) =>
                {
                    UpdateStatus($"Gagal menyimpan: {error}");
                }
            );
        }
        
        private void OnAddScoreClicked()
        {
            if (!PlayerDataManager.Instance.IsLoggedIn)
            {
                UpdateStatus("Anda belum login!");
                return;
            }
            
            PlayerDataManager.Instance.AddScore(100);
            UpdateStatus("Score +100");
        }
        
        private void OnAddCoinClicked()
        {
            if (!PlayerDataManager.Instance.IsLoggedIn)
            {
                UpdateStatus("Anda belum login!");
                return;
            }
            
            PlayerDataManager.Instance.AddCoin(50);
            UpdateStatus("Coin +50");
        }
        
        private void OnLeaderboardClicked()
        {
            UpdateStatus("Loading leaderboard...");
            
            PlayerDataManager.Instance.GetLeaderboard(
                top: 10,
                onSuccess: (entries) =>
                {
                    DisplayLeaderboard(entries);
                    UpdateStatus($"Leaderboard loaded - {entries.Length} entries");
                },
                onError: (error) =>
                {
                    UpdateStatus($"Gagal load leaderboard: {error}");
                }
            );
        }
        
        private void OnLogoutClicked()
        {
            PlayerDataManager.Instance.Logout();
            UpdateStatus("Logged out");
        }
        
        #endregion
        
        #region UI Updates
        
        private void UpdateUI()
        {
            if (playerInfoText)
            {
                if (PlayerDataManager.Instance.IsLoggedIn)
                {
                    playerInfoText.text = $"Player: {PlayerDataManager.Instance.Nickname}\n" +
                                         $"ID: {PlayerDataManager.Instance.PlayerId}\n" +
                                         $"Score: {PlayerDataManager.Instance.Score}\n" +
                                         $"Coin: {PlayerDataManager.Instance.Coin}";
                }
                else
                {
                    playerInfoText.text = "Not logged in";
                }
            }
        }
        
        private void UpdateStatus(string message)
        {
            if (statusText)
            {
                statusText.text = message;
            }
            Debug.Log($"[PlayerApiExample] {message}");
        }
        
        private void DisplayLeaderboard(LeaderboardEntry[] entries)
        {
            if (leaderboardText)
            {
                string text = "=== LEADERBOARD ===\n\n";
                
                foreach (var entry in entries)
                {
                    text += $"#{entry.rank} - {entry.nickname}\n";
                    text += $"Score: {entry.score} | Coin: {entry.coin}\n\n";
                }
                
                leaderboardText.text = text;
            }
        }
        
        #endregion
    }
}

