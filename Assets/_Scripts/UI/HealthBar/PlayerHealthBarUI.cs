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

        playerHealth?.OnHealthChanged?.AddListener(UpdateHealthBar);
    }

    protected override float GetCurrentHealth()
    {
        return playerData.currentHealth;
    }

    protected override float GetMaxHealth()
    {
        return playerData.maxHealth;
    }
}
