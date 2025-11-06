using UnityEngine;

namespace Assets.Scripts.Player.API
{
    /// <summary>
    /// Konfigurasi untuk Player API
    /// Buat instance melalui: Assets > Create > Crimson Sanctum > Player API Config
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerApiConfig", menuName = "Crimson Sanctum/Player API Config", order = 2)]
    public class PlayerApiConfig : ScriptableObject
    {
        [Header("API Settings")]
        [Tooltip("Base URL untuk REST API (contoh: https://api.example.com)")]
        public string baseUrl = "http://localhost:5000";
        
        [Header("Auto Save Settings")]
        [Tooltip("Otomatis save ke server setiap X detik (0 = disabled)")]
        public float autoSaveInterval = 30f;
        
        [Header("Retry Settings")]
        [Tooltip("Jumlah retry jika API call gagal")]
        [Range(0, 5)]
        public int maxRetries = 3;
        
        [Tooltip("Delay antar retry dalam detik")]
        public float retryDelay = 2f;
    }
}

