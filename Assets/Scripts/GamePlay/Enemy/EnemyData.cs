using System;
using UnityEngine;

public class EnemyData : MonoBehaviour
{
    [Header("Basic Info")]
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Health Settings")]
    public float maxHealth = 100;
    public float currentHealth = 100;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float detectionRange = 10f;
    public float attackRange = 15;
    public float rotationSpeed = 5f;

    [Header("Ground Check")]
    public float groundDistance = 0.4f;
    public float gravity = -10;

    [Header("Combat - Melee")]
    public float contactDamage = 10f;
    public float attackCooldown = 1f;

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

    public void ResetData()
    {
        maxHealth = 100;
        currentHealth = maxHealth;

        moveSpeed = 3f;
        detectionRange = 10f;
        attackRange = 15;
        rotationSpeed = 5f;
        groundDistance = 0.4f;
        gravity = -10;
        contactDamage = 10f;
        attackCooldown = 1f;
        projectileDamage = 15f;
        projectileSpeed = 10f;
        shootCooldown = 2f;
        projectileLifetime = 5f;

        knockbackForce = 3f;
        knockbackDuration = 0.2f;

        expValue = 10;
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
