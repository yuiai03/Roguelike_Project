using UnityEngine;

[CreateAssetMenu(fileName = "GeoBossConfig", menuName = "Roguelike/Enemy/Boss Configs/Geo Boss")]
public class LawaChurlGeoConfig : BossEnemyConfig
{
    [Header("Geo Boss Skills")]
    [Tooltip("Hệ số tốc độ bay của đá (so với bossProjectileSpeed mặc định)")]
    public float rockSpeedMultiplier = 3f;
}
