using System;
using UnityEngine;

public class EnemyData : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private EnemyConfig dataConfig;

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
    public float projectileLifetime = 5f;
    public float shootCooldown = 2f;

    [Header("Knockback")]
    public float knockbackForce = 3f;
    public float knockbackDuration = 0.2f;

    [Header("Drops")]
    public int expValue = 10;

    private void Awake()
    {
        // Load data từ Config nếu có
        if (dataConfig != null)
        {
            LoadFromConfig();
        }
    }

    /// <summary>
    /// Load dữ liệu từ ScriptableObject Config (READ-ONLY, không modify Config)
    /// </summary>
    public void LoadFromConfig()
    {
        if (dataConfig == null) return;

        // Copy common data từ Config sang runtime variables
        enemyType = dataConfig.enemyType;
        maxHealth = dataConfig.maxHealth;
        currentHealth = maxHealth;

        moveSpeed = dataConfig.moveSpeed;
        attackRange = dataConfig.attackRange;
        rotationSpeed = dataConfig.rotationSpeed;

        groundDistance = dataConfig.groundDistance;
        gravity = dataConfig.gravity;

        knockbackForce = dataConfig.knockbackForce;
        knockbackDuration = dataConfig.knockbackDuration;

        expValue = dataConfig.expValue;

        // Load specific data based on config type
        if (dataConfig is MeleeEnemyConfig meleeConfig)
        {
            contactDamage = meleeConfig.contactDamage;
            attackCooldown = meleeConfig.attackCooldown;
            lungeSpeed = meleeConfig.lungeSpeed;
            lungeDistance = meleeConfig.lungeDistance;
            lungeDetectionRadius = meleeConfig.lungeDetectionRadius;
            retreatSpeed = meleeConfig.retreatSpeed;
            retreatDistance = meleeConfig.retreatDistance;
        }
        else if (dataConfig is RangedEnemyConfig rangedConfig)
        {
            projectileDamage = rangedConfig.projectileDamage;
            projectileSpeed = rangedConfig.projectileSpeed;
            projectileLifetime = rangedConfig.projectileLifetime;
            shootCooldown = rangedConfig.shootCooldown;
        }
    }

    /// <summary>
    /// Reset về giá trị mặc định từ Config (hoặc hardcoded nếu không có Config)
    /// </summary>
    public void ResetData()
    {
        if (dataConfig != null)
        {
            // Nếu có Config, load từ Config
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
}

public enum EnemyType
{
    Melee,
    Ranged,
}
