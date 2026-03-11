using UnityEngine;

public class LawaChurl_Pyro : Enemy
{
    private float warningDuration = 1.5f;
    private float effectLifetime = 3f;

    private bool isAttackingAnimation;

    protected override void OnEnable()
    {
        base.OnEnable();
        isAttackingAnimation = false;
        if (enemyData != null)
        {
            enemyData.enemyType = EnemyType.Ranged;
        }

        LawaChurlPyroConfig config = enemyData.GetConfig<LawaChurlPyroConfig>();
        if (config != null)
        {
            warningDuration = config.warningDuration;
            effectLifetime = config.effectLifetime;
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

        if (distanceToPlayer > enemyData.attackRange || attackTimer > 0f)
        {
            currentState = EnemyState.Chasing;
            MoveTowardsPlayer();
        }
        else
        {
            currentState = EnemyState.Attacking;
            StartCoroutine(PerformAttackCoroutine());
        }
    }

    private System.Collections.IEnumerator PerformAttackCoroutine()
    {
        isAttackingAnimation = true;
        velocity.x = 0;
        velocity.z = 0;
        ShootProjectile();
        yield return new WaitForSeconds(1.0f);
        isAttackingAnimation = false;
    }

    protected override void ShootProjectile() // Đập đất spawn warning circle
    {
        attackTimer = enemyData.shootCooldown;
        OnAttack?.Invoke(); // Trigger smash animation

        Vector3 targetPos = player.position;

        // Spawn Warning Circle
        GameObject warningObj = ObjectPool.Instance.Spawn(PoolType.WarningCircle, targetPos, Quaternion.identity);
        if (warningObj != null)
        {
            WarningCircle warningCircle = warningObj.GetComponent<WarningCircle>();
            if (warningCircle != null)
            {
                // Clear any old listeners to prevent memory stringing if pooled poorly
                warningCircle.OnWarningComplete.RemoveAllListeners();

                warningCircle.OnWarningComplete.AddListener(() =>
                {
                    // Spawn Ice effect from below ground when warning completes
                    SpawnPyroEffect(targetPos);
                });
                warningCircle.StartWarning(warningDuration, 3.5f);
            }
        }
    }

    private void SpawnPyroEffect(Vector3 position)
    {
        GameObject effectObj = ObjectPool.Instance.Spawn(PoolType.LawaChurlPyroEffect, position, Quaternion.identity);

        // Cần gắn script gây damage lên effectObj: projectile hoặc một collider tự nổ
        // Tái sử dụng logic Projectile hoặc viết script AOEDamage riêng trên prefab
        Projectile proj = effectObj.GetComponent<Projectile>();
        if (proj != null)
        {
            // Set speed = 0 as it bursts from ground instead of traveling
            proj.Initialize(enemyData.projectileDamage, 0f, 2f, Vector3.up, playerLayer, gameObject);
        }

        // Auto despawn
        ObjectPool.Instance.DespawnAfterDelay(effectObj, PoolType.LawaChurlPyroEffect, effectLifetime);
    }
}
