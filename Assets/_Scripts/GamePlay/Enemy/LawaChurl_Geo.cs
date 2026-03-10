using UnityEngine;

public class LawaChurl_Geo : Enemy
{
    private bool isAttackingAnimation;

    protected override void OnEnable()
    {
        base.OnEnable();
        isAttackingAnimation = false;
        // Force Ranged AI behavior for throwing rocks
        if (enemyData != null)
        {
            enemyData.enemyType = EnemyType.Ranged;
        }
    }

    protected override void UpdateRangedAI(float distanceToPlayer)
    {
        if (isAttackingAnimation)
        {
            LookAtPlayer();
            velocity.x = 0;
            velocity.z = 0;
            return;
        }

        LookAtPlayer();

        // Thêm logic di chuyển liên tục trong lúc chờ hồi chiêu
        if (distanceToPlayer > enemyData.attackRange || attackTimer > 0f)
        {
            currentState = EnemyState.Chasing;
            MoveTowardsPlayer();
        }
        else
        {
            currentState = EnemyState.Attacking;
            // Dừng lại khi đang ra đòn
            StartCoroutine(PerformAttackCoroutine());
        }
    }

    private System.Collections.IEnumerator PerformAttackCoroutine()
    {
        isAttackingAnimation = true;
        velocity.x = 0;
        velocity.z = 0;
        OnAttack?.Invoke();
        yield return new WaitForSeconds(1.3f);
        ShootProjectile();
        yield return new WaitForSeconds(1.5f);
        isAttackingAnimation = false;
    }

    protected override void ShootProjectile() // Ném đá
    {
        if (projectilePrefab == null) return;

        attackTimer = enemyData.shootCooldown;

        Vector3 spawnPosition = firePoint.position + Vector3.up * 1f;
        Vector3 targetPosition = player.position + Vector3.up * 1f + moveOffset * 0.3f;
        Vector3 rawDir = targetPosition - spawnPosition;
        rawDir.y = 0f;
        Vector3 direction = rawDir.normalized;

        // Use custom pool type for Geo Rock
        GameObject projectile = ObjectPool.Instance.Spawn(PoolType.LawaChurlGeoRock, spawnPosition, Quaternion.LookRotation(direction));

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            float speedMult = 1.5f;
            LawaChurlGeoConfig config = enemyData.GetConfig<LawaChurlGeoConfig>();
            if (config != null) speedMult = config.rockSpeedMultiplier;

            // Geo Rock generally has high damage and faster speed
            proj.Initialize(enemyData.projectileDamage, enemyData.projectileSpeed * speedMult,
                enemyData.projectileLifetime, direction, playerLayer, gameObject);
        }
    }
}
