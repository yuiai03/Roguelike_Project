using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị 1 card trong UI selection
/// </summary>
public class BuffCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private Button selectButton;

    private BuffCard currentCard;
    private CardSelectionUI parentUI;

    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnCardSelected);
        }
    }

    public void Setup(BuffCard card, CardSelectionUI parent)
    {
        currentCard = card;
        parentUI = parent;

        if (card == null) return;

        // Set card info
        if (nameText != null)
            nameText.text = card.cardName;

        if (descriptionText != null)
            descriptionText.text = card.GetFormattedDescription();

        if (iconImage != null && card.icon != null)
            iconImage.sprite = card.icon;

        // Set rarity color
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
