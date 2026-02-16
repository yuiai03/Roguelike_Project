using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quản lý danh sách buff cards và random selection
/// </summary>
public class BuffCardManager : Singleton<BuffCardManager>
{
    [Header("Card Data")]
    [SerializeField] private List<BuffCardConfig> allCards = new List<BuffCardConfig>();

    [Header("Selection Settings")]
    [SerializeField] private int cardsPerSelection = 3;

    [Header("References")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CardSelectionUI cardSelectionUI;

    private List<BuffCardConfig> selectedCardsHistory = new List<BuffCardConfig>();

    /// <summary>
    /// Random chọn N cards từ pool
    /// </summary>
    public List<BuffCardConfig> GetRandomCards(int count)
    {
        if (allCards.Count == 0)
        {
            Debug.LogError("No cards available in the card pool!");
            return new List<BuffCardConfig>();
        }

        // Nếu số card yêu cầu nhiều hơn số card có sẵn, trả về tất cả
        if (count >= allCards.Count)
        {
            return new List<BuffCardConfig>(allCards);
        }

        // Random với weighted rarity
        List<BuffCardConfig> selectedCards = new List<BuffCardConfig>();
        List<BuffCardConfig> availableCards = new List<BuffCardConfig>(allCards);

        for (int i = 0; i < count && availableCards.Count > 0; i++)
        {
            BuffCardConfig card = GetWeightedRandomCard(availableCards);
            selectedCards.Add(card);
            availableCards.Remove(card); // Không chọn trùng trong cùng 1 lần
        }

        return selectedCards;
    }

    /// <summary>
    /// Chọn random card với xác suất dựa trên rarity
    /// </summary>
    private BuffCardConfig GetWeightedRandomCard(List<BuffCardConfig> cards)
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
    public void ApplyCard(BuffCardConfig card)
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
    public List<BuffCardConfig> GetAllCards() => allCards;
    public List<BuffCardConfig> GetSelectedHistory() => selectedCardsHistory;
    public int GetCardsPerSelection() => cardsPerSelection;

    // Public method for external access
    public void SelectCards()
    {
        List<BuffCardConfig> cards = GetRandomCards(cardsPerSelection);

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
        List<BuffCardConfig> cards = GetRandomCards(3);
        Debug.Log("=== Random 3 Cards ===");
        foreach (var card in cards)
        {
            Debug.Log($"- {card.cardName} (Rarity: {card.rarity})");
        }
    }

}
