using UnityEngine;

[CreateAssetMenu(fileName = "ElectroBossConfig", menuName = "Roguelike/Enemy/Boss Configs/Electro Boss")]
public class LawaChurlElectroConfig : BossEnemyConfig
{
    [Header("Electro Boss Skills")]
    [Tooltip("Thời gian bom rời từ trên trời xuống (bằng với thời gian vòng cảnh báo)")]
    public float bombDropDuration = 2f;
    
    [Tooltip("Độ cao bắt đầu thả bom")]
    public float bombSpawnHeight = 15f;
}
