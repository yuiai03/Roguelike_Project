using System;
using System.Collections.Generic;
using DG.Tweening;
using PlayFab.ClientModels;
using Roguelike.Systems.Leaderboard;
using Roguelike.UI.Leaderboard;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quan ly LeaderboardPanel va cac tham chieu UI ben trong panel.
/// </summary>
public class LeaderboardPanel : PanelBase
{
    private const string LeaderboardTitle = "LEADERBOARD";
    private const string LeaderboardNote = "b\u1ea3ng x\u1ebfp h\u1ea1ng \u0111\u01b0\u1ee3c t\u00ednh theo t\u1ed5ng \u0111i\u1ec3m kinh nghi\u1ec7m b\u1ea1n nh\u1eadn \u0111\u01b0\u1ee3c";
    private const string BackHintText = "ESC TO BACK";
    private const string DeathBackHintText = "ESC TO RESTART";

    [Header("Tham chieu trong Menu")]
    [SerializeField] private GameObject bg;
    [SerializeField] private Button hideButton;

    [Header("UI References")]
    [SerializeField] private Transform entriesContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private TextMeshProUGUI leaderboardTitleText;
    [SerializeField] private TextMeshProUGUI leaderboardNoteText;
    [SerializeField] private TextMeshProUGUI backHintText;

    [Header("My Score UI")]
    [SerializeField] private LeaderboardEntryUI myEntryUI;

    [Header("Reveal Animation")]
    [SerializeField] private float entryFadeDuration = 0.25f;
    [SerializeField] private float entryFadeStagger = 0.05f;
    [SerializeField] private float myEntryFadeDelay = 0.12f;

    [Header("Entry Colors")]
    public Color oddRowColor = Color.white;
    public Color evenRowColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color myEntryColor = new Color(1f, 0.9f, 0.6f, 1f);

    public static Action OnClosed;

    private bool ownsPlayerInput = true;
    private bool isDeathMode;
    private bool isRestartingAfterDeath;
    private PlayFabLeaderboardManager subscribedLeaderboardManager;

    protected override void Awake()
    {
        base.Awake();
        EnsureLeaderboardNoteText();
        RefreshStaticTexts();

        if (hideButton != null)
        {
            hideButton.onClick.AddListener(HandleHideClicked);
        }
    }

    private void OnEnable()
    {
        SubscribeToCurrentLeaderboardManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromLeaderboardManager();
    }

    private void Update()
    {
        if (!isDeathMode || !IsOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            RestartRunAfterDeath();
        }
    }

    public void FetchLeaderboard()
    {
        ClearEntries();

        if (myEntryUI != null)
        {
            myEntryUI.HideInstant();
            myEntryUI.gameObject.SetActive(false);
        }

        PlayFabLeaderboardManager leaderboardManager = SubscribeToCurrentLeaderboardManager();
        if (leaderboardManager != null)
        {
            leaderboardManager.GetLeaderboardData();
            leaderboardManager.GetPlayerLeaderboardData();
        }
    }

    public void Show(bool takeInputOwnership, Action onComplete = null)
    {
        ShowInternal(takeInputOwnership, false, onComplete);
    }

    public void ShowAfterDeath(Action onComplete = null)
    {
        GameUI.Instance?.CardSelectionPanel?.CancelPendingSelectionsForLeaderboard();
        ShowInternal(false, true, onComplete);
    }

    private void ShowInternal(bool takeInputOwnership, bool deathMode, Action onComplete)
    {
        ownsPlayerInput = takeInputOwnership;
        isDeathMode = deathMode;
        isRestartingAfterDeath = false;
        RefreshBackHintText();
        PrepareVisualsForShow();

        GameUI.Instance?.InteractPanel?.Hide();
        if (ownsPlayerInput && PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(false);
        }

        AudioManager.Instance?.PlayUISfx(AudioCue.UiLeaderboardOpen);

        FetchLeaderboard();

        base.Show(onComplete);
    }

    public override void Show(Action onComplete = null)
    {
        Show(true, onComplete);
    }

