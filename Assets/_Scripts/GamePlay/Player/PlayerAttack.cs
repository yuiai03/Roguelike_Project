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
        SpawnProjectiles();
    }

    private void SpawnProjectiles()
    {
        if (projectilePrefab == null) return;
        if (currentTarget == null) return;

        Vector3 spawnPos = attackPoint.position + Vector3.up * 1f;
        Vector3 targetPos = currentTarget.position + Vector3.up * 1f;
        Vector3 rawDir = targetPos - spawnPos;
        rawDir.y = 0f; // bắn ngang, trục y không đổi
        Vector3 baseDir = rawDir.normalized;

        int shotCount = Mathf.Max(1, playerData.multiShotCount);

        if (shotCount == 1)
        {
            SpawnSingleProjectile(spawnPos, baseDir);
        }
        else
        {
            // Hình quạt: spread đều hai bên
            float totalSpread = playerData.multiShotAngle * (shotCount - 1);
            float startAngle = -totalSpread / 2f;

            for (int i = 0; i < shotCount; i++)
            {
                float angle = startAngle + playerData.multiShotAngle * i;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
                SpawnSingleProjectile(spawnPos, dir);
            }
        }
    }

    private void SpawnSingleProjectile(Vector3 spawnPos, Vector3 direction)
    {
        GameObject obj = ObjectPool.Instance.Spawn(PoolType.PlayerProjectile, spawnPos, Quaternion.identity);
        if (obj == null) return;

        // Nếu đạn MultiShot có damage riêng thì dùng, ngược lại dùng damage gốc
        float finalDamage = (playerData.multiShotCount > 1 && playerData.multiShotDamage > 0f)
            ? playerData.multiShotDamage
            : playerData.GetTotalDamage();

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

            projectile.InitializeExtra(
                playerData.isAoEEnabled,
                playerData.aoeRadius,
                playerData.aoeDamagePercent,
                playerData.aoeDamage,
                0, // pierce count — spirit sẽ xử lý riêng
                enemyLayer
            );
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
