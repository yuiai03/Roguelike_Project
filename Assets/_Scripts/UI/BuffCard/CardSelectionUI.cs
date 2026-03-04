using UnityEngine;
using System.Collections.Generic;

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
            List<BuffCardConfig> cards = cardManager.GetRandomCards(cardManager.GetCardsPerSelection());
            ShowCards(cards);
        }
    }

    public void ShowCards(List<BuffCardConfig> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.LogError("No cards to show!");
            return;
        }

        ClearCards();

        if (selectionPanel != null)
            selectionPanel.SetActive(true);

        if (pauseGameOnShow)
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
            cardUI.Setup(card, this);
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

        ClearCards();

        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (pauseGameOnShow)
            Time.timeScale = 1f;
            
        if (PlayerController.Instance != null) PlayerController.Instance.SetInputActive(true);
    }

    private void OnDestroy()
    {

        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem != null)
        {
            levelSystem.OnLevelUp.RemoveListener(OnPlayerLevelUp);
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
