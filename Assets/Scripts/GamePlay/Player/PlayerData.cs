using System;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
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

    [Header("Stats Multipliers")]
    public float healthMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;

    public void ResetData()
    {
        moveSpeed = 5f;
        gravity = -20f;
        rotationSpeed = 1440f;
        
        dashSpeed = 15f;
        dashDuration = 0.2f;
        dashCooldown = 1f;
        
        groundDistance = 0.3f;
        
        maxHealth = 1000f;
        currentHealth = maxHealth;

        attackDamage = 50f;
        attackCooldown = 1f;
        attackRange = 20f;
        projectileSpeed = 30f;
        projectileLifetime = 3f;
        
        healthMultiplier = 1f;
        moveSpeedMultiplier = 1f;
        damageMultiplier = 1f;
        attackSpeedMultiplier = 1f;
    }

    public float GetEffectiveMoveSpeed()
    {
        return moveSpeed * moveSpeedMultiplier;
    }

    public float GetTotalDamage()
    {
        return attackDamage * damageMultiplier;
    }

    public float GetAttackCooldown()
    {
        return attackCooldown / attackSpeedMultiplier;
    }

    public float GetMaxHealth()
    {
        return maxHealth * healthMultiplier;
    }
}
