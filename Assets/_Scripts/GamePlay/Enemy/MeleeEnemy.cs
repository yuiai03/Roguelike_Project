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

        if (isLunging)
        {
            PerformLunge(distanceToPlayer);
            return;
        }

        if (isRetreating)
        {
            PerformRetreat();
            return;
        }

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

            currentState = EnemyState.Chasing;
            MoveTowardsPlayer();
        }
    }

    protected override void Update()
    {
        base.Update();
    }

    private System.Collections.IEnumerator PrepareAndLunge()
    {
        isAttackingAnimation = true;
        attackTimer = enemyData.attackCooldown;
        hasDealtDamage = false;

        lungeStartPosition = transform.position;
        lungeDirection = (player.position - transform.position).normalized;
        lungeDirection.y = 0f;

        LookAtPlayer();

        OnAttack?.Invoke();

        yield return new WaitForSeconds(0.2f);

        currentState = EnemyState.Lunging;
        isLunging = true;
    }

    private void PerformLunge(float distanceToPlayer)
    {
        if (!isLunging) return;

        Vector3 moveVector = lungeDirection * enemyData.lungeSpeed * Time.deltaTime;

        if (controller != null && controller.enabled)
        {
            controller.Move(moveVector);
        }
        else
        {
            transform.position += moveVector;
        }

        CheckLungeHit(distanceToPlayer);

        float distanceTraveled = Vector3.Distance(transform.position, lungeStartPosition);
        if (distanceTraveled >= enemyData.lungeDistance)
        {
            EndLunge();
        }
    }

    private void CheckLungeHit(float distanceToPlayer)
    {
        if (player == null || hasDealtDamage) return;

        if (distanceToPlayer <= enemyData.lungeDetectionRadius)
        {
            IDamageable damageable = player.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead())
            {
                Vector3 hitDirection = (player.position - transform.position).normalized;
                Vector3 hitPoint = transform.position + hitDirection * enemyData.lungeDetectionRadius;

                damageable.TakeDamage(enemyData.contactDamage, hitPoint, hitDirection);
                hasDealtDamage = true;

                EndLunge();
            }
        }
    }

    private void EndLunge()
    {
        isLunging = false;

        retreatStartPosition = transform.position;
        currentState = EnemyState.Retreating;
        isRetreating = true;
    }

    private void PerformRetreat()
    {
        if (!isRetreating) return;

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

        float distanceRetreated = Vector3.Distance(transform.position, retreatStartPosition);
        if (distanceRetreated >= enemyData.retreatDistance)
        {
            EndRetreat();
        }
    }

    private void EndRetreat()
    {
        isRetreating = false;
        isAttackingAnimation = false;
        currentState = EnemyState.Idle;
    }

    protected override void OnCollisionStay(Collision collision)
    {

        if (isLunging || isRetreating)
            return;

        base.OnCollisionStay(collision);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isLunging = false;
        isRetreating = false;
        isAttackingAnimation = false;
        hasDealtDamage = false;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (enemyData == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyData.lungeDetectionRadius);

        if (isLunging)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, lungeDirection * enemyData.lungeDistance);
        }

        if (isRetreating)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, -lungeDirection * enemyData.retreatDistance);
        }
    }
}