    public void HideWithoutInputRestore(Action onComplete = null)
    {
        HideInternal(false, onComplete);
    }

    public override void Hide(Action onComplete = null)
    {
        HideInternal(ownsPlayerInput, onComplete);
    }

    public void ForceHideForSceneReload()
    {
        CanvasGroup canvasGroup = GetOrAddCG(gameObject);
        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        isDeathMode = false;
        isRestartingAfterDeath = false;
        ownsPlayerInput = false;

        ClearEntries();
        if (myEntryUI != null)
        {
            myEntryUI.HideInstant();
            myEntryUI.gameObject.SetActive(false);
        }

        if (menu != null)
        {
            menu.SetActive(false);
        }

        if (bg != null)
        {
            bg.SetActive(false);
        }

        if (leaderboardTitleText != null)
        {
            leaderboardTitleText.gameObject.SetActive(false);
        }

        if (leaderboardNoteText != null)
        {
            leaderboardNoteText.gameObject.SetActive(false);
        }

        RefreshBackHintText();
    }

    private void HideInternal(bool restoreInput, Action onComplete = null)
    {
        base.Hide(() =>
        {
            isDeathMode = false;
            isRestartingAfterDeath = false;
            RefreshBackHintText();

            if (bg != null)
            {
                bg.SetActive(false);
            }

            if (leaderboardNoteText != null)
            {
                leaderboardNoteText.gameObject.SetActive(false);
            }

            if (backHintText != null)
            {
                backHintText.gameObject.SetActive(false);
            }

            AudioManager.Instance?.PlayUISfx(AudioCue.UiLeaderboardClose);
            if (restoreInput && PlayerController.Instance != null)
            {
                PlayerController.Instance.SetInputActive(true);
            }

            OnClosed?.Invoke();
            onComplete?.Invoke();
        });
    }

    private void HandleHideClicked()
    {
        if (isDeathMode)
        {
            RestartRunAfterDeath();
            return;
        }

        Hide();
    }

    private PlayFabLeaderboardManager SubscribeToCurrentLeaderboardManager()
    {
        PlayFabLeaderboardManager leaderboardManager = PlayFabLeaderboardManager.Instance;
        if (subscribedLeaderboardManager == leaderboardManager)
        {
            return subscribedLeaderboardManager;
        }

        UnsubscribeFromLeaderboardManager();

        subscribedLeaderboardManager = leaderboardManager;
        if (subscribedLeaderboardManager != null)
        {
            subscribedLeaderboardManager.OnLeaderboardDataArrived += UpdateLeaderboardUI;
            subscribedLeaderboardManager.OnPlayerLeaderboardDataArrived += UpdatePlayerLeaderboardUI;
        }

        return subscribedLeaderboardManager;
    }

    private void UnsubscribeFromLeaderboardManager()
    {
        if (subscribedLeaderboardManager != null)
        {
            subscribedLeaderboardManager.OnLeaderboardDataArrived -= UpdateLeaderboardUI;
            subscribedLeaderboardManager.OnPlayerLeaderboardDataArrived -= UpdatePlayerLeaderboardUI;
        }

        subscribedLeaderboardManager = null;
    }

    private void PrepareVisualsForShow()
    {
        CanvasGroup canvasGroup = GetOrAddCG(gameObject);
        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        if (menu != null)
        {
            menu.SetActive(true);
        }

        if (bg != null)
        {
            bg.SetActive(true);
        }

        if (leaderboardTitleText != null)
        {
            leaderboardTitleText.gameObject.SetActive(true);
        }

        if (leaderboardNoteText != null)
        {
            leaderboardNoteText.gameObject.SetActive(true);
        }

        if (backHintText != null)
        {
            backHintText.gameObject.SetActive(true);
        }
    }

