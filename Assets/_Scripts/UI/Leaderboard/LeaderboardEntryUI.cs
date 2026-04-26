using DG.Tweening;
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

        private CanvasGroup canvasGroup;
        private Tween revealTween;

        public void Setup(int rank, string displayName, int score, bool highlightAsMyEntry = false)
        {
            if (rankText != null) rankText.text = $"#{rank}";
            if (nameText != null) nameText.text = string.IsNullOrEmpty(displayName) ? "Unknown Player" : displayName;
            if (scoreText != null) scoreText.text = score.ToString();

            if (backgroundImage == null || GameUI.Instance == null || GameUI.Instance.LeaderboardPanel == null)
            {
                return;
            }

            if (highlightAsMyEntry)
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

        public void HideInstant()
        {
            CanvasGroup group = EnsureCanvasGroup();
            revealTween?.Kill();
            group.alpha = 0f;
        }

        public void ShowAnimated(float delay = 0f, float duration = 0.25f)
        {
            CanvasGroup group = EnsureCanvasGroup();
            revealTween?.Kill();
            group.alpha = 0f;
            revealTween = group
                .DOFade(1f, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private CanvasGroup EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            return canvasGroup;
        }

        private void OnDisable()
        {
            revealTween?.Kill();
        }

        private void OnDestroy()
        {
            revealTween?.Kill();
        }
    }
}
