using UnityEngine;

public class EnemyHealthBarUI : HealthBarUIBase
{
    private Enemy enemy;

    protected override void Initialize()
    {
        enemy = GetComponentInParent<Enemy>();
        enemy?.OnHealthChanged?.AddListener(UpdateHealthBar);
    }

    protected override float GetCurrentHealth()
    {
        return enemy != null ? enemy.GetCurrentHealth() : 0f;
    }

    protected override float GetMaxHealth()
    {
        return enemy != null ? enemy.GetMaxHealth() : 0f;
    }
}
