using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CardSelectionPanel : PanelBase
{
    [Header("UI References")]
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private GameObject cardUIPrefab;

    private readonly List<GameObject> spawnedCardUIs = new List<GameObject>();
    private readonly Queue<int> queuedLevelRewards = new Queue<int>();

    private BuffCardManager cardManager;
    private bool waitingForThemeTransition;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        cardManager = BuffCardManager.Instance;

        if (cardManager == null)
        {
            Debug.LogError("BuffCardManager instance not found!");
        }

        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp.AddListener(OnPlayerLevelUp);
        }

        if (MapThemeManager.Instance != null)
        {
            MapThemeManager.Instance.OnThemeTransitionCompleted += HandleThemeTransitionCompleted;
        }
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        if (ShouldSuppressCardSelection())
        {
            Debug.Log($"Skipped card selection for level {newLevel} because leaderboard or death UI is active.");
            return;
        }

        Debug.Log($"Queued card selection for level {newLevel}.");
        queuedLevelRewards.Enqueue(newLevel);
        TryShowNextQueuedCards();
    }

    public void ShowCards(List<BuffCardConfig> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.LogError("No cards to show!");
            return;
        }

        ClearCards();

        AudioManager.Instance?.PlayUISfx(AudioCue.CardShow);
        Show();

        Time.timeScale = 0f;
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(false);
        }

        foreach (BuffCardConfig card in cards)
        {
            SpawnCardUI(card);
        }
    }

    private void SpawnCardUI(BuffCardConfig card)
    {
        if (cardUIPrefab == null || cardsContainer == null)
        {
            Debug.LogError("CardUI prefab or container not assigned!");
            return;
        }

        GameObject cardObj = Instantiate(cardUIPrefab, cardsContainer);
        BuffCardUI cardUI = cardObj.GetComponent<BuffCardUI>();

        if (cardUI != null)
        {
            int currentLevel = 0;
            int maxLevel = 0;

            if (cardManager != null)
            {
                currentLevel = cardManager.GetCardLevel(card.buffType);
                maxLevel = cardManager.GetMaxLevelForBuff(card);
            }

            cardUI.Setup(card, this, currentLevel, maxLevel);
        }

        spawnedCardUIs.Add(cardObj);
    }

    private void ClearCards()
    {
        foreach (GameObject cardObj in spawnedCardUIs)
        {
            if (cardObj != null)
            {
                Destroy(cardObj);
            }
        }

        spawnedCardUIs.Clear();
    }

    public void OnCardSelected(BuffCardConfig card)
    {
        if (card == null || cardManager == null)
        {
            return;
        }

        Debug.Log($"Card selected: {card.cardName}");

        cardManager.ApplyCard(card);
        HideCards();
    }

    public void HideCards()
    {
        Hide(() =>
        {
            ClearCards();

            if (TryShowNextQueuedCards())
            {
                return;
            }

            if (queuedLevelRewards.Count == 0)
            {
                RestoreGameplayAfterRewards();
            }
        });
    }

    public void CancelPendingSelectionsForLeaderboard()
    {
        queuedLevelRewards.Clear();
        waitingForThemeTransition = false;
        ClearCards();

        CanvasGroup canvasGroup = GetOrAddCG(gameObject);
        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (menu != null)
        {
            menu.SetActive(false);
        }
    }

    private bool TryShowNextQueuedCards()
    {
        if (ShouldSuppressCardSelection())
        {
            queuedLevelRewards.Clear();
            waitingForThemeTransition = false;
            return false;
        }

        if (IsOpen)
        {
            return false;
        }

        if (cardManager == null)
        {
            if (queuedLevelRewards.Count > 0)
            {
                Debug.LogError("BuffCardManager instance not found!");
                queuedLevelRewards.Clear();
                RestoreGameplayAfterRewards();
            }

            return false;
        }

        MapThemeManager mapThemeManager = MapThemeManager.Instance;
        if (mapThemeManager != null && mapThemeManager.IsTransitioning)
        {
            waitingForThemeTransition = queuedLevelRewards.Count > 0;
            return false;
        }

        waitingForThemeTransition = false;

        while (queuedLevelRewards.Count > 0)
        {
            int rewardLevel = queuedLevelRewards.Dequeue();
            List<BuffCardConfig> cards = cardManager.GetRandomCards(cardManager.GetCardsPerSelection());
            if (cards == null || cards.Count == 0)
            {
                Debug.LogWarning($"No cards available for level {rewardLevel}.");
                continue;
            }

            Debug.Log($"Showing queued card selection for level {rewardLevel}.");
            ShowCards(cards);
            return true;
        }

        return false;
    }

    private void RestoreGameplayAfterRewards()
    {
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead())
        {
            return;
        }

        Time.timeScale = 1f;
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(true);
        }
    }

    private bool ShouldSuppressCardSelection()
    {
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead())
        {
            return true;
        }

        LeaderboardPanel leaderboardPanel = GameUI.Instance != null ? GameUI.Instance.LeaderboardPanel : null;
        return leaderboardPanel != null && leaderboardPanel.IsOpen;
    }

    private void HandleThemeTransitionCompleted(int _)
    {
        bool wasWaitingForThemeTransition = waitingForThemeTransition;

        if (TryShowNextQueuedCards())
        {
            return;
        }

        if (wasWaitingForThemeTransition && !IsOpen && queuedLevelRewards.Count == 0)
        {
            RestoreGameplayAfterRewards();
        }

        waitingForThemeTransition = false;
    }

    private void OnDestroy()
    {
        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp.RemoveListener(OnPlayerLevelUp);
        }

        if (MapThemeManager.Instance != null)
        {
            MapThemeManager.Instance.OnThemeTransitionCompleted -= HandleThemeTransitionCompleted;
        }

        queuedLevelRewards.Clear();
        ClearCards();
    }

    [ContextMenu("Test Show Cards")]
    public void TestShowCards()
    {
        if (cardManager != null)
        {
            List<BuffCardConfig> cards = cardManager.GetRandomCards(3);
            ShowCards(cards);
        }
    }
}
