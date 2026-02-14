using UnityEngine;

/// <summary>
/// Ranged Enemy - giữ khoảng cách và bắn từ xa
/// </summary>
public class RangedEnemy : Enemy
{
    protected override void Awake()
    {
        base.Awake();

        // Đảm bảo ranged enemy có attack range đủ xa
        if (enemyData.enemyType == EnemyType.Ranged)
        {
            if (enemyData.detectionRange < enemyData.attackRange + 5f)
            {
                enemyData.detectionRange = enemyData.attackRange + 5f;
            }
        }
    }

    protected override void UpdateRangedAI(float distanceToPlayer, bool hasLineOfSight)
    {
        if (!hasLineOfSight)
        {
            currentState = EnemyState.Idle;
            return;
        }

        if (distanceToPlayer <= enemyData.detectionRange)
        {
            LookAtPlayer();

            // Nếu trong attack range -> tấn công
            if (distanceToPlayer <= enemyData.attackRange)
            {
                currentState = EnemyState.Attacking;

                if (attackTimer <= 0f)
                {
                    ShootProjectile();
                }
            }
            // Nếu ngoài attack range -> di chuyển đến gần
            else
            {
                currentState = EnemyState.Chasing;
                MoveTowardsPlayer();
            }
        }
        else
        {
            currentState = EnemyState.Idle;
        }
    }
}
