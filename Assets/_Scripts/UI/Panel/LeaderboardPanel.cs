using System;
using UnityEngine;
using UnityEngine.UI;
using Roguelike.Systems.Leaderboard;

/// <summary>
/// Quản lý LeaderboardPanel — có 2 obj con: bg (background) và menu (nội dung).
/// Tự quản lý tất cả tham chiếu UI bên trong panel.
/// </summary>
public class LeaderboardPanel : PanelBase
{
    [Header("Tham chiếu trong Menu")]
    [SerializeField] private GameObject bg;
    [SerializeField] private Button hideButton;

    public static Action OnClosed;

    protected override void Awake()
    {
        base.Awake();
        if (hideButton != null) hideButton.onClick.AddListener(() => Hide());
    }

    public override void Show(Action onComplete = null)
    {
        GameUI.Instance?.InteractPanel?.Hide();
        PlayerController.Instance.SetInputActive(false);

        PlayFabLeaderboardManager.Instance.GetLeaderboardData();

        bg.SetActive(true);

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

    private void OnDestroy()
    {
        if (hideButton != null) hideButton.onClick.RemoveListener(() => Hide());
    }
}
