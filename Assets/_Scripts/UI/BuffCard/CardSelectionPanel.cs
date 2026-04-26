using UnityEngine;
using System.Collections.Generic;

public class CardSelectionPanel : PanelBase
{
    [Header("UI References")]
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private GameObject cardUIPrefab;

    private List<GameObject> spawnedCardUIs = new List<GameObject>();
    private BuffCardManager cardManager;
    private List<BuffCardConfig> pendingCards;

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
        Debug.Log($"Level up to {newLevel}! Preparing card selection...");

        if (cardManager == null)
        {
            return;
        }

        List<BuffCardConfig> cards = cardManager.GetRandomCards(cardManager.GetCardsPerSelection());
        if (cards == null || cards.Count == 0)
        {
            return;
        }

        MapThemeManager mapThemeManager = MapThemeManager.Instance;
        if (mapThemeManager != null && mapThemeManager.IsTransitioning)
        {
            pendingCards = new List<BuffCardConfig>(cards);
            return;
        }

        ShowCards(cards);
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
        if (PlayerController.Instance != null) PlayerController.Instance.SetInputActive(false);

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
                Destroy(cardObj);
        }
        spawnedCardUIs.Clear();
    }

    public void OnCardSelected(BuffCardConfig card)
    {
        if (card == null || cardManager == null) return;

        Debug.Log($"Card selected: {card.cardName}");

        cardManager.ApplyCard(card);

        HideCards();
    }

    public void HideCards()
    {
        Hide(() =>
        {
            Time.timeScale = 1f;
            if (PlayerController.Instance != null) PlayerController.Instance.SetInputActive(true);
        });
    }

    private void HandleThemeTransitionCompleted(int _)
    {
        if (pendingCards == null || pendingCards.Count == 0)
        {
            return;
        }

        List<BuffCardConfig> cardsToShow = pendingCards;
        pendingCards = null;
        ShowCards(cardsToShow);
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
