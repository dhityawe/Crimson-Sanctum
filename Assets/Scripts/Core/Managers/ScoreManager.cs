using System;
using UnityEngine;

namespace Assets.Scripts.Core.Managers
{
    public class ScoreManager : MonoBehaviour
    {
        public int Score { get; private set; }
        public int DisplayScore { get; private set; }
        public static ScoreManager Instance;
        
        public event Action<int> OnScoreChanged;
        public event Action<int> OnNextStage;
        public event Action<int> OnRecycleStage;
        
        [SerializeField] private PlayerData playerData;
        
        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        #region Public Functions
        public void AddScore()
        {
            Score += 1;
            DisplayScore = Score - 1;
            OnScoreChanged?.Invoke(Score);
            Debug.Log($"Score: {Score}");
        }

        public void SetNewFloor()
        {
            int floor = Score + 1;
            OnNextStage?.Invoke(floor);
        }

        public void RecycleFloor()
        {
            int floor = Score + 1;
            OnRecycleStage?.Invoke(floor);
        }

        public void SaveScore()
        {
            // Save to PlayerPrefs (legacy support)
            bool hasKey = PlayerPrefs.HasKey("Score");
            if (hasKey)
            {
                if (DisplayScore > PlayerPrefs.GetInt("Score"))
                {
                    PlayerPrefs.SetInt("Score", DisplayScore);
                }
            }
            else 
            {
                PlayerPrefs.SetInt("Score", DisplayScore);
            }
            
            // Save to PlayerData ScriptableObject
            if (playerData != null)
            {
                playerData.UpdateHighestScore(DisplayScore);
                playerData.IncrementRuns();
                playerData.SaveToPersistentStorage();
            }
        }
        #endregion
    }
}
