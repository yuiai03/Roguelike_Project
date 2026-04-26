using UnityEngine;

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
    SpiritHealing,
    SpiritTripleShot,
}

public enum RarityType
{
    Common = 1,
    Rare = 2,
    Epic = 3,
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
    [Tooltip("Damage or power multiplier based on the player's current ATK (1 = 100% ATK, 0.5 = 50% ATK).")]
    public float attackDamageMultiplier = 1f;
    public RarityType rarity = RarityType.Common;
    [Tooltip("Maximum number of times this card can be picked (0 = unlimited).")]
    public int maxLevel = 0;

    [Header("MultiShot Settings")]
    [Tooltip("Extra projectiles added each time the MultiShot card is picked.")]
    public int shotCount = 1;

    [Header("AoEExplosion Settings")]
    [Tooltip("Explosion radius used by the AoEExplosion buff.")]
    public float aoeRadius = 2f;

    [Header("OrbitingBall Settings")]
    [Tooltip("Extra orbiting balls added each time the OrbitingBall card is picked.")]
    public int ballCount = 1;

    public Color GetRarityColor() => Utils.GetRarityColor(rarity);
    public string GetRarityName() => Utils.GetRarityName(rarity);

    public bool UsesAttackDamageMultiplier()
    {
        switch (buffType)
        {
            case BuffType.MultiShot:
            case BuffType.AoEExplosion:
            case BuffType.OrbitingBall:
            case BuffType.SpiritPierce:
            case BuffType.SpiritExplosion:
            case BuffType.SpiritHealing:
            case BuffType.SpiritTripleShot:
                return true;

            default:
                return false;
        }
    }

    public void ApplyBuff(PlayerData playerData, PlayerHealth playerHealth)
    {
        if (playerData == null) return;

        switch (buffType)
        {
            case BuffType.IncreaseDamage:
                playerData.damageBonus += value;
                Debug.Log($"[Buff] Damage +{value} -> total {playerData.GetTotalDamage()}");
                break;

            case BuffType.IncreaseProjectileSpeed:
                playerData.projectileSpeed += value;
                Debug.Log($"[Buff] ProjectileSpeed +{value}");
                break;

            case BuffType.IncreaseAttackSpeed:
                playerData.attackSpeedBonus += value;
                Debug.Log($"[Buff] AttackSpeed bonus +{value}s -> cooldown {playerData.GetAttackCooldown():F2}s");
                break;

            case BuffType.MultiShot:
                playerData.multiShotAtkMultiplier = attackDamageMultiplier;
                playerData.multiShotCount += Mathf.Max(1, shotCount);
                Debug.Log($"[Buff] MultiShot +{shotCount} shots ({FormatAttackDamageMultiplier()}) -> total {playerData.multiShotCount} shots");
                break;

            case BuffType.AoEExplosion:
                playerData.isAoEEnabled = true;
                if (aoeRadius > 0f) playerData.aoeRadius = aoeRadius;
                playerData.aoeAtkMultiplier = attackDamageMultiplier;
                Debug.Log($"[Buff] AoEExplosion ON, radius={playerData.aoeRadius}, damage={FormatAttackDamageMultiplier()}");
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
                Debug.Log($"[Buff] MoveSpeed +{value} -> {playerData.GetEffectiveMoveSpeed()}");
                break;

            case BuffType.ExpBoost:
                playerData.expBonusPercent += value / 100f;
                Debug.Log($"[Buff] ExpBoost +{value}% -> total +{playerData.expBonusPercent * 100f:F0}%");
                break;

            case BuffType.OrbitingBall:
            {
                OrbitingBallManager ballManager = playerData.GetComponent<OrbitingBallManager>();
                if (ballManager == null)
                    ballManager = playerData.gameObject.AddComponent<OrbitingBallManager>();

                int count = Mathf.Max(1, ballCount);
                for (int i = 0; i < count; i++)
                    ballManager.AddBall(attackDamageMultiplier);
                Debug.Log($"[Buff] OrbitingBall +{count} balls ({FormatAttackDamageMultiplier()}) -> total {ballManager.GetBallCount()}");
                break;
            }

            case BuffType.SpiritPierce:
            case BuffType.SpiritExplosion:
            case BuffType.SpiritHealing:
            case BuffType.SpiritTripleShot:
            {
                SpiritManager spiritManager = playerData.GetComponent<SpiritManager>();
                if (spiritManager == null)
                    spiritManager = playerData.gameObject.AddComponent<SpiritManager>();

                SpiritType spiritType = ResolveSpiritType(buffType);
                spiritManager.AddSpirit(spiritType, attackDamageMultiplier);
                Debug.Log($"[Buff] Spirit {spiritType} updated ({FormatAttackDamageMultiplier()})");
                break;
            }

            case BuffType.IncreaseLuck:
                playerData.luckBonus += value;
                Debug.Log($"[Buff] Luck +{value} -> total {playerData.luckBonus}");
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

        return finalDesc.Replace("{value}", GetDisplayValueText());
    }

    private string GetDisplayValueText()
    {
        if (UsesAttackDamageMultiplier())
        {
            return FormatAttackDamageMultiplier();
        }

        return value.ToString();
    }

    private string FormatAttackDamageMultiplier()
    {
        return $"{attackDamageMultiplier * 100f:0.#}% ATK";
    }

    private static SpiritType ResolveSpiritType(BuffType type)
    {
        switch (type)
        {
            case BuffType.SpiritPierce:
                return SpiritType.Pierce;
            case BuffType.SpiritExplosion:
                return SpiritType.Explosion;
            case BuffType.SpiritHealing:
                return SpiritType.Healing;
            case BuffType.SpiritTripleShot:
                return SpiritType.TripleShot;
            default:
                Debug.LogWarning($"[Buff] {type} is not a spirit buff. Falling back to Pierce.");
                return SpiritType.Pierce;
        }
    }
}
