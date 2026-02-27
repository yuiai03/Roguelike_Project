using UnityEngine;

/// <summary>
/// Config cho Exploder Enemy — lao thẳng vào player rồi nổ
/// </summary>
[CreateAssetMenu(fileName = "ExplodeEnemyConfig", menuName = "Roguelike/Enemy/Explode Config")]
public class ExplodeEnemyConfig : EnemyConfig
{
    [Header("Exploder Settings")]
    public float detectionRange    = 6f;    // Tầm phát hiện để bắt đầu charge
    public float chargeSpeed       = 18f;   // Tốc độ lao
    public float chargeDistance    = 8f;    // Khoảng cách lao tối đa
    public float warningDuration   = 0.8f;  // Thời gian hiển thị indicator

    [Header("Explosion")]
    public float explosionRadius   = 3f;
    public float explosionDamage   = 50f;
    public float deathExplosionMult = 0.5f; // % explosion khi bị giết trước khi nổ
}
