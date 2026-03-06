using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BuffCardManager : Singleton<BuffCardManager>
{
    [Header("Card Data")]
    [SerializeField] private List<BuffCardConfig> allCards = new List<BuffCardConfig>();

    [Header("Selection Settings")]
    [SerializeField] private int cardsPerSelection = 3;

    [Header("References")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CardSelectionPanel cardSelectionUI;

    private List<BuffCardConfig> selectedCardsHistory = new List<BuffCardConfig>();
    private Dictionary<BuffType, int> cardLevels = new Dictionary<BuffType, int>();

    public List<BuffCardConfig> GetRandomCards(int count)
    {
        if (allCards.Count == 0)
        {
            Debug.LogError("No cards available in the card pool!");
            return new List<BuffCardConfig>();
        }

        if (count >= allCards.Count)
        {
            return new List<BuffCardConfig>(allCards);
        }

        List<BuffCardConfig> selectedCards = new List<BuffCardConfig>();
        List<BuffCardConfig> availableCards = new List<BuffCardConfig>();

        foreach (var c in allCards)
        {
            int currentLevel = GetCardLevel(c.buffType);
            int maxLevel = GetMaxLevelForBuff(c);
            
            if (maxLevel == 0 || currentLevel < maxLevel)
            {
                availableCards.Add(c);
            }
        }

        if (count >= availableCards.Count)
        {
            return new List<BuffCardConfig>(availableCards);
        }

        for (int i = 0; i < count && availableCards.Count > 0; i++)
        {
            BuffCardConfig card = GetWeightedRandomCard(availableCards);
            selectedCards.Add(card);
            availableCards.Remove(card);
        }

        return selectedCards;
    }

    private BuffCardConfig GetWeightedRandomCard(List<BuffCardConfig> cards)
    {
        float luck = (playerData != null) ? playerData.luckBonus : 0f;

        float totalWeight = 0f;
        foreach (var card in cards)
        {
            float weight = Utils.GetRarityWeight(card.rarity, luck);
            totalWeight += weight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var card in cards)
        {
            float weight = Utils.GetRarityWeight(card.rarity, luck);
            currentWeight += weight;

            if (randomValue <= currentWeight)
            {
                return card;
            }
        }

        return cards[cards.Count - 1];
    }

    public void ApplyCard(BuffCardConfig card)
    {
        if (card == null)
        {
            Debug.LogError("Trying to apply null card!");
            return;
        }

        card.ApplyBuff(playerData, playerHealth);
        selectedCardsHistory.Add(card);

        if (cardLevels.ContainsKey(card.buffType))
        {
            cardLevels[card.buffType]++;
        }
        else
        {
            cardLevels[card.buffType] = 1;
        }

        Debug.Log($"Applied card: {card.cardName} (Level {cardLevels[card.buffType]})");
    }

    public int GetCardLevel(BuffType type)
    {
        return cardLevels.ContainsKey(type) ? cardLevels[type] : 0;
    }

    public int GetMaxLevelForBuff(BuffCardConfig card)
    {
        return card.maxLevel;
    }

    public List<BuffCardConfig> GetAllCards() => allCards;
    public List<BuffCardConfig> GetSelectedHistory() => selectedCardsHistory;
    public int GetCardsPerSelection() => cardsPerSelection;

    public void SelectCards()
    {
        List<BuffCardConfig> cards = GetRandomCards(cardsPerSelection);

        if (cardSelectionUI != null)
        {
            cardSelectionUI.ShowCards(cards);
        }
        else
        {
            Debug.LogError("CardSelectionPanel not found in scene!");
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
