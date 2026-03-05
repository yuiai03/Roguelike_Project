using TMPro;
using UnityEngine;

public class PlayerHealthBarUI : HealthBarUIBase
{
    private PlayerHealth playerHealth;
    private PlayerData playerData;

    protected override void Initialize()
    {
        playerHealth = GetComponentInParent<PlayerHealth>();
        playerData = GetComponentInParent<PlayerData>();

        playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
        ChallengePanel.onGameStart += OnGameStart;

        canvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        ChallengePanel.onGameStart -= OnGameStart;
    }

    private void OnGameStart()
    {
        canvasGroup.alpha = 1f;
        UpdateHealthBar(GetCurrentHealth(), GetMaxHealth());
    }

    protected override float GetCurrentHealth() => playerData.currentHealth;

    protected override float GetMaxHealth() => playerData.maxHealth;
}
