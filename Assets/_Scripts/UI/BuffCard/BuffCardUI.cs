using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Button selectButton;

    private BuffCardConfig currentCard;
    private CardSelectionPanel parentUI;

    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnCardSelected);
        }
    }

    public void Setup(BuffCardConfig card, CardSelectionPanel parent, int currentLevel, int maxLevel)
    {
        currentCard = card;
        parentUI = parent;

        if (card == null) return;

        if (nameText != null)
        {
            if (maxLevel > 0)
                nameText.text = $"{card.cardName} ({currentLevel}/{maxLevel})";
            else
                nameText.text = $"{card.cardName} (Lv. {currentLevel + 1})";
        }

        if (descriptionText != null)
            descriptionText.text = card.GetFormattedDescription(currentLevel);

        if (iconImage != null && card.icon != null)
            iconImage.sprite = card.icon;

        Color rarityColor = card.GetRarityColor();

        if (cardBackground != null)
            cardBackground.color = rarityColor;

        if (rarityText != null)
        {
            rarityText.text = card.GetRarityName();
            rarityText.color = rarityColor;
        }
    }

    private void OnCardSelected()
    {
        if (currentCard != null && parentUI != null)
        {
            parentUI.OnCardSelected(currentCard);
        }
    }
}
