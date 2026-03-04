using UnityEngine;

[CreateAssetMenu(fileName = "BossEnemyConfig", menuName = "Roguelike/Enemy/Boss Config")]
public class BossEnemyConfig : EnemyConfig
{
    [Header("Boss Identity")]
    public string bossName = "Boss";

    [Header("Phase Thresholds (HP %)")]
    [Tooltip("% HP trigger phase 2 (0-1)")]
    [Range(0f, 1f)]
    public float phase2Threshold = 0.6f; 
    [Tooltip("% HP trigger phase 3 (0-1)")]
    [Range(0f, 1f)]
    public float phase3Threshold = 0.3f; 

    [Header("Phase Damage Multipliers")]
    public float phase1DamageMult   = 1f;
    public float phase2DamageMult   = 1.5f;
    public float phase3DamageMult   = 2f;

    [Header("Phase Speed Multipliers")]
    public float phase1SpeedMult    = 1f;
    public float phase2SpeedMult    = 1.3f;
    public float phase3SpeedMult    = 1.6f;

    [Header("Projectile (Boss Bullets)")]
    public float bossProjectileSpeed   = 15f;
    public float bossProjectileLifetime = 6f;
    public float bossProjectileDamage  = 25f;
    public float bossShootCooldown     = 3f;

    [Header("Buff Card Drops")]
    public int buffCardDropCount = 2;
}
