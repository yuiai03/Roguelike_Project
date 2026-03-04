using UnityEngine;

public class ChallengePostNPC : NPC
{
    protected override void OnEnable()
    {
        base.OnEnable();
        ChallengePanel.OnClosed += OnPanelClosed;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ChallengePanel.OnClosed -= OnPanelClosed;
    }

    protected override bool IsPanelOpen() =>
        GameUI.Instance?.ChallengePanel?.IsOpen == true;

    protected override void Interact()
    {
        GameUI.Instance?.ChallengePanel?.ShowTutorial();
    }
}
