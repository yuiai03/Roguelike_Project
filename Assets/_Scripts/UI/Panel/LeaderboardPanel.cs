using System;
using System.Collections.Generic;
using PlayFab.ClientModels;
using Roguelike.Systems.Leaderboard;
using Roguelike.UI.Leaderboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quan ly LeaderboardPanel va cac tham chieu UI ben trong panel.
/// </summary>
public class LeaderboardPanel : PanelBase
{
    private const string LeaderboardTitle = "LEADERBOARD";

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

    protected override void Awake()
    {
        base.Awake();
        EnsureLeaderboardTitle();
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
        ownsPlayerInput = takeInputOwnership;
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
        if (myEntryUI == null)
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

        if (backHintText != null)
        {
            backHintText.text = "ESC TO BACK";
            backHintText.gameObject.SetActive(false);
        }
    }

    private void EnsureLeaderboardTitle()
    {
        if (menu == null)
        {
            return;
        }

        if (leaderboardTitleText == null)
        {
            Transform existingTitle = menu.transform.Find("LeaderboardTitle");
            if (existingTitle != null)
            {
                leaderboardTitleText = existingTitle.GetComponent<TextMeshProUGUI>();
            }
        }

        if (leaderboardTitleText != null)
        {
            return;
        }

        GameObject titleObject = new GameObject("LeaderboardTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(menu.transform, false);

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -8f);
        titleRect.sizeDelta = new Vector2(-100f, 42f);

        leaderboardTitleText = titleObject.GetComponent<TextMeshProUGUI>();
        leaderboardTitleText.fontSize = 32f;
        leaderboardTitleText.fontStyle = FontStyles.Bold;
        leaderboardTitleText.alignment = TextAlignmentOptions.Center;
        leaderboardTitleText.raycastTarget = false;

        if (backHintText != null)
        {
            if (backHintText.font != null)
            {
                leaderboardTitleText.font = backHintText.font;
            }

            leaderboardTitleText.color = backHintText.color;
        }
        else
        {
            leaderboardTitleText.color = Color.white;
        }
    }

    private void OnDestroy()
    {
        if (hideButton != null)
        {
            hideButton.onClick.RemoveListener(HandleHideClicked);
        }
    }
}
