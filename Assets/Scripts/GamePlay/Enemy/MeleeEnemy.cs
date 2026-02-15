using UnityEngine;
public class MeleeEnemy : Enemy
{
    [Header("Melee Settings")]
    [SerializeField] private float attackAnimationDuration = 0.3f;
    private bool isAttackingAnimation;

    protected override void UpdateMeleeAI(float distanceToPlayer, bool hasLineOfSight)
    {
        base.UpdateMeleeAI(distanceToPlayer, hasLineOfSight);

        if (currentState == EnemyState.Attacking && attackTimer <= 0f && !isAttackingAnimation)
        {
            StartCoroutine(PerformMeleeAttack());
        }
    }

    private System.Collections.IEnumerator PerformMeleeAttack()
    {
        isAttackingAnimation = true;
        attackTimer = enemyData.attackCooldown;
        OnAttack?.Invoke();

        yield return new WaitForSeconds(attackAnimationDuration);
        isAttackingAnimation = false;
    }
}
