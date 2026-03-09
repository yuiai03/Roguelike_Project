using UnityEngine;

public class ChestBuffBox : NPC
{
    private PoolType poolType;

    protected override void Awake()
    {
        base.Awake();
        poolType = GetComponent<PoolTypeConfig>()?.poolType ?? PoolType.BuffChest;
    }

    protected override bool CanShowPrompt()
    {
        return !IsPanelOpen();
    }

    protected override bool IsPanelOpen()
    {
        return GameUI.Instance?.CardSelectionPanel?.IsOpen ?? false;
    }

    protected override void Interact()
    {
        if (GameUI.Instance != null && GameUI.Instance.CardSelectionPanel != null && !GameUI.Instance.CardSelectionPanel.IsOpen)
        {
            GameUI.Instance.CardSelectionPanel.Show();
            
            // Re-subscribe or handle panel close if needed to unpause, but relying on CardSelectionPanel to handle its own time freeze
            
            // Chest is consumed
            ObjectPool.Instance.Despawn(gameObject, poolType);
        }
    }
}
