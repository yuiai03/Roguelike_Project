using UnityEngine;
using System;

/// <summary>
/// Enum định nghĩa các loại buff
/// </summary>
public enum BuffType
{
    // Damage buffs
    IncreaseDamage,
    IncreaseProjectileSpeed,
    IncreaseAttackSpeed,

    // Health buffs
    IncreaseMaxHealth,
    HealthRegen,

    // Movement buffs
    IncreaseMoveSpeed,
    ReduceDashCooldown,

    // Special buffs
    ExpBoost,
    MultiShot,
    CriticalChance
}

/// <summary>
/// Enum định nghĩa độ hiếm của card
/// </summary>
public enum RarityType
{
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

/// <summary>
/// ScriptableObject cho mỗi loại buff card
/// </summary>
[CreateAssetMenu(fileName = "BuffCard", menuName = "Roguelike/Buff Card")]
public class BuffCard : ScriptableObject
{
    [Header("Card Info")]
    public string cardName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;

    [Header("Buff Settings")]
    public BuffType buffType;
    public float value; // Giá trị buff (%, flat number, etc.)
    public RarityType rarity = RarityType.Common;

    /// <summary>
    /// Lấy màu dựa trên rarity
    /// </summary>
    public Color GetRarityColor()
    {
        return Utils.GetRarityColor(rarity);
    }

    /// <summary>
    /// Lấy tên rarity
    /// </summary>
    public string GetRarityName()
    {
        return Utils.GetRarityName(rarity);
    }

    /// <summary>
    /// Apply buff vào player
    /// </summary>
    public void ApplyBuff(PlayerData playerData, PlayerHealth playerHealth)
    {
        if (playerData == null) return;

        switch (buffType)
        {
            case BuffType.IncreaseDamage:
                playerData.damageMultiplier += value / 100f;
                Debug.Log($"Damage increased by {value}%! New multiplier: {playerData.damageMultiplier}");
                break;

            case BuffType.IncreaseProjectileSpeed:
                playerData.projectileSpeed += value;
                Debug.Log($"Projectile speed increased by {value}! New speed: {playerData.projectileSpeed}");
                break;

            case BuffType.IncreaseAttackSpeed:
                playerData.attackSpeedMultiplier += value / 100f;
                Debug.Log($"Attack speed increased by {value}%! New multiplier: {playerData.attackSpeedMultiplier}");
                break;

            case BuffType.IncreaseMaxHealth:
                float healthIncrease = value;
                playerData.maxHealth += healthIncrease;
                playerData.currentHealth += healthIncrease; // Heal the increased amount
                Debug.Log($"Max health increased by {healthIncrease}! New max: {playerData.maxHealth}");
                break;

            case BuffType.HealthRegen:
                if (playerHealth != null)
                {
                    playerHealth.Heal(value);
                    Debug.Log($"Healed {value} HP!");
                }
                break;

            case BuffType.IncreaseMoveSpeed:
                playerData.moveSpeedMultiplier += value / 100f;
                Debug.Log($"Move speed increased by {value}%! New multiplier: {playerData.moveSpeedMultiplier}");
                break;

            case BuffType.ReduceDashCooldown:
                playerData.dashCooldown *= (1f - value / 100f);
                playerData.dashCooldown = Mathf.Max(0.1f, playerData.dashCooldown);
                Debug.Log($"Dash cooldown reduced by {value}%! New cooldown: {playerData.dashCooldown}");
                break;

            case BuffType.ExpBoost:
                // This would need ExpBoostManager or similar
                Debug.Log($"Exp boost by {value}% activated!");
                // TODO: Implement exp boost system
                break;

            case BuffType.MultiShot:
                // This would need to modify PlayerAttack
                Debug.Log($"Multi-shot buff activated!");
                // TODO: Implement multi-shot system
                break;

            case BuffType.CriticalChance:
                // This would need critical hit system
                Debug.Log($"Critical chance increased by {value}%!");
                // TODO: Implement critical hit system
                break;
        }
    }

    /// <summary>
    /// Get formatted description with value
    /// </summary>
    public string GetFormattedDescription()
    {
        return description.Replace("{value}", value.ToString());
    }
}
