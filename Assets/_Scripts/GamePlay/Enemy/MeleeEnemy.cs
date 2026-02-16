using UnityEngine;

public class MeleeEnemy : Enemy
{
    [Header("Melee Settings")]
    private bool isAttackingAnimation;
    private bool isLunging;
    private bool isRetreating;
    private Vector3 lungeDirection;
    private Vector3 lungeStartPosition;
    private Vector3 retreatStartPosition;
    private bool hasDealtDamage;

    protected override void UpdateMeleeAI(float distanceToPlayer)
    {
        // Nếu đang húc về phía player
        if (isLunging)
        {
            PerformLunge(distanceToPlayer);
            return;
        }

        // Nếu đang lùi lại
        if (isRetreating)
        {
            PerformRetreat();
            return;
        }

        // Logic cũ: nếu trong tầm tấn công và cooldown hết
        if (distanceToPlayer <= enemyData.attackRange)
        {
            currentState = EnemyState.Attacking;

            if (attackTimer <= 0f && !isAttackingAnimation)
            {
                StartCoroutine(PrepareAndLunge());
            }
        }
        else
        {
            // Ngoài tầm, đuổi theo player
            currentState = EnemyState.Chasing;
            MoveTowardsPlayer();
        }
    }

    protected override void Update()
    {
        base.Update();
    }

    /// <summary>
    /// Chuẩn bị và thực hiện đòn húc
    /// </summary>
    private System.Collections.IEnumerator PrepareAndLunge()
    {
        isAttackingAnimation = true;
        attackTimer = enemyData.attackCooldown;
        hasDealtDamage = false;

        // Lưu vị trí bắt đầu và hướng húc
        lungeStartPosition = transform.position;
        lungeDirection = (player.position - transform.position).normalized;
        lungeDirection.y = 0f;

        // Quay về phía player
        LookAtPlayer();

        OnAttack?.Invoke();

        // Delay nhỏ trước khi húc (animation windup)
        yield return new WaitForSeconds(0.2f);

        // Bắt đầu húc
        currentState = EnemyState.Lunging;
        isLunging = true;
    }

    /// <summary>
    /// Thực hiện đòn húc về phía player
    /// </summary>
    private void PerformLunge(float distanceToPlayer)
    {
        if (!isLunging) return;

        // Di chuyển với tốc độ lunge
        Vector3 moveVector = lungeDirection * enemyData.lungeSpeed * Time.deltaTime;

        if (controller != null && controller.enabled)
        {
            controller.Move(moveVector);
        }
        else
        {
            transform.position += moveVector;
        }

        // Kiểm tra va chạm với player trong khi lunge
        CheckLungeHit(distanceToPlayer);

        // Kiểm tra xem đã húc đủ xa chưa
        float distanceTraveled = Vector3.Distance(transform.position, lungeStartPosition);
        if (distanceTraveled >= enemyData.lungeDistance)
        {
            EndLunge();
        }
    }

    /// <summary>
    /// Kiểm tra xem có đánh trúng player trong lúc húc không
    /// </summary>
    private void CheckLungeHit(float distanceToPlayer)
    {
        if (player == null || hasDealtDamage) return;

        // Kiểm tra khoảng cách với player
        if (distanceToPlayer <= enemyData.lungeDetectionRadius)
        {
            IDamageable damageable = player.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead())
            {
                Vector3 hitDirection = (player.position - transform.position).normalized;
                Vector3 hitPoint = transform.position + hitDirection * enemyData.lungeDetectionRadius;

                damageable.TakeDamage(enemyData.contactDamage, hitPoint, hitDirection);
                hasDealtDamage = true;

                // Ngay lập tức kết thúc húc và bắt đầu lùi về
                EndLunge();
            }
        }
    }

    /// <summary>
    /// Kết thúc đòn húc và bắt đầu lùi lại
    /// </summary>
    private void EndLunge()
    {
        isLunging = false;

        // Bắt đầu lùi lại
        retreatStartPosition = transform.position;
        currentState = EnemyState.Retreating;
        isRetreating = true;
    }

    /// <summary>
    /// Lùi lại về phía sau
    /// </summary>
    private void PerformRetreat()
    {
        if (!isRetreating) return;

        // Lùi về hướng ngược lại với hướng húc
        Vector3 retreatDirection = -lungeDirection;
        Vector3 moveVector = retreatDirection * enemyData.retreatSpeed * Time.deltaTime;

        if (controller != null && controller.enabled)
        {
            controller.Move(moveVector);
        }
        else
        {
            transform.position += moveVector;
        }

        // Kiểm tra xem đã lùi đủ xa chưa
        float distanceRetreated = Vector3.Distance(transform.position, retreatStartPosition);
        if (distanceRetreated >= enemyData.retreatDistance)
        {
            EndRetreat();
        }
    }

    /// <summary>
    /// Kết thúc lùi lại và quay về follow player
    /// </summary>
    private void EndRetreat()
    {
        isRetreating = false;
        isAttackingAnimation = false;
        currentState = EnemyState.Idle;
    }

    /// <summary>
    /// Xử lý va chạm với player (backup cho trường hợp collision)
    /// </summary>
    protected override void OnCollisionStay(Collision collision)
    {
        // Nếu đang lunge, damage đã được xử lý bởi CheckLungeHit
        if (isLunging || isRetreating)
            return;

        // Gọi base để xử lý tấn công thường (fallback)
        base.OnCollisionStay(collision);
    }

    /// <summary>
    /// Reset khi spawn từ pool
    /// </summary>
    private void OnEnable()
    {
        isLunging = false;
        isRetreating = false;
        isAttackingAnimation = false;
        hasDealtDamage = false;
    }

    /// <summary>
    /// Vẽ Gizmos để visualize lunge
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (enemyData == null) return;

        // Vẽ lunge detection radius (màu vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyData.lungeDetectionRadius);

        // Nếu đang lunge, vẽ hướng lunge
        if (isLunging)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, lungeDirection * enemyData.lungeDistance);
        }

        // Nếu đang retreat, vẽ hướng retreat
        if (isRetreating)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, -lungeDirection * enemyData.retreatDistance);
        }
    }
}
