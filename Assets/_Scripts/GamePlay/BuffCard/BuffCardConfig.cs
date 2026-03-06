using UnityEngine;
using System;

public enum BuffType
{

    IncreaseDamage,
    IncreaseProjectileSpeed,
    IncreaseAttackSpeed,
    MultiShot,
    AoEExplosion,

    IncreaseMaxHealth,
    HealthRegen,

    IncreaseMoveSpeed,

    ExpBoost,

    OrbitingBall,

    SpiritPierce,
    SpiritExplosion,

    IncreaseLuck,
}

public enum RarityType
{
    Common    = 1,
    Rare      = 2,
    Epic      = 3,
    Legendary = 4
}

[CreateAssetMenu(fileName = "BuffCard", menuName = "Roguelike/Buff Card")]
public class BuffCardConfig : ScriptableObject
{
    [Header("Card Info")]
    public string cardName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("Buff Settings")]
    public BuffType buffType;
    public float value; 
    public RarityType rarity = RarityType.Common;
    [Tooltip("Giới hạn tối đa (0 = không giới hạn)")]
    public int maxLevel = 0;

    [Header("MultiShot Settings")]
    [Tooltip("Số đạn bắn thêm mỗi lần pick (chỉ dùng với buff MultiShot)")]
    public int shotCount = 1;

    [Header("AoEExplosion Settings")]
    [Tooltip("Phạm vi nổ AoE (chỉ dùng với buff AoEExplosion)")]
    public float aoeRadius = 2f;

    [Header("OrbitingBall Settings")]
    [Tooltip("Số bóng spawn thêm mỗi lần pick (chỉ dùng với buff OrbitingBall)")]
    public int ballCount = 1;

    public Color GetRarityColor()  => Utils.GetRarityColor(rarity);
    public string GetRarityName()  => Utils.GetRarityName(rarity);

    public void ApplyBuff(PlayerData playerData, PlayerHealth playerHealth)
    {
        if (playerData == null) return;

        switch (buffType)
        {

            case BuffType.IncreaseDamage:
                playerData.damageBonus += value;
                Debug.Log($"[Buff] Damage +{value} → total {playerData.GetTotalDamage()}");
                break;

            case BuffType.IncreaseProjectileSpeed:
                playerData.projectileSpeed += value;
                Debug.Log($"[Buff] ProjectileSpeed +{value}");
                break;

            case BuffType.IncreaseAttackSpeed:
                playerData.attackSpeedBonus += value;
                Debug.Log($"[Buff] AttackSpeed bonus +{value}s → cooldown {playerData.GetAttackCooldown():F2}s");
                break;

            case BuffType.MultiShot:

                playerData.multiShotDamage = value;
                playerData.multiShotCount += Mathf.Max(1, shotCount);
                Debug.Log($"[Buff] MultiShot +{shotCount} đạn (damage={value}) → total {playerData.multiShotCount} đạn");
                break;

            case BuffType.AoEExplosion:
                playerData.isAoEEnabled = true;
                if (aoeRadius > 0f) playerData.aoeRadius = aoeRadius;
                if (value > 0f) playerData.aoeDamage += value;
                Debug.Log($"[Buff] AoEExplosion ON, radius={playerData.aoeRadius}, damage={playerData.aoeDamage}");
                break;

            case BuffType.IncreaseMaxHealth:
                playerHealth?.IncreaseMaxHealth(value);
                Debug.Log($"[Buff] MaxHealth +{value}");
                break;

            case BuffType.HealthRegen:
                playerHealth?.Heal(value);
                Debug.Log($"[Buff] Healed {value} HP");
                break;

            case BuffType.IncreaseMoveSpeed:
                playerData.moveSpeedBonus += value;
                Debug.Log($"[Buff] MoveSpeed +{value} → {playerData.GetEffectiveMoveSpeed()}");
                break;

            case BuffType.ExpBoost:
                playerData.expBonusPercent += value / 100f;
                Debug.Log($"[Buff] ExpBoost +{value}% → total +{playerData.expBonusPercent * 100:F0}%");
                break;

            case BuffType.OrbitingBall:
            {
                OrbitingBallManager ballManager = playerData.GetComponent<OrbitingBallManager>();
                if (ballManager == null)
                    ballManager = playerData.gameObject.AddComponent<OrbitingBallManager>();

                int count = Mathf.Max(1, ballCount);
                for (int i = 0; i < count; i++)
                    ballManager.AddBall(value);
                Debug.Log($"[Buff] OrbitingBall +{count} bóng (damage={value}) → tổng {ballManager.GetBallCount()}");
                break;
            }

            case BuffType.SpiritPierce:
            case BuffType.SpiritExplosion:
            {
                SpiritManager spiritManager = playerData.GetComponent<SpiritManager>();
                if (spiritManager == null)
                    spiritManager = playerData.gameObject.AddComponent<SpiritManager>();

                SpiritType sType = buffType == BuffType.SpiritPierce ? SpiritType.Pierce : SpiritType.Explosion;
                spiritManager.AddSpirit(sType);
                Debug.Log($"[Buff] Spirit {sType} added");
                break;
            }

            case BuffType.IncreaseLuck:
                playerData.luckBonus += value;
                Debug.Log($"[Buff] Luck +{value} → total {playerData.luckBonus}");
                break;
        }
    }

    public string GetFormattedDescription(int currentLevel)
    {
        string finalDesc = description;
        
        if (currentLevel > 0 && buffType == BuffType.AoEExplosion)
        {
            finalDesc = "Tăng thêm sát thương đạn nổ: +{value}";
        }

        return finalDesc.Replace("{value}", value.ToString());
    }
}
