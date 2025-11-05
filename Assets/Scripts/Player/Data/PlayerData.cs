using UnityEngine;

namespace Assets.Scripts.Core.Managers
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Crimson Sanctum/Player Data", order = 1)]
    public class PlayerData : ScriptableObject
    {
        [Header("Currency")]
        [Tooltip("Current coins the player has")]
        public int currentCoins = 0;
        
        [Header("Score")]
        [Tooltip("Highest score achieved by the player")]
        public int highestScore = 0;
        
        [Header("Statistics (Optional)")]
        [Tooltip("Total coins collected across all runs")]
        public int totalCoinsCollected = 0;
        
        [Tooltip("Total number of runs/games played")]
        public int totalRuns = 0;
        
        // Cache to prevent redundant saves
        private bool _isDirty = false;
        
        /// <summary>
        /// Updates the highest score if the new score is higher
        /// </summary>
        public void UpdateHighestScore(int newScore)
        {
            if (newScore > highestScore)
            {
                highestScore = newScore;
                _isDirty = true;
            }
        }
        
        /// <summary>
        /// Adds to total coins collected statistic
        /// </summary>
        public void AddToTotalCoins(int amount)
        {
            totalCoinsCollected += amount;
            _isDirty = true;
        }
        
        /// <summary>
        /// Increments total runs counter
        /// </summary>
        public void IncrementRuns()
        {
            totalRuns++;
            _isDirty = true;
        }
        
        /// <summary>
        /// Resets current run data (coins), but keeps persistent data (highest score, statistics)
        /// </summary>
        public void ResetCurrentRunData()
        {
            currentCoins = 0;
            _isDirty = true;
        }
        
        /// <summary>
        /// Resets all data (use for "Clear Save Data" feature)
        /// </summary>
        public void ResetAllData()
        {
            currentCoins = 0;
            highestScore = 0;
            totalCoinsCollected = 0;
            totalRuns = 0;
            _isDirty = true;
            SaveToPersistentStorage();
        }
        
        /// <summary>
        /// Saves the ScriptableObject data (editor only, lightweight)
        /// </summary>
        public void Save()
        {
            if (!_isDirty) return; // Skip if no changes
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            _isDirty = false;
        }
        
        /// <summary>
        /// Called when ScriptableObject is loaded
        /// </summary>
        private void OnEnable()
        {
            // Load from PlayerPrefs on startup
            LoadFromPlayerPrefs();
        }
        
        /// <summary>
        /// Load data from PlayerPrefs for persistent storage
        /// </summary>
        private void LoadFromPlayerPrefs()
        {
            currentCoins = PlayerPrefs.GetInt("PlayerData_Coins", 0);
            highestScore = PlayerPrefs.GetInt("PlayerData_HighestScore", 0);
            totalCoinsCollected = PlayerPrefs.GetInt("PlayerData_TotalCoins", 0);
            totalRuns = PlayerPrefs.GetInt("PlayerData_TotalRuns", 0);
        }
        
        /// <summary>
        /// Save data to PlayerPrefs for persistent storage (use sparingly)
        /// </summary>
        private void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetInt("PlayerData_Coins", currentCoins);
            PlayerPrefs.SetInt("PlayerData_HighestScore", highestScore);
            PlayerPrefs.SetInt("PlayerData_TotalCoins", totalCoinsCollected);
            PlayerPrefs.SetInt("PlayerData_TotalRuns", totalRuns);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Save to both ScriptableObject and PlayerPrefs (call only on important events)
        /// </summary>
        public void SaveToPersistentStorage()
        {
            SaveToPlayerPrefs();
            Save();
        }
        
        /// <summary>
        /// Call this periodically or on game pause/quit to batch save
        /// </summary>
        public void FlushSave()
        {
            if (_isDirty)
            {
                SaveToPersistentStorage();
            }
        }
    }
}