    private void UpdateLeaderboardUI(List<PlayerLeaderboardEntry> leaderboardData)
    {
        ClearEntries();

        for (int i = 0; i < leaderboardData.Count; i++)
        {
            PlayerLeaderboardEntry entry = leaderboardData[i];
            GameObject newEntry = Instantiate(entryPrefab, entriesContainer);
            LeaderboardEntryUI entryScript = newEntry.GetComponent<LeaderboardEntryUI>();

            if (entryScript == null)
            {
                continue;
            }

            bool highlightAsMyEntry = PlayFabLeaderboardManager.Instance != null &&
                entry.PlayFabId == PlayFabLeaderboardManager.Instance.CurrentPlayFabId;
            entryScript.Setup(entry.Position + 1, entry.DisplayName, entry.StatValue, highlightAsMyEntry);
            entryScript.ShowAnimated(i * entryFadeStagger, entryFadeDuration);
        }
    }

    private void UpdatePlayerLeaderboardUI(PlayerLeaderboardEntry entry)
    {
        if (myEntryUI == null || entry == null)
        {
            return;
        }

        if (!myEntryUI.gameObject.activeSelf)
        {
            myEntryUI.gameObject.SetActive(true);
        }

        myEntryUI.Setup(entry.Position + 1, entry.DisplayName, entry.StatValue, true);
        myEntryUI.ShowAnimated(myEntryFadeDelay, entryFadeDuration);
    }

    private void ClearEntries()
    {
        if (entriesContainer == null)
        {
            return;
        }

        foreach (Transform child in entriesContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void RefreshStaticTexts()
    {
        EnsureLeaderboardNoteText();

        if (leaderboardTitleText != null)
        {
            leaderboardTitleText.text = LeaderboardTitle;
        }

        if (leaderboardNoteText != null)
        {
            leaderboardNoteText.text = LeaderboardNote;
        }

        RefreshBackHintText();
    }

    private void EnsureLeaderboardNoteText()
    {
        if (leaderboardNoteText != null || leaderboardTitleText == null)
        {
            return;
        }

        GameObject noteObject = Instantiate(leaderboardTitleText.gameObject, leaderboardTitleText.transform.parent);
        noteObject.name = "LeaderboardNoteText";
        noteObject.transform.SetSiblingIndex(leaderboardTitleText.transform.GetSiblingIndex() + 1);

        leaderboardNoteText = noteObject.GetComponent<TextMeshProUGUI>();
        if (leaderboardNoteText == null)
        {
            return;
        }

        RectTransform titleRect = leaderboardTitleText.rectTransform;
        RectTransform noteRect = leaderboardNoteText.rectTransform;
        noteRect.anchorMin = titleRect.anchorMin;
        noteRect.anchorMax = titleRect.anchorMax;
        noteRect.pivot = titleRect.pivot;
        noteRect.anchoredPosition = titleRect.anchoredPosition + new Vector2(0f, -34f);
        noteRect.sizeDelta = new Vector2(Mathf.Max(titleRect.sizeDelta.x, 560f), 36f);

        leaderboardNoteText.fontSize = Mathf.Max(12f, leaderboardTitleText.fontSize * 0.45f);
        leaderboardNoteText.fontStyle = FontStyles.Normal;
        leaderboardNoteText.alignment = TextAlignmentOptions.Center;
        leaderboardNoteText.enableWordWrapping = true;
        leaderboardNoteText.raycastTarget = false;

        Color noteColor = leaderboardTitleText.color;
        noteColor.a = Mathf.Min(noteColor.a, 0.85f);
        leaderboardNoteText.color = noteColor;
    }

    private void RefreshBackHintText()
    {
        if (backHintText != null)
        {
            backHintText.text = isDeathMode ? DeathBackHintText : BackHintText;
            backHintText.gameObject.SetActive(false);
        }
    }

    private void RestartRunAfterDeath()
    {
        if (isRestartingAfterDeath)
        {
            return;
        }

        isRestartingAfterDeath = true;
        Time.timeScale = 1f;

        if (LoadingUIManager.Instance != null)
        {
            LoadingUIManager.Instance.ShowBlackFadeAndRestart();
            return;
        }

        GameUI.Instance?.PrepareForSceneReload();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        UnsubscribeFromLeaderboardManager();

        if (hideButton != null)
        {
            hideButton.onClick.RemoveListener(HandleHideClicked);
        }
    }
}
