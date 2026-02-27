using UnityEngine;
using System;

/// <summary>
/// Enum định nghĩa các loại buff
/// </summary>
public enum BuffType
{
    // Combat buffs
    IncreaseDamage,
    IncreaseProjectileSpeed,
    IncreaseAttackSpeed,
    MultiShot,
    AoEExplosion,

    // Health buffs
    IncreaseMaxHealth,
    HealthRegen,

    // Movement buffs
    IncreaseMoveSpeed,

    // Utility
    ExpBoost,

    // Orbital abilities (cũ)
    OrbitingBall,

    // Spirit abilities
    SpiritPierce,
    SpiritExplosion,
}

/// <summary>
/// Enum định nghĩa độ hiếm của card
/// </summary>
public enum RarityType
{
    Common    = 1,
    Rare      = 2,
    Epic      = 3,
    Legendary = 4
}

/// <summary>
/// ScriptableObject cho mỗi loại buff card
/// </summary>
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
    public float value; // Giá trị buff — với OrbitingBall: đây là damage của mỗi bóng
    public RarityType rarity = RarityType.Common;

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

    /// <summary>Apply buff vào player</summary>
    public void ApplyBuff(PlayerData playerData, PlayerHealth playerHealth)
    {
        if (playerData == null) return;

        switch (buffType)
        {
            // ── Damage ──────────────────────────────────────────
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
                // value = damage mỗi đạn, shotCount = số đạn thêm
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

            // ── Health ──────────────────────────────────────────
            case BuffType.IncreaseMaxHealth:
                playerHealth?.IncreaseMaxHealth(value);
                Debug.Log($"[Buff] MaxHealth +{value}");
                break;

            case BuffType.HealthRegen:
                playerHealth?.Heal(value);
                Debug.Log($"[Buff] Healed {value} HP");
                break;

            // ── Movement ────────────────────────────────────────
            case BuffType.IncreaseMoveSpeed:
                playerData.moveSpeedBonus += value;
                Debug.Log($"[Buff] MoveSpeed +{value} → {playerData.GetEffectiveMoveSpeed()}");
                break;



            // ── Utility ─────────────────────────────────────────
            case BuffType.ExpBoost:
                playerData.expBonusPercent += value / 100f;
                Debug.Log($"[Buff] ExpBoost +{value}% → total +{playerData.expBonusPercent * 100:F0}%");
                break;

            // ── Orbital (legacy) ────────────────────────────────
            case BuffType.OrbitingBall:
            {
                OrbitingBallManager ballManager = playerData.GetComponent<OrbitingBallManager>();
                if (ballManager == null)
                    ballManager = playerData.gameObject.AddComponent<OrbitingBallManager>();

                // ballCount = số bóng spawn, value = damage mỗi bóng
                int count = Mathf.Max(1, ballCount);
                for (int i = 0; i < count; i++)
                    ballManager.AddBall(value);
                Debug.Log($"[Buff] OrbitingBall +{count} bóng (damage={value}) → tổng {ballManager.GetBallCount()}");
                break;
            }

            // ── Spirits ─────────────────────────────────────────
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
        }
    }

    public string GetFormattedDescription()
    {
        return description.Replace("{value}", value.ToString());
    }
}
