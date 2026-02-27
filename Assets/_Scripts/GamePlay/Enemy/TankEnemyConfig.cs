using UnityEngine;

/// <summary>
/// Config cho Tank Enemy — x2 HP, phản đạn theo chu kỳ
/// </summary>
[CreateAssetMenu(fileName = "TankEnemyConfig", menuName = "Roguelike/Enemy/Tank Config")]
public class TankEnemyConfig : EnemyConfig
{
    [Header("Tank Settings")]
    [Tooltip("Thời gian di chuyển bình thường trước khi kích hoạt shield")]
    public float shieldCycleNormal  = 4f; // giây không có shield
    [Tooltip("Thời gian shield active")]
    public float shieldCycleActive  = 2f; // giây có shield
    [Tooltip("Phần trăm damage phản lại player khi shield active (0-1)")]
    [Range(0f, 1f)]
    public float reflectDamagePercent = 0.3f; // 30%
}
