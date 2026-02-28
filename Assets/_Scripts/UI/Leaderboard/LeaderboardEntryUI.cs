using TMPro;
using UnityEngine;

namespace Roguelike.UI.Leaderboard
{
    public class LeaderboardEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Background Sprites")]
        [SerializeField] private UnityEngine.UI.Image backgroundImage;
        [SerializeField] private Sprite oddRowSprite;
        [SerializeField] private Sprite evenRowSprite;
        [SerializeField] private Sprite myEntrySprite;

        public void Setup(int rank, string displayName, int score, bool isMyEntry = false)
        {
            if (rankText != null) rankText.text = $"#{rank}";
            if (nameText != null) nameText.text = string.IsNullOrEmpty(displayName) ? "Unknown Player" : displayName;
            if (scoreText != null) scoreText.text = score.ToString();

            if (backgroundImage != null)
            {
                if (isMyEntry)
                {
                    if (myEntrySprite != null) backgroundImage.sprite = myEntrySprite;
                }
                else
                {
                    if (rank % 2 == 0)
                    {
                        if (evenRowSprite != null) backgroundImage.sprite = evenRowSprite;
                    }
                    else
                    {
                        if (oddRowSprite != null) backgroundImage.sprite = oddRowSprite;
                    }
                }
            }
        }
    }
}
