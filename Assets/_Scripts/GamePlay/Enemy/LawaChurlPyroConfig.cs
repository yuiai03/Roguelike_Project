using UnityEngine;

[CreateAssetMenu(fileName = "PyroBossConfig", menuName = "Roguelike/Enemy/Boss Configs/Pyro Boss")]
public class LawaChurlPyroConfig : BossEnemyConfig
{
    [Header("Pyro Boss Skills")]
    [Tooltip("Thời gian cảnh báo trước khi vụ nổ băng/lửa trồi lên")]
    public float warningDuration = 1.5f;
    
    [Tooltip("Thời gian tồn tại của vụ nổ")]
    public float effectLifetime = 3f;
}
