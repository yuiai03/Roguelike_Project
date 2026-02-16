using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Detection")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject projectilePrefab;

    private float attackTimer;
    private PlayerHealth playerHealth;
    private PlayerData playerData;
    private Transform currentTarget;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerData = GetComponent<PlayerData>();
    }

    void Update()
    {
        if (playerData == null) return;
        if (playerHealth != null && playerHealth.IsDead()) return;

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            TryAttack();
            attackTimer = playerData.attackCooldown;
        }
    }

    private void TryAttack()
    {
        if (attackTimer > 0f) return;

        Transform target = FindNearestEnemy();
        if (target != null)
        {
            currentTarget = target;
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        attackTimer = playerData.GetAttackCooldown();
        
        SpawnProjectile();
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null) return;

        GameObject obj = ObjectPool.Instance.Spawn(PoolType.PlayerProjectile, attackPoint.position, attackPoint.rotation);
        
        if (obj != null && currentTarget != null)
        {
            Vector3 direction = (currentTarget.position - attackPoint.position).normalized;
            
            float finalDamage = playerData.GetTotalDamage();
            
            PlayerProjectile projectile = obj.GetComponent<PlayerProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(
                    finalDamage,
                    playerData.projectileSpeed,
                    playerData.projectileLifetime,
                    direction,
                    enemyLayer,
                    gameObject
                );
            }
        }
    }

    private Transform FindNearestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, playerData.attackRange, enemyLayer);
        
        Transform nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider enemyCollider in enemies)
        {
            IDamageable damageable = enemyCollider.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead())
            {
                float distance = Vector3.Distance(transform.position, enemyCollider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemyCollider.transform;
                }
            }
        }

        return nearestEnemy;
    }

    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            attackPoint = transform;

        if (playerData == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, playerData.attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, playerData.attackRange);

        if (currentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
