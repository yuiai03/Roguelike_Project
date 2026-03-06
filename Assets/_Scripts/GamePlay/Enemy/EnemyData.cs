using System;
using UnityEngine;

public class EnemyData : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] protected EnemyConfig dataConfig;

    [Header("Basic Info")]
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Health Settings")]
    public float maxHealth = 100;
    public float currentHealth = 100;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float attackRange = 15;
    public float rotationSpeed = 5f;

    [Header("Ground Check")]
    public float groundDistance = 0.4f;
    public float gravity = -10;

    [Header("Combat - Melee")]
    public float contactDamage = 10f;
    public float attackCooldown = 1f;

    [Header("Melee - Lunge Attack")]
    public float lungeSpeed = 12f;
    public float lungeDistance = 3f;
    public float lungeDetectionRadius = 1.5f;
    public float retreatSpeed = 6f;
    public float retreatDistance = 2f;

    [Header("Combat - Ranged")]
    public float projectileDamage = 15f;
    public float projectileSpeed = 10f;
    public float projectileLifetime = 10f;
    public float shootCooldown = 2f;

    [Header("Combat - Fly")]
    public int burstCount = 6;
    public float burstDelay = 0.15f;
    public float stopDistance = 8f;
    public float bobFrequency = 4f;
    public float bobAmplitude = 0.3f;

    [Header("Knockback")]
    public float knockbackForce = 3f;
    public float knockbackDuration = 0.2f;

    [Header("Drops")]
    public int expValue = 10;

    [Header("Randomization")]
    [Tooltip("Độ lệch ngẫu nhiên khi spawn (0.15 = ±15%)")]
    public float randomVariation = 0.15f;

    private void Awake()
    {

        if (dataConfig != null)
        {
            LoadFromConfig();
        }
    }

    public void LoadFromConfig()
    {
        if (dataConfig == null) return;

        float var = randomVariation;

        maxHealth = dataConfig.maxHealth;
        currentHealth = maxHealth;

        moveSpeed = dataConfig.moveSpeed * UnityEngine.Random.Range(1f - var, 1f + var);
        attackRange = dataConfig.attackRange; 
        rotationSpeed = dataConfig.rotationSpeed;

        groundDistance = dataConfig.groundDistance;
        gravity = dataConfig.gravity;

        knockbackForce = dataConfig.knockbackForce;
        knockbackDuration = dataConfig.knockbackDuration;

        expValue = dataConfig.expValue; 

        if (dataConfig is MeleeEnemyConfig meleeConfig)
        {
            contactDamage = meleeConfig.contactDamage;
            attackCooldown = meleeConfig.attackCooldown * UnityEngine.Random.Range(1f - var, 1f + var);
            lungeSpeed = meleeConfig.lungeSpeed * UnityEngine.Random.Range(1f - var, 1f + var);
            lungeDistance = meleeConfig.lungeDistance;
            lungeDetectionRadius = meleeConfig.lungeDetectionRadius;
            retreatSpeed = meleeConfig.retreatSpeed * UnityEngine.Random.Range(1f - var, 1f + var);
            retreatDistance = meleeConfig.retreatDistance;
        }
        else if (dataConfig is RangedEnemyConfig rangedConfig)
        {
            projectileDamage = rangedConfig.projectileDamage;
            projectileSpeed = rangedConfig.projectileSpeed * UnityEngine.Random.Range(1f - var, 1f + var);
            projectileLifetime = rangedConfig.projectileLifetime;
            shootCooldown = rangedConfig.shootCooldown * UnityEngine.Random.Range(1f - var, 1f + var);
        }
        else if (dataConfig is FlyEnemyConfig flyConfig)
        {
            projectileDamage = flyConfig.projectileDamage;
            projectileSpeed = flyConfig.projectileSpeed * UnityEngine.Random.Range(1f - var, 1f + var);
            projectileLifetime = flyConfig.projectileLifetime;
            shootCooldown = flyConfig.shootCooldown * UnityEngine.Random.Range(1f - var, 1f + var);

            burstCount = flyConfig.burstCount;
            burstDelay = flyConfig.burstDelay;
            stopDistance = flyConfig.stopDistance;
            bobFrequency = flyConfig.bobFrequency;
            bobAmplitude = flyConfig.bobAmplitude;
        }
    }

    public void ResetData()
    {
        if (dataConfig != null)
        {

            LoadFromConfig();
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
    }

    public bool IsDead()
    {
        return currentHealth <= 0f;
    }

    public T GetConfig<T>() where T : EnemyConfig
    {

        return dataConfig as T;
    }
}

public enum EnemyType
{
    Melee,
    Ranged,
    Tank,
    Exploder,
    Boss,
    Fly,
}
