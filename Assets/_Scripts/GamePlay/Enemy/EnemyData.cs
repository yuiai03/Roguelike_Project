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

        // Tính toán thông số ngẫu nhiên cho mỗi con quái
        float var = randomVariation;
        
        // Randomize các chỉ số chính (± variation %)
        maxHealth = dataConfig.maxHealth;
        currentHealth = maxHealth;

        moveSpeed = dataConfig.moveSpeed * UnityEngine.Random.Range(1f - var, 1f + var);
        attackRange = dataConfig.attackRange; // Tầm đánh giữ nguyên để không bị lỗi logic
        rotationSpeed = dataConfig.rotationSpeed;

        groundDistance = dataConfig.groundDistance;
        gravity = dataConfig.gravity;

        knockbackForce = dataConfig.knockbackForce;
        knockbackDuration = dataConfig.knockbackDuration;

        expValue = dataConfig.expValue; // EXP rớt ra giữ nguyên

        // Load specific data based on config type
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

    /// <summary>
    /// Helper: Lấy config của enemy theo kiểu cụ thể (ví dụ TankEnemyConfig)
    /// </summary>
    public T GetConfig<T>() where T : EnemyConfig
    {
        // EnemyConfig lưu dưới dạng private field, truy cập qua đối tượng này
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
}
