using UnityEngine;
using TMPro;
using System;
using DG.Tweening;
using Roguelike.Systems.Leaderboard;

/// <summary>
/// Panel dùng để hiển thị các thông báo nhanh (ví dụ: Chào mừng người chơi).
/// Kế thừa từ PanelBase, tự động ẩn sau một khoảng thời gian.
/// </summary>
public class NotiPanel : PanelBase
{
    [Header("NotiPanel Settings")]
    [SerializeField] private TextMeshProUGUI notiText;
    [SerializeField] private float displayDuration = 2f;

    [Header("Animation Settings")]
    [Tooltip("Kéo transform chứa background + text vào đây để chạy anim trượt lên xuống.")]
    [SerializeField] private RectTransform contentRect; 
    [Tooltip("Khoảng cách trượt khởi đầu (Dương: ở trên slide xuống).")]
    [SerializeField] private float slideOffset = 150f;
    [Tooltip("Thời gian trượt xuống và trượt lên (giây).")]
    [SerializeField] private float slideDuration = 2f;

    private Vector2 originalPos;
    private Tween hideTween;

    protected override void Awake()
    {
        base.Awake();
        if (contentRect != null)
        {
            originalPos = contentRect.anchoredPosition;
        }
    }

    private void Start()
    {
        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.OnProfileLoadedEvent += OnProfileLoaded;
        }
    }

    private void OnDestroy()
    {
        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.OnProfileLoadedEvent -= OnProfileLoaded;
        }
    }

    private void OnProfileLoaded()
    {
        string displayName = PlayFabLeaderboardManager.Instance.CurrentDisplayName;
        if (!string.IsNullOrEmpty(displayName))
        {
            ShowNoti($"Welcome back, {displayName}!");
        }
    }

    public void ShowNoti(string message)
    {
        if (notiText != null)
        {
            notiText.text = message;
        }
        
        hideTween?.Kill();
        
        DOVirtual.DelayedCall(2f, () => 
        {
            Show();
            
            hideTween = DOVirtual.DelayedCall(displayDuration + slideDuration, () => 
            {
                if (IsOpen) Hide();
            }, false);
            
        }, false);
    }

    public override void Show(Action onComplete = null)
    {
        menu.SetActive(true);

        CanvasGroup canvasGroup = GetOrAddCG(gameObject);
        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;

        if (contentRect != null)
        {
            DOTween.Kill(contentRect);
            contentRect.anchoredPosition = new Vector2(originalPos.x, originalPos.y + slideOffset);
            contentRect.DOAnchorPosY(originalPos.y, slideDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        canvasGroup.DOFade(1f, slideDuration).SetUpdate(true).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    public override void Hide(Action onComplete = null)
    {
        CanvasGroup canvasGroup = GetOrAddCG(gameObject);
        DOTween.Kill(canvasGroup);
        canvasGroup.blocksRaycasts = false;

        if (contentRect != null)
        {
            DOTween.Kill(contentRect);
            contentRect.DOAnchorPosY(originalPos.y + slideOffset, slideDuration).SetEase(Ease.InCubic).SetUpdate(true);
        }

        canvasGroup.DOFade(0f, slideDuration).SetUpdate(true).OnComplete(() =>
        {
            menu.SetActive(false);
            onComplete?.Invoke();
        });
    }
}
