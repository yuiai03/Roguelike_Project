using TMPro;
using UnityEngine;

namespace Roguelike.UI.Leaderboard
{
    public class LeaderboardEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Background Settings")]
        [SerializeField] private UnityEngine.UI.Image backgroundImage;

        public void Setup(int rank, string displayName, int score, bool isMyEntry = false)
        {
            Debug.Log($"Setup: Rank={rank}, DisplayName={displayName}, Score={score}, IsMyEntry={isMyEntry}");
            if (rankText != null) rankText.text = $"#{rank}";
            if (nameText != null) nameText.text = string.IsNullOrEmpty(displayName) ? "Unknown Player" : displayName;
            if (scoreText != null) scoreText.text = score.ToString();

            if (backgroundImage != null)
            {
                if (GameUI.Instance != null && GameUI.Instance.LeaderboardPanel != null)
                {
                    if (isMyEntry)
                    {
                        backgroundImage.color = GameUI.Instance.LeaderboardPanel.myEntryColor;
                    }
                    else if (rank % 2 == 0)
                    {
                        backgroundImage.color = GameUI.Instance.LeaderboardPanel.evenRowColor;
                    }
                    else
                    {
                        backgroundImage.color = GameUI.Instance.LeaderboardPanel.oddRowColor;
                    }
                }
            }
        }
    }
}
