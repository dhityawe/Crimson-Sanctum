using System;

namespace Assets.Scripts.Player.API
{
    #region Request DTOs
    
    [Serializable]
    public class CreatePlayerRequest
    {
        public string nickname;
    }
    
    [Serializable]
    public class LoadPlayerRequest
    {
        public string nickname;
    }
    
    [Serializable]
    public class SavePlayerRequest
    {
        public string playerId;
        public int score;
        public int coin;
    }
    
    #endregion
    
    #region Response DTOs
    
    [Serializable]
    public class PlayerResponse
    {
        public string playerId;
        public string nickname;
        public int score;
        public int coin;
        public string createdAt;
        public string updatedAt;
    }
    
    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string nickname;
        public int score;
        public int coin;
        public string createdAt;
        public string updatedAt;
    }
    
    [Serializable]
    public class LeaderboardResponse
    {
        public LeaderboardEntry[] entries;
    }
    
    #endregion
}

