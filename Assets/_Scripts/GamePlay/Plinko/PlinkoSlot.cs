using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlinkoSlot : MonoBehaviour
{
    [SerializeField] private int rewardValue;
    [SerializeField] private TextMeshPro label;

    private PlinkoGameController controller;

    public int RewardValue => rewardValue;

    private void Reset()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        EnsureLabel();
    }

    public void Initialize(PlinkoGameController owner, int value)
    {
        controller = owner;
        rewardValue = value;
        EnsureLabel();

        if (label != null)
        {
            label.text = rewardValue.ToString();
        }
    }

    public void SetLabel(TextMeshPro valueLabel)
    {
        label = valueLabel;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlinkoBall ball = other.GetComponent<PlinkoBall>();
        if (ball == null || !ball.IsLockedToSlot(transform) || !ball.TryResolve())
        {
            return;
        }

        if (controller == null)
        {
            controller = GetComponentInParent<PlinkoGameController>();
        }

        controller?.HandleBallScored(ball.LockedRewardValue, ball.gameObject);
    }

    private void EnsureLabel()
    {
        if (label != null)
        {
            return;
        }

        label = GetComponentInChildren<TextMeshPro>(true);
        if (label != null)
        {
            return;
        }

        GameObject labelObject = new GameObject("ValueLabel");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, -0.05f, -0.1f);
        labelObject.transform.localScale = Vector3.one * 0.18f;

        label = labelObject.AddComponent<TextMeshPro>();
        label.fontSize = 7f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.NoWrap;
    }
}
