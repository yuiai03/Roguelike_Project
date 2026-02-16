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

    [Header("Lunge Attack")]
    public float lungeSpeed = 12f;
    public float lungeDistance = 3f;
    public float lungeDetectionRadius = 1.5f;
    public float retreatSpeed = 6f;
    public float retreatDistance = 2f;

    public MeleeEnemyConfig()
    {
        enemyType = EnemyType.Melee;
        attackRange = 5;
    }
}
