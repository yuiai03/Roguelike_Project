using System;
using System.Collections.Generic;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;
using Roguelike.Systems.Leaderboard;
using Roguelike.UI.Leaderboard;

/// <summary>
/// Quản lý LeaderboardPanel — có 2 obj con: bg (background) và menu (nội dung).
/// Tự quản lý tất cả tham chiếu UI bên trong panel.
/// </summary>
public class LeaderboardPanel : PanelBase
{
    [Header("Tham chiếu trong Menu")]
    [SerializeField] private GameObject bg;
    [SerializeField] private Button hideButton;

    [Header("UI References")]
    [SerializeField] private Transform entriesContainer;
    [SerializeField] private GameObject entryPrefab;

    [Header("My Score UI")]
    [SerializeField] private LeaderboardEntryUI myEntryUI;

    [Header("Entry Colors")]
    public Color oddRowColor = Color.white;
    public Color evenRowColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color myEntryColor = new Color(1f, 0.9f, 0.6f, 1f);

    public static Action OnClosed;

    protected override void Awake()
    {
        base.Awake();
        if (hideButton != null) hideButton.onClick.AddListener(() => Hide());
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
        foreach (Transform child in entriesContainer)
        {
            Destroy(child.gameObject);
        }

        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.GetLeaderboardData();
            PlayFabLeaderboardManager.Instance.GetPlayerLeaderboardData();
        }
    }

    public override void Show(Action onComplete = null)
    {
        GameUI.Instance?.InteractPanel?.Hide();
        PlayerController.Instance.SetInputActive(false);

        bg.SetActive(true);

        FetchLeaderboard();

        base.Show(onComplete);
    }

    public override void Hide(Action onComplete = null)
    {
        base.Hide(() =>
        {
            bg.SetActive(false);
            PlayerController.Instance.SetInputActive(true);

            OnClosed?.Invoke();
            onComplete?.Invoke();
        });
    }

    private void UpdateLeaderboardUI(List<PlayerLeaderboardEntry> leaderboardData)
    {
        foreach (Transform child in entriesContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var entry in leaderboardData)
        {
            GameObject newEntry = Instantiate(entryPrefab, entriesContainer);
            LeaderboardEntryUI entryScript = newEntry.GetComponent<LeaderboardEntryUI>();

            if (entryScript != null)
            {
                bool isMyEntry = entry.PlayFabId == PlayFabLeaderboardManager.Instance.CurrentPlayFabId;
                entryScript.Setup(entry.Position + 1, entry.DisplayName, entry.StatValue, isMyEntry);
            }
        }
    }

    private void UpdatePlayerLeaderboardUI(PlayerLeaderboardEntry entry)
    {
        if (myEntryUI != null)
        {
            if (!myEntryUI.gameObject.activeSelf)
            {
                myEntryUI.gameObject.SetActive(true);
            }

            myEntryUI.Setup(entry.Position + 1, entry.DisplayName, entry.StatValue, true);
        }
    }

    private void OnDestroy()
    {
        if (hideButton != null) hideButton.onClick.RemoveListener(() => Hide());
    }
}
