using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Player.API
{
    /// <summary>
    /// Client untuk berkomunikasi dengan Player REST API
    /// </summary>
    public class PlayerApiClient
    {
        private readonly string _baseUrl;
        private const string CONTENT_TYPE_JSON = "application/json";
        
        public PlayerApiClient(string baseUrl)
        {
            // Ensure base URL doesn't end with slash
            _baseUrl = baseUrl.TrimEnd('/');
        }
        
        #region POST api/player/new
        
        /// <summary>
        /// Membuat player baru
        /// </summary>
        public IEnumerator CreateNewPlayer(string nickname, Action<PlayerResponse> onSuccess, Action<string> onError)
        {
            var request = new CreatePlayerRequest { nickname = nickname };
            string jsonBody = JsonUtility.ToJson(request);
            
            using (UnityWebRequest webRequest = new UnityWebRequest($"{_baseUrl}/api/player/new", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", CONTENT_TYPE_JSON);
                
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        PlayerResponse response = JsonUtility.FromJson<PlayerResponse>(webRequest.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"Failed to parse response: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Error: {webRequest.error} - {webRequest.downloadHandler.text}");
                }
            }
        }
        
        #endregion
        
        #region POST api/player/load
        
        /// <summary>
        /// Load data player berdasarkan nickname
        /// </summary>
        public IEnumerator LoadPlayer(string nickname, Action<PlayerResponse> onSuccess, Action<string> onError)
        {
            var request = new LoadPlayerRequest { nickname = nickname };
            string jsonBody = JsonUtility.ToJson(request);
            
            using (UnityWebRequest webRequest = new UnityWebRequest($"{_baseUrl}/api/player/load", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", CONTENT_TYPE_JSON);
                
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        PlayerResponse response = JsonUtility.FromJson<PlayerResponse>(webRequest.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"Failed to parse response: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Error: {webRequest.error} - {webRequest.downloadHandler.text}");
                }
            }
        }
        
        #endregion
        
        #region POST api/player/save
        
        /// <summary>
        /// Save data player ke server
        /// </summary>
        public IEnumerator SavePlayer(string playerId, int score, int coin, Action<PlayerResponse> onSuccess, Action<string> onError)
        {
            var request = new SavePlayerRequest 
            { 
                playerId = playerId,
                score = score,
                coin = coin
            };
            string jsonBody = JsonUtility.ToJson(request);
            
            using (UnityWebRequest webRequest = new UnityWebRequest($"{_baseUrl}/api/player/save", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", CONTENT_TYPE_JSON);
                
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        PlayerResponse response = JsonUtility.FromJson<PlayerResponse>(webRequest.downloadHandler.text);
                        onSuccess?.Invoke(response);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"Failed to parse response: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Error: {webRequest.error} - {webRequest.downloadHandler.text}");
                }
            }
        }
        
        #endregion
        
        #region GET api/player/leaderboard
        
        /// <summary>
        /// Mendapatkan leaderboard
        /// </summary>
        public IEnumerator GetLeaderboard(int top, Action<LeaderboardEntry[]> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{_baseUrl}/api/player/leaderboard?top={top}"))
            {
                yield return webRequest.SendWebRequest();
                
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        // Wrap array response untuk parsing dengan JsonUtility
                        string jsonArray = webRequest.downloadHandler.text;
                        string wrappedJson = $"{{\"entries\":{jsonArray}}}";
                        
                        LeaderboardResponse response = JsonUtility.FromJson<LeaderboardResponse>(wrappedJson);
                        onSuccess?.Invoke(response.entries);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"Failed to parse response: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Error: {webRequest.error} - {webRequest.downloadHandler.text}");
                }
            }
        }
        
        #endregion
    }
}

