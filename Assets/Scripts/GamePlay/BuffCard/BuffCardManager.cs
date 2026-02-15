using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quản lý danh sách buff cards và random selection
/// </summary>
public class BuffCardManager : Singleton<BuffCardManager>
{
    [Header("Card Data")]
    [SerializeField] private List<BuffCard> allCards = new List<BuffCard>();

    [Header("Selection Settings")]
    [SerializeField] private int cardsPerSelection = 3;

    [Header("References")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CardSelectionUI cardSelectionUI;

    private List<BuffCard> selectedCardsHistory = new List<BuffCard>();

    /// <summary>
    /// Random chọn N cards từ pool
    /// </summary>
    public List<BuffCard> GetRandomCards(int count)
    {
        if (allCards.Count == 0)
        {
            Debug.LogError("No cards available in the card pool!");
            return new List<BuffCard>();
        }

        // Nếu số card yêu cầu nhiều hơn số card có sẵn, trả về tất cả
        if (count >= allCards.Count)
        {
            return new List<BuffCard>(allCards);
        }

        // Random với weighted rarity
        List<BuffCard> selectedCards = new List<BuffCard>();
        List<BuffCard> availableCards = new List<BuffCard>(allCards);

        for (int i = 0; i < count && availableCards.Count > 0; i++)
        {
            BuffCard card = GetWeightedRandomCard(availableCards);
            selectedCards.Add(card);
            availableCards.Remove(card); // Không chọn trùng trong cùng 1 lần
        }

        return selectedCards;
    }

    /// <summary>
    /// Chọn random card với xác suất dựa trên rarity
    /// </summary>
    private BuffCard GetWeightedRandomCard(List<BuffCard> cards)
    {
        // Tính tổng weight (rarity càng cao càng hiếm)
        float totalWeight = 0f;
        foreach (var card in cards)
        {
            float weight = Utils.GetRarityWeight(card.rarity);
            totalWeight += weight;
        }

        // Random pick
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var card in cards)
        {
            float weight = Utils.GetRarityWeight(card.rarity);
            currentWeight += weight;

            if (randomValue <= currentWeight)
            {
                return card;
            }
        }

        // Fallback
        return cards[cards.Count - 1];
    }

    /// <summary>
    /// Apply card buff vào player
    /// </summary>
    public void ApplyCard(BuffCard card)
    {
        if (card == null)
        {
            Debug.LogError("Trying to apply null card!");
            return;
        }

        card.ApplyBuff(playerData, playerHealth);
        selectedCardsHistory.Add(card);

        Debug.Log($"Applied card: {card.cardName}");
    }

    // Getters
    public List<BuffCard> GetAllCards() => allCards;
    public List<BuffCard> GetSelectedHistory() => selectedCardsHistory;
    public int GetCardsPerSelection() => cardsPerSelection;

    // Public method for external access
    public void SelectCards()
    {
        List<BuffCard> cards = GetRandomCards(cardsPerSelection);

        if (cardSelectionUI != null)
        {
            cardSelectionUI.ShowCards(cards);
        }
        else
        {
            Debug.LogError("CardSelectionUI not found in scene!");
        }
    }

    [ContextMenu("Test Random 3 Cards")]
    private void TestRandomCards()
    {
        List<BuffCard> cards = GetRandomCards(3);
        Debug.Log("=== Random 3 Cards ===");
        foreach (var card in cards)
        {
            Debug.Log($"- {card.cardName} (Rarity: {card.rarity})");
        }
    }

}
