using System;
using UnityEngine;

namespace Assets.Scripts.Player.API
{
    /// <summary>
    /// Extension methods dan helper utilities untuk PlayerDataManager
    /// </summary>
    public static class PlayerDataExtensions
    {
        /// <summary>
        /// Shortcut untuk cek apakah player memiliki cukup coin
        /// </summary>
        public static bool HasEnoughCoins(this PlayerDataManager manager, int requiredAmount)
        {
            return manager.Coin >= requiredAmount;
        }
        
        /// <summary>
        /// Get formatted player info string
        /// </summary>
        public static string GetPlayerInfoString(this PlayerDataManager manager)
        {
            if (!manager.IsLoggedIn)
            {
                return "Not logged in";
            }
            
            return $"Player: {manager.Nickname}\n" +
                   $"Score: {manager.Score:N0}\n" +
                   $"Coin: {manager.Coin:N0}";
        }
        
        /// <summary>
        /// Get formatted leaderboard entry string
        /// </summary>
        public static string GetFormattedEntry(this LeaderboardEntry entry)
        {
            string medal = entry.rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"#{entry.rank}"
            };
            
            return $"{medal} {entry.nickname} - {entry.score:N0} points";
        }
        
        /// <summary>
        /// Parse DateTime dari API response string
        /// </summary>
        public static DateTime ParseApiDateTime(string dateTimeString)
        {
            if (DateTime.TryParse(dateTimeString, out DateTime result))
            {
                return result;
            }
            return DateTime.MinValue;
        }
    }
    
    /// <summary>
    /// Helper class untuk PlayerPrefs operations
    /// </summary>
    public static class PlayerPrefsHelper
    {
        private const string LAST_NICKNAME_KEY = "PlayerData_LastNickname";
        private const string LAST_LOGIN_TIME_KEY = "PlayerData_LastLoginTime";
        
        /// <summary>
        /// Save last used nickname
        /// </summary>
        public static void SaveLastNickname(string nickname)
        {
            PlayerPrefs.SetString(LAST_NICKNAME_KEY, nickname);
            PlayerPrefs.SetString(LAST_LOGIN_TIME_KEY, DateTime.Now.ToString("o"));
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Get last used nickname
        /// </summary>
        public static string GetLastNickname()
        {
            return PlayerPrefs.GetString(LAST_NICKNAME_KEY, "");
        }
        
        /// <summary>
        /// Check if should auto-login (has last nickname)
        /// </summary>
        public static bool ShouldAutoLogin()
        {
            return !string.IsNullOrEmpty(GetLastNickname());
        }
        
        /// <summary>
        /// Get last login time
        /// </summary>
        public static DateTime GetLastLoginTime()
        {
            string timeString = PlayerPrefs.GetString(LAST_LOGIN_TIME_KEY, "");
            if (DateTime.TryParse(timeString, out DateTime result))
            {
                return result;
            }
            return DateTime.MinValue;
        }
        
        /// <summary>
        /// Clear all saved login data
        /// </summary>
        public static void ClearLoginData()
        {
            PlayerPrefs.DeleteKey(LAST_NICKNAME_KEY);
            PlayerPrefs.DeleteKey(LAST_LOGIN_TIME_KEY);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// Event system untuk PlayerData changes (optional, untuk reactive UI)
    /// </summary>
    public class PlayerDataEvents
    {
        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnCoinChanged;
        public static event Action<string> OnPlayerLoggedIn;
        public static event Action OnPlayerLoggedOut;
        
        public static void TriggerScoreChanged(int newScore)
        {
            OnScoreChanged?.Invoke(newScore);
        }
        
        public static void TriggerCoinChanged(int newCoin)
        {
            OnCoinChanged?.Invoke(newCoin);
        }
        
        public static void TriggerPlayerLoggedIn(string nickname)
        {
            OnPlayerLoggedIn?.Invoke(nickname);
        }
        
        public static void TriggerPlayerLoggedOut()
        {
            OnPlayerLoggedOut?.Invoke();
        }
    }
}

