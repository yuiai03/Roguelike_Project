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
    [SerializeField] private PauseMenuPanel pauseMenuPanel;

    protected override void Awake()
    {
        base.Awake();
        ResolveMissingReferences();
    }

    public InteractPanel InteractPanel => interactPanel;
    public ChallengePanel ChallengePanel => challengePanel;
    public LeaderboardPanel LeaderboardPanel => leaderboardPanel;
    public NameInputPanel NameInputPanel => nameInputPanel;
    public CardSelectionPanel CardSelectionPanel => cardSelectionPanel;
    public PlayerStatsPanel PlayerStatsPanel => playerStatsPanel;
    public NotiPanel NotiPanel => notiPanel;
    public PauseMenuPanel PauseMenuPanel => pauseMenuPanel;

    private void ResolveMissingReferences()
    {
        if (interactPanel == null) interactPanel = GetComponentInChildren<InteractPanel>(true);
        if (challengePanel == null) challengePanel = GetComponentInChildren<ChallengePanel>(true);
        if (leaderboardPanel == null) leaderboardPanel = GetComponentInChildren<LeaderboardPanel>(true);
        if (nameInputPanel == null) nameInputPanel = GetComponentInChildren<NameInputPanel>(true);
        if (cardSelectionPanel == null) cardSelectionPanel = GetComponentInChildren<CardSelectionPanel>(true);
        if (playerStatsPanel == null) playerStatsPanel = GetComponentInChildren<PlayerStatsPanel>(true);
        if (notiPanel == null) notiPanel = GetComponentInChildren<NotiPanel>(true);
        if (pauseMenuPanel == null) pauseMenuPanel = GetComponentInChildren<PauseMenuPanel>(true);
    }
}
