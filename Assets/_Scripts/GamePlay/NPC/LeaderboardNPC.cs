using UnityEngine;

public class LeaderboardNPC : NPC
{
    protected override bool CanShowPrompt() =>
        WaveSpawner.Instance == null || WaveSpawner.Instance.GetCurrentWave() <= 0;

    protected override void OnEnable()
    {
        base.OnEnable();
        LeaderboardPanel.OnClosed += OnPanelClosed;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        LeaderboardPanel.OnClosed -= OnPanelClosed;
    }

    protected override bool IsPanelOpen() =>
        GameUI.Instance?.LeaderboardPanel?.IsOpen == true;

    protected override void Interact()
    {
        GameUI.Instance?.LeaderboardPanel?.Show();
    }

    protected override void OnPlayerExit()
    {
        GameUI.Instance?.LeaderboardPanel?.Hide();
    }
}
