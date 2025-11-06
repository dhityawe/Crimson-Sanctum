using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Player.API
{
    /// <summary>
    /// Manager utama untuk mengelola data player - menggabungkan local storage (PlayerPrefs) dan remote storage (API)
    /// Singleton pattern untuk akses mudah dari script manapun
    /// </summary>
    public class PlayerDataManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlayerApiConfig apiConfig;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        // Singleton instance
        public static PlayerDataManager Instance { get; private set; }
        
        // API Client
        private PlayerApiClient _apiClient;
        
        // Player data yang sedang aktif
        private string _currentPlayerId;
        private string _currentNickname;
        private int _currentScore;
        private int _currentCoin;
        
        // Keys untuk PlayerPrefs
        private const string PREF_PLAYER_ID = "PlayerData_PlayerId";
        private const string PREF_NICKNAME = "PlayerData_Nickname";
        private const string PREF_SCORE = "PlayerData_Score";
        private const string PREF_COIN = "PlayerData_Coin";
        
        // Auto save timer
        private float _autoSaveTimer;
        
        // Public properties untuk akses data
        public string PlayerId => _currentPlayerId;
        public string Nickname => _currentNickname;
        public int Score => _currentScore;
        public int Coin => _currentCoin;
        public bool IsLoggedIn => !string.IsNullOrEmpty(_currentPlayerId);
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Setup singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            // Auto save logic
            if (apiConfig != null && apiConfig.autoSaveInterval > 0 && IsLoggedIn)
            {
                _autoSaveTimer += Time.deltaTime;
                if (_autoSaveTimer >= apiConfig.autoSaveInterval)
                {
                    _autoSaveTimer = 0f;
                    SaveToServer();
                }
            }
        }
        
        private void OnApplicationPause(bool pause)
        {
            if (pause && IsLoggedIn)
            {
                SaveToLocalAndServer();
            }
        }
        
        private void OnApplicationQuit()
        {
            if (IsLoggedIn)
            {
                SaveToLocal();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void Initialize()
        {
            if (apiConfig == null)
            {
                LogError("PlayerApiConfig belum di-assign! Buat config di Assets > Create > Crimson Sanctum > Player API Config");
                return;
            }
            
            _apiClient = new PlayerApiClient(apiConfig.baseUrl);
            LoadFromLocal();
            Log("PlayerDataManager initialized");
        }
        
        #endregion
        
        #region Local Storage (PlayerPrefs)
        
        /// <summary>
        /// Load data dari PlayerPrefs
        /// </summary>
        public void LoadFromLocal()
        {
            _currentPlayerId = PlayerPrefs.GetString(PREF_PLAYER_ID, "");
            _currentNickname = PlayerPrefs.GetString(PREF_NICKNAME, "");
            _currentScore = PlayerPrefs.GetInt(PREF_SCORE, 0);
            _currentCoin = PlayerPrefs.GetInt(PREF_COIN, 0);
            
            Log($"Loaded from local - Nickname: {_currentNickname}, Score: {_currentScore}, Coin: {_currentCoin}");
        }
        
        /// <summary>
        /// Save data ke PlayerPrefs
        /// </summary>
        public void SaveToLocal()
        {
            PlayerPrefs.SetString(PREF_PLAYER_ID, _currentPlayerId);
            PlayerPrefs.SetString(PREF_NICKNAME, _currentNickname);
            PlayerPrefs.SetInt(PREF_SCORE, _currentScore);
            PlayerPrefs.SetInt(PREF_COIN, _currentCoin);
            PlayerPrefs.Save();
            
            Log($"Saved to local - Nickname: {_currentNickname}, Score: {_currentScore}, Coin: {_currentCoin}");
        }
        
        /// <summary>
        /// Hapus semua data lokal
        /// </summary>
        public void ClearLocalData()
        {
            PlayerPrefs.DeleteKey(PREF_PLAYER_ID);
            PlayerPrefs.DeleteKey(PREF_NICKNAME);
            PlayerPrefs.DeleteKey(PREF_SCORE);
            PlayerPrefs.DeleteKey(PREF_COIN);
            PlayerPrefs.Save();
            
            _currentPlayerId = "";
            _currentNickname = "";
            _currentScore = 0;
            _currentCoin = 0;
            
            Log("Local data cleared");
        }
        
        #endregion
        
        #region API Calls
        
        /// <summary>
        /// Membuat player baru di server
        /// </summary>
        public void CreateNewPlayer(string nickname, Action onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(CreateNewPlayerCoroutine(nickname, onSuccess, onError));
        }
        
        private IEnumerator CreateNewPlayerCoroutine(string nickname, Action onSuccess, Action<string> onError)
        {
            Log($"Creating new player with nickname: {nickname}");
            
            yield return _apiClient.CreateNewPlayer(
                nickname,
                response =>
                {
                    // Update local data
                    _currentPlayerId = response.playerId;
                    _currentNickname = response.nickname;
                    _currentScore = response.score;
                    _currentCoin = response.coin;
                    
                    // Save to PlayerPrefs
                    SaveToLocal();
                    
                    Log($"Player created successfully - ID: {_currentPlayerId}");
                    onSuccess?.Invoke();
                },
                error =>
                {
                    LogError($"Failed to create player: {error}");
                    onError?.Invoke(error);
                }
            );
        }
        
        /// <summary>
        /// Load player dari server berdasarkan nickname
        /// </summary>
        public void LoadPlayerFromServer(string nickname, Action onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(LoadPlayerCoroutine(nickname, onSuccess, onError));
        }
        
        private IEnumerator LoadPlayerCoroutine(string nickname, Action onSuccess, Action<string> onError)
        {
            Log($"Loading player with nickname: {nickname}");
            
            yield return _apiClient.LoadPlayer(
                nickname,
                response =>
                {
                    // Update local data
                    _currentPlayerId = response.playerId;
                    _currentNickname = response.nickname;
                    _currentScore = response.score;
                    _currentCoin = response.coin;
                    
                    // Save to PlayerPrefs
                    SaveToLocal();
                    
                    Log($"Player loaded successfully - ID: {_currentPlayerId}, Score: {_currentScore}, Coin: {_currentCoin}");
                    onSuccess?.Invoke();
                },
                error =>
                {
                    LogError($"Failed to load player: {error}");
                    onError?.Invoke(error);
                }
            );
        }
        
        /// <summary>
        /// Save current player data ke server
        /// </summary>
        public void SaveToServer(Action onSuccess = null, Action<string> onError = null)
        {
            if (!IsLoggedIn)
            {
                LogError("Cannot save to server: No player logged in");
                onError?.Invoke("No player logged in");
                return;
            }
            
            StartCoroutine(SavePlayerCoroutine(onSuccess, onError));
        }
        
        private IEnumerator SavePlayerCoroutine(Action onSuccess, Action<string> onError)
        {
            Log($"Saving player to server - Score: {_currentScore}, Coin: {_currentCoin}");
            
            yield return _apiClient.SavePlayer(
                _currentPlayerId,
                _currentScore,
                _currentCoin,
                response =>
                {
                    // Sync dengan response dari server (jika ada perubahan)
                    _currentScore = response.score;
                    _currentCoin = response.coin;
                    
                    Log("Player saved successfully to server");
                    onSuccess?.Invoke();
                },
                error =>
                {
                    LogError($"Failed to save player: {error}");
                    onError?.Invoke(error);
                }
            );
        }
        
        /// <summary>
        /// Get leaderboard dari server
        /// </summary>
        public void GetLeaderboard(int top, Action<LeaderboardEntry[]> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(GetLeaderboardCoroutine(top, onSuccess, onError));
        }
        
        private IEnumerator GetLeaderboardCoroutine(int top, Action<LeaderboardEntry[]> onSuccess, Action<string> onError)
        {
            Log($"Fetching leaderboard top {top}");
            
            yield return _apiClient.GetLeaderboard(
                top,
                entries =>
                {
                    Log($"Leaderboard fetched successfully - {entries.Length} entries");
                    onSuccess?.Invoke(entries);
                },
                error =>
                {
                    LogError($"Failed to fetch leaderboard: {error}");
                    onError?.Invoke(error);
                }
            );
        }
        
        #endregion
        
        #region Data Manipulation
        
        /// <summary>
        /// Update score (hanya local, belum save ke server)
        /// </summary>
        public void UpdateScore(int newScore)
        {
            _currentScore = newScore;
            Log($"Score updated to: {_currentScore}");
        }
        
        /// <summary>
        /// Add score (hanya local, belum save ke server)
        /// </summary>
        public void AddScore(int amount)
        {
            _currentScore += amount;
            Log($"Score increased by {amount}, new score: {_currentScore}");
        }
        
        /// <summary>
        /// Update coin (hanya local, belum save ke server)
        /// </summary>
        public void UpdateCoin(int newCoin)
        {
            _currentCoin = newCoin;
            Log($"Coin updated to: {_currentCoin}");
        }
        
        /// <summary>
        /// Add coin (hanya local, belum save ke server)
        /// </summary>
        public void AddCoin(int amount)
        {
            _currentCoin += amount;
            Log($"Coin increased by {amount}, new coin: {_currentCoin}");
        }
        
        /// <summary>
        /// Spend coin (hanya local, belum save ke server)
        /// </summary>
        public bool SpendCoin(int amount)
        {
            if (_currentCoin >= amount)
            {
                _currentCoin -= amount;
                Log($"Coin decreased by {amount}, new coin: {_currentCoin}");
                return true;
            }
            
            LogError($"Insufficient coins. Required: {amount}, Available: {_currentCoin}");
            return false;
        }
        
        #endregion
        
        #region Convenience Methods
        
        /// <summary>
        /// Save to both local and server
        /// </summary>
        public void SaveToLocalAndServer(Action onSuccess = null, Action<string> onError = null)
        {
            SaveToLocal();
            SaveToServer(onSuccess, onError);
        }
        
        /// <summary>
        /// Login player: coba load dari server, jika gagal buat baru
        /// </summary>
        public void LoginOrCreatePlayer(string nickname, Action onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(LoginOrCreatePlayerCoroutine(nickname, onSuccess, onError));
        }
        
        private IEnumerator LoginOrCreatePlayerCoroutine(string nickname, Action onSuccess, Action<string> onError)
        {
            Log($"Attempting to login or create player: {nickname}");
            
            bool loadSuccess = false;
            bool loadComplete = false;
            
            // Coba load dulu
            yield return _apiClient.LoadPlayer(
                nickname,
                response =>
                {
                    _currentPlayerId = response.playerId;
                    _currentNickname = response.nickname;
                    _currentScore = response.score;
                    _currentCoin = response.coin;
                    SaveToLocal();
                    loadSuccess = true;
                    loadComplete = true;
                    Log("Player loaded successfully");
                },
                error =>
                {
                    loadSuccess = false;
                    loadComplete = true;
                    Log("Player not found, will create new player");
                }
            );
            
            // Tunggu sampai load selesai
            yield return new WaitUntil(() => loadComplete);
            
            if (loadSuccess)
            {
                onSuccess?.Invoke();
            }
            else
            {
                // Jika load gagal, buat player baru
                yield return _apiClient.CreateNewPlayer(
                    nickname,
                    response =>
                    {
                        _currentPlayerId = response.playerId;
                        _currentNickname = response.nickname;
                        _currentScore = response.score;
                        _currentCoin = response.coin;
                        SaveToLocal();
                        Log("New player created successfully");
                        onSuccess?.Invoke();
                    },
                    error =>
                    {
                        LogError($"Failed to create new player: {error}");
                        onError?.Invoke(error);
                    }
                );
            }
        }
        
        /// <summary>
        /// Logout player (clear data)
        /// </summary>
        public void Logout()
        {
            ClearLocalData();
            Log("Player logged out");
        }
        
        #endregion
        
        #region Logging
        
        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerDataManager] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[PlayerDataManager] {message}");
        }
        
        #endregion
    }
}

