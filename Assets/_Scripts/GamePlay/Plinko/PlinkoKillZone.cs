using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlinkoKillZone : MonoBehaviour
{
    private PlinkoGameController controller;

    public void Initialize(PlinkoGameController owner)
    {
        controller = owner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlinkoBall ball = other.GetComponent<PlinkoBall>();
        if (ball == null || !ball.TryResolve())
        {
            return;
        }

        if (controller == null)
        {
            controller = GetComponentInParent<PlinkoGameController>();
        }

        controller?.RemoveBall(ball.gameObject);
    }
}
