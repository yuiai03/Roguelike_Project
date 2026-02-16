using UnityEngine;

/// <summary>
/// Base ScriptableObject cho tất cả Enemy Config (READ-ONLY)
/// Chứa các thuộc tính chung cho mọi loại enemy
/// </summary>
public abstract class EnemyConfig : ScriptableObject
{
    [Header("Basic Info")]
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Health Settings")]
    public float maxHealth = 100;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float attackRange = 50;
    public float rotationSpeed = 1440f;

    [Header("Ground Check")]
    public float groundDistance = 0.4f;
    public float gravity = -20;

    [Header("Knockback")]
    public float knockbackForce = 3f;
    public float knockbackDuration = 0.2f;

    [Header("Drops")]
    public int expValue = 10;
}
