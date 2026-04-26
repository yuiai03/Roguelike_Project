using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSfx : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private AudioCue hoverCue = AudioCue.UiButtonHover;
    [SerializeField] private AudioCue clickCue = AudioCue.UiButtonClick;
    [SerializeField] private bool playHover = true;
    [SerializeField] private bool playClick = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null && playClick)
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHover || button == null || !button.interactable)
        {
            return;
        }

        AudioManager.Instance?.PlayUISfx(hoverCue);
    }

    private void PlayClick()
    {
        AudioManager.Instance?.PlayUISfx(clickCue);
    }

    private void OnDestroy()
    {
        if (button != null && playClick)
        {
            button.onClick.RemoveListener(PlayClick);
        }
    }
}
