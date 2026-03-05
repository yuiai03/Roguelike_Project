using UnityEngine;

/// <summary>
/// Singleton trung tâm quản lý tất cả các Panel trong game.
/// Các class khác truy cập panel thông qua GameUI.Instance.
/// </summary>
public class GameUI : Singleton<GameUI>
{
    [Header("Panels")]
    [SerializeField] private InteractPanel interactPanel;
    [SerializeField] private ChallengePanel challengePanel;
    [SerializeField] private LeaderboardPanel leaderboardPanel;
    [SerializeField] private NameInputPanel nameInputPanel;
    [SerializeField] private CardSelectionPanel cardSelectionPanel;
    [SerializeField] private PlayerStatsPanel playerStatsPanel;
    [SerializeField] private NotiPanel notiPanel;

    public InteractPanel InteractPanel => interactPanel;
    public ChallengePanel ChallengePanel => challengePanel;
    public LeaderboardPanel LeaderboardPanel => leaderboardPanel;
    public NameInputPanel NameInputPanel => nameInputPanel;
    public CardSelectionPanel CardSelectionPanel => cardSelectionPanel;
    public PlayerStatsPanel PlayerStatsPanel => playerStatsPanel;
    public NotiPanel NotiPanel => notiPanel;
}
