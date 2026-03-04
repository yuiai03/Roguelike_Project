using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Roguelike/Player Config")]
public class PlayerConfig : ScriptableObject
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

    [Header("Combat Settings")]
    public float attackDamage = 50f;
    public float attackCooldown = 1f;
    public float attackRange = 50f;
    public float projectileSpeed = 30f;
    public float projectileLifetime = 10f;
}
