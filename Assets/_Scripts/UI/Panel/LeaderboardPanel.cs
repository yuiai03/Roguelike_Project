using System;
using System.Collections.Generic;
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
    private const string BackHintText = "ESC TO BACK";
    private const string DeathBackHintText = "ESC TO RESTART";

    [Header("Tham chieu trong Menu")]
    [SerializeField] private GameObject bg;
    [SerializeField] private Button hideButton;

    [Header("UI References")]
    [SerializeField] private Transform entriesContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private TextMeshProUGUI leaderboardTitleText;
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

    protected override void Awake()
    {
        base.Awake();
        RefreshStaticTexts();

        if (hideButton != null)
        {
            hideButton.onClick.AddListener(HandleHideClicked);
        }
    }

    private void OnEnable()
    {
        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.OnLeaderboardDataArrived += UpdateLeaderboardUI;
            PlayFabLeaderboardManager.Instance.OnPlayerLeaderboardDataArrived += UpdatePlayerLeaderboardUI;
        }
    }

    private void OnDisable()
    {
        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.OnLeaderboardDataArrived -= UpdateLeaderboardUI;
            PlayFabLeaderboardManager.Instance.OnPlayerLeaderboardDataArrived -= UpdatePlayerLeaderboardUI;
        }
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

        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.GetLeaderboardData();
            PlayFabLeaderboardManager.Instance.GetPlayerLeaderboardData();
        }
    }

    public void Show(bool takeInputOwnership, Action onComplete = null)
    {
        ShowInternal(takeInputOwnership, false, onComplete);
    }

    public void ShowAfterDeath(Action onComplete = null)
    {
        ShowInternal(false, true, onComplete);
    }

    private void ShowInternal(bool takeInputOwnership, bool deathMode, Action onComplete)
    {
        ownsPlayerInput = takeInputOwnership;
        isDeathMode = deathMode;
        isRestartingAfterDeath = false;
        RefreshBackHintText();

        GameUI.Instance?.InteractPanel?.Hide();
        if (ownsPlayerInput && PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(false);
        }

        bg.SetActive(true);
        if (leaderboardTitleText != null)
        {
            leaderboardTitleText.gameObject.SetActive(true);
        }

        if (backHintText != null)
        {
            backHintText.gameObject.SetActive(true);
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

    private void HideInternal(bool restoreInput, Action onComplete = null)
    {
        base.Hide(() =>
        {
            isDeathMode = false;
            isRestartingAfterDeath = false;
            RefreshBackHintText();

            bg.SetActive(false);
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
        if (leaderboardTitleText != null)
        {
            leaderboardTitleText.text = LeaderboardTitle;
        }

        RefreshBackHintText();
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

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (hideButton != null)
        {
            hideButton.onClick.RemoveListener(HandleHideClicked);
        }
    }
}
