using UnityEngine;

[CreateAssetMenu(fileName = "FlyEnemy_", menuName = "Roguelike/Enemy/Fly Enemy Config")]
public class FlyEnemyConfig : EnemyConfig
{
    [Header("Fly Combat (Burst)")]
    public float projectileDamage = 10f;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 5f;
    public float shootCooldown = 3f;
    
    public int burstCount = 6;
    public float burstDelay = 0.15f;

    [Header("Fly Movement")]
    public float stopDistance = 8f; // Distance from player to stop and shoot
    public float bobFrequency = 4f; // Speed of the bobbing
    public float bobAmplitude = 0.3f; // Height of the bobbing

    public FlyEnemyConfig()
    {
        enemyType = EnemyType.Fly;
    }
}
