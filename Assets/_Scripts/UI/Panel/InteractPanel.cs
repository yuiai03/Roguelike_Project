using TMPro;
using UnityEngine;

public class InteractPanel : PanelBase
{
    [SerializeField] private TextMeshProUGUI promptText;

    protected override void Awake()
    {
        base.Awake();
    }

    public void ShowText(string text)
    {
        if (!string.IsNullOrEmpty(text) && promptText != null)
            promptText.text = text;

        Show();
    }

}
