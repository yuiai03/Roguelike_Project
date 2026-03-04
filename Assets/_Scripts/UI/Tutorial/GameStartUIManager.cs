using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Roguelike.Systems.Leaderboard;
using UnityEngine.Events;
using DG.Tweening;

public class GameStartUIManager : Singleton<GameStartUIManager>
{
    [Header("Đăng ký thành phần UI")]
    [SerializeField] private GameObject interactPromptPanel;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Button backgroundCloseButton;
    [SerializeField] private Button startGameButton;

    [Header("Leaderboard & Naming UI")]
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button submitNameButton;
    [SerializeField] private TextMeshProUGUI notiText;

    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Button hideLeaderboardButton;

    [Header("Cài đặt")]
    [SerializeField] private float tutorialDelayTime = 3f;
    [SerializeField] private float notiFadeDuration = 0.4f;
    [SerializeField] private float notiDisplayDuration = 2f;

    [Header("Sự kiện")]
    public UnityEvent onGameStart;

    private PreGameNPC currentNPC;
    private Coroutine tutorialWaitCoroutine;

    protected override void Awake()
    {
        base.Awake();

        backgroundCloseButton.onClick.AddListener(OnTutorialDismiss);
        startGameButton.onClick.AddListener(StartGame);
        submitNameButton.onClick.AddListener(OnSubmitNameClicked);
        hideLeaderboardButton.onClick.AddListener(HideLeaderboard);

        if (interactPromptPanel != null) interactPromptPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
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
        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.OnProfileLoadedEvent += OnPlayFabProfileLoaded;
            PlayFabLeaderboardManager.Instance.OnSubmitNameFailed += OnSubmitNameFailed_Handler;
        }
    }

    private void OnPlayFabProfileLoaded()
    {
        if (PlayFabLeaderboardManager.Instance != null &&
            string.IsNullOrEmpty(PlayFabLeaderboardManager.Instance.CurrentDisplayName))
        {
            if (PlayerController.Instance != null) PlayerController.Instance.SetInputActive(false);
            if (nameInputPanel != null) nameInputPanel.SetActive(true);
        }
    }

    private void OnSubmitNameFailed_Handler()
    {
        ShowNotification("Tên không hợp lệ hoặc đã được sử dụng. Vui lòng thử tên khác.");
    }

    public void ShowInteractPrompt(bool isVisible)
    {
        if (interactPromptPanel != null)
        {
            interactPromptPanel.SetActive(isVisible);
        }
    }

    public void ShowTutorial(PreGameNPC npc)
    {
        currentNPC = npc;
        ShowInteractPrompt(false);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(false);
        }

        if (PlayFabLeaderboardManager.Instance != null &&
            string.IsNullOrEmpty(PlayFabLeaderboardManager.Instance.CurrentDisplayName))
        {
            // Panel nhập tên có thể đã được hiện tự động sau loading, chỉ bật nếu chưa hiện
            if (nameInputPanel != null && !nameInputPanel.activeSelf)
            {
                nameInputPanel.SetActive(true);
            }
        }
        else
        {
            ProceedToTutorialUI();
        }
    }

    private void OnSubmitNameClicked()
    {
        string nameInput = nameInputField != null ? nameInputField.text.Trim() : "";

        PlayFabLeaderboardManager.Instance.SubmitName(nameInput,
        onFailed: () =>
        {
            ShowNotification("Tên không hợp lệ hoặc đã được sử dụng. Vui lòng thử lại.");
        },
        onSuccess: () =>
        {
            if (nameInputPanel != null) nameInputPanel.SetActive(false);
            if (PlayerController.Instance != null) PlayerController.Instance.SetInputActive(true);
        });

    }

    private void ShowNotification(string message)
    {
        if (notiText == null) return;

        notiText.text = message;
        notiText.gameObject.SetActive(true);

        DOTween.Kill(notiText);
        notiText.DOFade(1f, notiFadeDuration)
            .From(0f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                DOVirtual.DelayedCall(notiDisplayDuration, () =>
                {
                    notiText.DOFade(0f, notiFadeDuration)
                        .SetUpdate(true)
                        .OnComplete(() => notiText.gameObject.SetActive(false));
                }, ignoreTimeScale: true);
            });
    }

    private void ProceedToTutorialUI()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);

        if (tutorialWaitCoroutine != null) StopCoroutine(tutorialWaitCoroutine);
        tutorialWaitCoroutine = StartCoroutine(WaitAndShowStartButton());
    }

    private IEnumerator WaitAndShowStartButton()
    {
        yield return new WaitForSeconds(tutorialDelayTime);
        OnTutorialDismiss();
    }

    public void OnTutorialDismiss()
    {

        if (tutorialWaitCoroutine != null)
        {
            StopCoroutine(tutorialWaitCoroutine);
            tutorialWaitCoroutine = null;
        }

        if (startGameButton != null) startGameButton.gameObject.SetActive(true);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(true);
        }
    }

    public void StartGame()
    {

        if (leaderboardPanel != null && leaderboardPanel.activeSelf)
            return;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(true);
        }

        if (WaveSpawner.Instance != null)
        {
            onGameStart?.Invoke();
            WaveSpawner.Instance.StartNextWave();
        }
        else
        {
            Debug.LogError("WaveSpawner Instance không tìm thấy để bắt đầu trò chơi!");
        }

        if (currentNPC != null)
        {
            currentNPC.Disappear();
        }

        gameObject.SetActive(false);
    }

    #region Leaderboard UI
    public void ShowLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetInputActive(false);
            }

            if (PlayFabLeaderboardManager.Instance != null)
            {
                PlayFabLeaderboardManager.Instance.GetLeaderboardData();
            }
        }
    }

    public void HideLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);

            if (PlayerController.Instance != null && (tutorialPanel == null || !tutorialPanel.activeSelf) && (nameInputPanel == null || !nameInputPanel.activeSelf))
            {
                PlayerController.Instance.SetInputActive(true);
            }
        }
    }
    #endregion

    private void OnDestroy()
    {
        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.OnProfileLoadedEvent -= OnPlayFabProfileLoaded;
            PlayFabLeaderboardManager.Instance.OnSubmitNameFailed -= OnSubmitNameFailed_Handler;
        }

        if (backgroundCloseButton != null) backgroundCloseButton.onClick.RemoveListener(OnTutorialDismiss);
        if (startGameButton != null) startGameButton.onClick.RemoveListener(StartGame);
        if (submitNameButton != null) submitNameButton.onClick.RemoveListener(OnSubmitNameClicked);
        if (hideLeaderboardButton != null) hideLeaderboardButton.onClick.RemoveListener(HideLeaderboard);
    }
}
