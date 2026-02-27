using TMPro;
using UnityEngine;

namespace Roguelike.UI.Leaderboard
{
    public class LeaderboardEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;

        public void Setup(int rank, string displayName, int score)
        {
            if (rankText != null) rankText.text = $"#{rank}";
            if (nameText != null) nameText.text = string.IsNullOrEmpty(displayName) ? "Unknown Player" : displayName;
            if (scoreText != null) scoreText.text = score.ToString();
        }
    }
}
