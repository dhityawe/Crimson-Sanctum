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
        public void AddScore(int value)
        {
            Score += value;
            DisplayScore = Score - 1;
            OnScoreChanged?.Invoke(Score);
            Debug.Log($"Actual Score: {Score}\nDisplay Score: {DisplayScore}");
        }

        public void SetNewFloor()
        {
            int floor = Score + 1;
            OnNextStage?.Invoke(floor);
        }

        public void SaveScore()
        {
            bool hasKey = PlayerPrefs.HasKey("Score");
            if (hasKey)
            {
                if (Score > PlayerPrefs.GetInt("Score"))
                {
                    PlayerPrefs.SetInt("Score", DisplayScore);
                }
            }
            else PlayerPrefs.SetInt("Score", DisplayScore);
        }
        #endregion
    }
}
