using System;
using Assets.Scripts.Core.Managers;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI
{
    public class ScoreUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _scoreText;

        private void Start()
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScoreUI;
        }

        private void UpdateScoreUI(int obj)
        {
            _scoreText.SetText($"{obj}");
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
