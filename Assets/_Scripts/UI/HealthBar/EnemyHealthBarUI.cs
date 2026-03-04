using UnityEngine;

public class EnemyHealthBarUI : HealthBarUIBase
{
    private Enemy enemy;

    protected override void Initialize()
    {
        enemy = GetComponentInParent<Enemy>();
        enemy.OnHealthChanged.AddListener(UpdateHealthBar);
    }

    protected override float GetCurrentHealth() => enemy.GetCurrentHealth();

    protected override float GetMaxHealth() => enemy.GetMaxHealth();
}
