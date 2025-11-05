using System;
using UnityEngine;
using TMPro;
using Assets.Scripts.Player;

namespace Assets.Scripts.Core.Managers
{
    public class CoinManager : MonoBehaviour
    {
        public int CurrentCoins { get; private set; }
        public static CoinManager Instance;
        
        public event Action<int> OnCoinChanged;
        
        [SerializeField] private PlayerData playerData;
        [SerializeField] private TMP_Text coinText;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        void Start()
        {
            // Load coins from PlayerData
            if (playerData != null)
            {
                CurrentCoins = playerData.currentCoins;
            }
            
            // Update UI immediately
            UpdateCoinUI();
            
            // Subscribe to coin pickup event
            PlayerEvents.OnCoinPickup += AddCoin;
        }
        
        void OnDestroy()
        {
            // Unsubscribe from coin pickup event
            PlayerEvents.OnCoinPickup -= AddCoin;
        }

        #region Public Functions
        
        /// <summary>
        /// Adds one coin to the current coin count
        /// </summary>
        public void AddCoin()
        {
            CurrentCoins += 1;
            
            // Update UI
            UpdateCoinUI();
            
            // Invoke event for other listeners
            OnCoinChanged?.Invoke(CurrentCoins);
            
            // Auto-save to PlayerData (only ScriptableObject, not PlayerPrefs every time)
            SaveCoins();
        }
        
        /// <summary>
        /// Adds multiple coins at once
        /// </summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            
            CurrentCoins += amount;
            
            // Update UI
            UpdateCoinUI();
            
            // Invoke event for other listeners
            OnCoinChanged?.Invoke(CurrentCoins);
            
            // Auto-save to PlayerData
            SaveCoins();
        }
        
        /// <summary>
        /// Spends coins (for shop, upgrades, etc.)
        /// </summary>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return false;
            if (CurrentCoins < amount) return false;
            
            CurrentCoins -= amount;
            
            // Update UI
            UpdateCoinUI();
            
            // Invoke event for other listeners
            OnCoinChanged?.Invoke(CurrentCoins);
            
            // Auto-save to PlayerData
            SaveCoins();
            return true;
        }
        
        /// <summary>
        /// Updates the coin UI text
        /// </summary>
        private void UpdateCoinUI()
        {
            if (coinText != null)
            {
                coinText.text = CurrentCoins.ToString();
            }
        }
        
        /// <summary>
        /// Saves current coins to PlayerData (lightweight, no PlayerPrefs spam)
        /// </summary>
        public void SaveCoins()
        {
            if (playerData != null)
            {
                playerData.currentCoins = CurrentCoins;
                playerData.Save(); // Just marks as dirty, doesn't write to disk
            }
        }
        
        /// <summary>
        /// Saves coins to persistent storage (PlayerPrefs) - call on important events only
        /// </summary>
        public void SaveCoinsToPersistent()
        {
            if (playerData != null)
            {
                playerData.currentCoins = CurrentCoins;
                playerData.SaveToPersistentStorage();
            }
        }
        
        /// <summary>
        /// Resets coins to zero (for new game, etc.)
        /// </summary>
        public void ResetCoins()
        {
            CurrentCoins = 0;
            UpdateCoinUI();
            OnCoinChanged?.Invoke(CurrentCoins);
            SaveCoinsToPersistent(); // Save reset to disk
        }
        
        /// <summary>
        /// Called when application is paused/quit - saves all pending changes
        /// </summary>
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && playerData != null)
            {
                playerData.FlushSave();
            }
        }
        
        void OnApplicationQuit()
        {
            if (playerData != null)
            {
                playerData.FlushSave();
            }
        }
        
        #endregion
    }
}
