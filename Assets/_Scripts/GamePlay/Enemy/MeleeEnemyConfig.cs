using UnityEngine;

/// <summary>
/// Config cho Melee Enemy (cận chiến)
/// </summary>
[CreateAssetMenu(fileName = "MeleeEnemy_", menuName = "Roguelike/Enemy/Melee Enemy Config")]
public class MeleeEnemyConfig : EnemyConfig
{
    [Header("Melee Combat")]
    public float contactDamage = 10f;
    public float attackCooldown = 1f;

    public MeleeEnemyConfig()
    {
        enemyType = EnemyType.Melee;
        attackRange = 5;
    }
}
