using UnityEngine;

namespace Assets.Scripts.Core.Managers
{
    public class ScoreManager : MonoBehaviour
    {
        public int Score { get; private set; }
        public int DisplayScore { get; private set; }
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
