using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CardSelectionPanel : PanelBase
{
    [Header("UI References")]
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private GameObject cardUIPrefab;

    private readonly List<GameObject> spawnedCardUIs = new List<GameObject>();
    private readonly Queue<int> queuedLevelRewards = new Queue<int>();

    private BuffCardManager cardManager;
    private PlayerLevelSystem boundLevelSystem;
    private MapThemeManager boundMapThemeManager;
    private bool waitingForThemeTransition;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        BindRuntimeReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindRuntimeReferences();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindRuntimeReferences();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        queuedLevelRewards.Clear();
        waitingForThemeTransition = false;
        ClearCards();
        HideImmediate();
        BindRuntimeReferences();
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        BindRuntimeReferences();

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
        BindRuntimeReferences();

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
        BindRuntimeReferences();

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
        BindRuntimeReferences();

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
        BindRuntimeReferences();

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
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindRuntimeReferences();

        queuedLevelRewards.Clear();
        ClearCards();
    }

    [ContextMenu("Test Show Cards")]
    public void TestShowCards()
    {
        BindRuntimeReferences();

        if (cardManager != null)
        {
            List<BuffCardConfig> cards = cardManager.GetRandomCards(3);
            ShowCards(cards);
        }
    }

    private void BindRuntimeReferences()
    {
        BuffCardManager currentCardManager = BuffCardManager.Instance;
        if (cardManager != currentCardManager)
        {
            cardManager = currentCardManager;
        }

        PlayerLevelSystem currentLevelSystem = PlayerLevelSystem.Instance;
        if (boundLevelSystem != currentLevelSystem)
        {
            if (boundLevelSystem != null)
            {
                boundLevelSystem.OnLevelUp.RemoveListener(OnPlayerLevelUp);
            }

            boundLevelSystem = currentLevelSystem;

            if (boundLevelSystem != null)
            {
                boundLevelSystem.OnLevelUp.RemoveListener(OnPlayerLevelUp);
                boundLevelSystem.OnLevelUp.AddListener(OnPlayerLevelUp);
            }
        }

        MapThemeManager currentMapThemeManager = MapThemeManager.Instance;
        if (boundMapThemeManager != currentMapThemeManager)
        {
            if (boundMapThemeManager != null)
            {
                boundMapThemeManager.OnThemeTransitionCompleted -= HandleThemeTransitionCompleted;
            }

            boundMapThemeManager = currentMapThemeManager;

            if (boundMapThemeManager != null)
            {
                boundMapThemeManager.OnThemeTransitionCompleted -= HandleThemeTransitionCompleted;
                boundMapThemeManager.OnThemeTransitionCompleted += HandleThemeTransitionCompleted;
            }
        }
    }

    private void UnbindRuntimeReferences()
    {
        if (boundLevelSystem != null)
        {
            boundLevelSystem.OnLevelUp.RemoveListener(OnPlayerLevelUp);
            boundLevelSystem = null;
        }

        if (boundMapThemeManager != null)
        {
            boundMapThemeManager.OnThemeTransitionCompleted -= HandleThemeTransitionCompleted;
            boundMapThemeManager = null;
        }

        cardManager = null;
    }
}
