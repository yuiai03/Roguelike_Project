using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý UI chọn card khi level up
/// </summary>
public class CardSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private GameObject cardUIPrefab;

    [Header("Settings")]
    [SerializeField] private bool pauseGameOnShow = true;

    private List<GameObject> spawnedCardUIs = new List<GameObject>();
    private BuffCardManager cardManager;

    private void Awake()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }

    private void Start()
    {
        cardManager = BuffCardManager.Instance;

        if (cardManager == null)
        {
            Debug.LogError("BuffCardManager instance not found!");
        }

        // Subscribe to level up event
        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp.AddListener(OnPlayerLevelUp);
        }
    }

    private void OnPlayerLevelUp(int newLevel)
    {
        Debug.Log($"Level up to {newLevel}! Showing card selection...");

        if (cardManager != null)
        {
            List<BuffCard> cards = cardManager.GetRandomCards(cardManager.GetCardsPerSelection());
            ShowCards(cards);
        }
    }

    /// <summary>
    /// Hiển thị danh sách cards để chọn
    /// </summary>
    public void ShowCards(List<BuffCard> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.LogError("No cards to show!");
            return;
        }

        // Clear previous cards
        ClearCards();

        // Show panel
        if (selectionPanel != null)
            selectionPanel.SetActive(true);

        // Pause game
        if (pauseGameOnShow)
            Time.timeScale = 0f;

        // Spawn card UIs
        foreach (BuffCard card in cards)
        {
            SpawnCardUI(card);
        }
    }

    /// <summary>
    /// Spawn 1 card UI
    /// </summary>
    private void SpawnCardUI(BuffCard card)
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
            cardUI.Setup(card, this);
        }

        spawnedCardUIs.Add(cardObj);
    }

    /// <summary>
    /// Xóa tất cả card UIs hiện tại
    /// </summary>
    private void ClearCards()
    {
        foreach (GameObject cardObj in spawnedCardUIs)
        {
            if (cardObj != null)
                Destroy(cardObj);
        }
        spawnedCardUIs.Clear();
    }

    /// <summary>
    /// Callback khi player chọn 1 card
    /// </summary>
    public void OnCardSelected(BuffCard card)
    {
        if (card == null || cardManager == null) return;

        Debug.Log($"Card selected: {card.cardName}");

        // Apply buff
        cardManager.ApplyCard(card);

        // Hide UI and resume game
        HideCards();
    }

    /// <summary>
    /// Ẩn UI và resume game
    /// </summary>
    public void HideCards()
    {
        // Clear cards
        ClearCards();

        // Hide panel
        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        // Resume game
        if (pauseGameOnShow)
            Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        // Unsubscribe
        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp.RemoveListener(OnPlayerLevelUp);
        }

        // Clear cards
        ClearCards();
    }

    // Public test method
    [ContextMenu("Test Show Cards")]
    public void TestShowCards()
    {
        if (cardManager != null)
        {
            List<BuffCard> cards = cardManager.GetRandomCards(3);
            ShowCards(cards);
        }
    }
}
