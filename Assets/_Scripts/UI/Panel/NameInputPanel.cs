using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Roguelike.Systems.Leaderboard;

/// <summary>
/// Panel nhập tên người chơi (PlayFab display name).
/// Singleton — tự quản lý toàn bộ UI bên trong.
/// </summary>
public class NameInputPanel : PanelBase
{
    [Header("Tham chiếu trong Panel")]
    [SerializeField] private GameObject bg;
    [SerializeField] private Button submitNameButton;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TextMeshProUGUI notiText;

    [Header("Cài đặt")]
    [SerializeField] private float notiFadeDuration = 0.4f;
    [SerializeField] private float notiDisplayDuration = 2f;

    public static Action OnNameSubmitted;

    protected override void Awake()
    {
        base.Awake();

        if (submitNameButton != null) submitNameButton.onClick.AddListener(OnSubmitClicked);
        if (notiText != null)
        {
            notiText.gameObject.SetActive(false);
            Color c = notiText.color;
            c.a = 0f;
            notiText.color = c;
        }
    }

    private void Start()
    {
        PlayFabLeaderboardManager.Instance.OnProfileLoadedEvent += OnProfileLoaded;
        PlayFabLeaderboardManager.Instance.OnSubmitNameFailed += OnSubmitFailed;
    }

    public override void Show(Action onComplete = null)
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetInputActive(false);

        bg.SetActive(true);
        base.Show(onComplete);
    }

    public override void Hide(Action onComplete = null)
    {
        base.Hide(() =>
        {
            bg.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private void OnProfileLoaded()
    {
        if (PlayFabLeaderboardManager.Instance != null &&
            string.IsNullOrEmpty(PlayFabLeaderboardManager.Instance.CurrentDisplayName))
        {
            Show();
        }
    }

    private void OnSubmitClicked()
    {
        string name = nameInputField != null ? nameInputField.text.Trim() : "";

        PlayFabLeaderboardManager.Instance.SubmitName(name,
            onFailed: () => ShowNotification("Tên không hợp lệ hoặc đã được sử dụng. Vui lòng thử lại."),
            onSuccess: () =>
            {
                Hide();
                if (PlayerController.Instance != null)
                    PlayerController.Instance.SetInputActive(true);
                OnNameSubmitted?.Invoke();
            });
    }

    private void OnSubmitFailed() =>
        ShowNotification("Tên không hợp lệ hoặc đã được sử dụng. Vui lòng thử tên khác.");

    private void ShowNotification(string message)
    {
        if (notiText == null) return;
        notiText.text = message;
        notiText.gameObject.SetActive(true);
        DOTween.Kill(notiText);
        notiText.DOFade(1f, notiFadeDuration).From(0f).SetUpdate(true).OnComplete(() =>
            DOVirtual.DelayedCall(notiDisplayDuration, () =>
                notiText.DOFade(0f, notiFadeDuration).SetUpdate(true)
                    .OnComplete(() => notiText.gameObject.SetActive(false)),
                ignoreTimeScale: true));
    }

    private void OnDestroy()
    {
        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.OnProfileLoadedEvent -= OnProfileLoaded;
            PlayFabLeaderboardManager.Instance.OnSubmitNameFailed -= OnSubmitFailed;
        }
    }
}
