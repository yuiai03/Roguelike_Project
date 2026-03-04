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
    public float projectileLifetime = 10f;

    [Header("Stats Bonuses")]
    public float healthBonus = 0f;
    public float moveSpeedBonus = 0f;
    public float damageBonus = 0f;
    public float attackSpeedBonus = 0f;

    [Header("MultiShot")]
    public int multiShotCount = 1; 
    public float multiShotAngle = 15f; 
    public float multiShotDamage = 0f; 

    [Header("AoE Explosion")]
    public bool isAoEEnabled = false;
    public float aoeRadius = 3f;
    public float aoeDamagePercent = 0.5f; 
    public float aoeDamage = 0f; 

    [Header("Frost Shot")]
    public float frostChance = 0f; 
    public float frostDuration = 1.5f;

    [Header("Exp Boost")]
    public float expBonusPercent = 0f; 

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

        healthBonus = 0f;
        moveSpeedBonus = 0f;
        damageBonus = 0f;
        attackSpeedBonus = 0f;

        multiShotCount = 1;
        multiShotDamage = 0f;
        isAoEEnabled = false;
        aoeDamage = 0f;
        frostChance = 0f;
        expBonusPercent = 0f;
    }

    public void ResetData()
    {
        if (dataConfig != null)
        {

            LoadFromConfig();
        }
    }

    public float GetEffectiveMoveSpeed()
    {
        return moveSpeed + moveSpeedBonus;
    }

    public float GetTotalDamage()
    {
        return Mathf.Round(attackDamage + damageBonus);
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
