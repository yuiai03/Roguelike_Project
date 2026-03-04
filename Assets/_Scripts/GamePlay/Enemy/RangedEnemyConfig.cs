using UnityEngine;

[CreateAssetMenu(fileName = "RangedEnemy_", menuName = "Roguelike/Enemy/Ranged Enemy Config")]
public class RangedEnemyConfig : EnemyConfig
{
    [Header("Ranged Combat")]
    public float projectileDamage = 15f;
    public float projectileSpeed = 10f;
    public float projectileLifetime = 10f;
    public float shootCooldown = 2f;

    public RangedEnemyConfig()
    {
        enemyType = EnemyType.Ranged;
    }
}
