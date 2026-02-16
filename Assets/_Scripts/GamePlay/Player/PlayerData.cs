using System;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private PlayerConfig dataConfig;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -20f;
    public float rotationSpeed = 1440f;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    [Header("Ground Check Settings")]
    public float groundDistance = 0.3f;

    [Header("Health Settings")]
    public float maxHealth = 1000f;
    public float currentHealth = 1000f;

    [Header("Combat Settings")]
    public float attackDamage = 50f;
    public float attackCooldown = 1f;
    public float attackRange = 20f;
    public float projectileSpeed = 30f;
    public float projectileLifetime = 3f;

    [Header("Stats Bonuses")]
    public float healthBonus = 0f;
    public float moveSpeedBonus = 0f;
    public float damageBonus = 0f;
    public float attackSpeedBonus = 0f;

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

        // Copy từ Config sang runtime variables
        moveSpeed = dataConfig.moveSpeed;
        gravity = dataConfig.gravity;
        rotationSpeed = dataConfig.rotationSpeed;

        dashSpeed = dataConfig.dashSpeed;
        dashDuration = dataConfig.dashDuration;
        dashCooldown = dataConfig.dashCooldown;

        groundDistance = dataConfig.groundDistance;

        maxHealth = dataConfig.maxHealth;
        currentHealth = maxHealth;

        attackDamage = dataConfig.attackDamage;
        attackCooldown = dataConfig.attackCooldown;
        attackRange = dataConfig.attackRange;
        projectileSpeed = dataConfig.projectileSpeed;
        projectileLifetime = dataConfig.projectileLifetime;

        // Bonuses start at 0
        healthBonus = 0f;
        moveSpeedBonus = 0f;
        damageBonus = 0f;
        attackSpeedBonus = 0f;
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

    public float GetEffectiveMoveSpeed()
    {
        return moveSpeed + moveSpeedBonus;
    }

    public float GetTotalDamage()
    {
        return attackDamage + damageBonus;
    }

    public float GetAttackCooldown()
    {
        return Mathf.Max(0.1f, attackCooldown - attackSpeedBonus);
    }

    public float GetMaxHealth()
    {
        return maxHealth + healthBonus;
    }
}
