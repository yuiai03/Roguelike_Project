using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public abstract class BossEnemy : Enemy
{
    [Header("Boss Events")]
    public UnityEvent<int> OnPhaseChanged; 
    public UnityEvent      OnBossDied;

    protected BossEnemyConfig bossConfig;
    protected int currentPhase = 1;
    protected float baseSpeed;
    protected float baseShootCooldown;

    protected float bossShootTimer;

    protected override void Awake()
    {
        base.Awake();
        bossConfig = GetComponent<EnemyData>()?.GetConfig<BossEnemyConfig>();
        if (bossConfig != null)
            baseShootCooldown = bossConfig.bossShootCooldown;
        baseSpeed = enemyData != null ? enemyData.moveSpeed : 5f;
    }

    protected override void Update()
    {
        base.Update();
        CheckPhaseTransition();

        if (bossShootTimer > 0f)
            bossShootTimer -= Time.deltaTime;
    }

    private void CheckPhaseTransition()
    {
        if (isDead || bossConfig == null || enemyData == null) return;

        float hpPercent = enemyData.currentHealth / enemyData.maxHealth;

        if (currentPhase == 1 && hpPercent <= bossConfig.phase2Threshold)
            TransitionToPhase(2);
        else if (currentPhase == 2 && hpPercent <= bossConfig.phase3Threshold)
            TransitionToPhase(3);
    }

    private void TransitionToPhase(int newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"[Boss] {bossConfig?.bossName} → Phase {newPhase}!");
        OnPhaseChanged?.Invoke(newPhase);

        if (enemyData != null && bossConfig != null)
        {
            float speedMult = newPhase == 2 ? bossConfig.phase2SpeedMult : bossConfig.phase3SpeedMult;
            enemyData.moveSpeed = baseSpeed * speedMult;
        }

        switch (newPhase)
        {
            case 2: OnPhase2(); break;
            case 3: OnPhase3(); break;
        }
    }

    protected override void Die()
    {
        base.Die();
        OnBossDied?.Invoke();

        if (bossConfig != null)
        {
            BuffCardManager.Instance?.SelectCards();
            Debug.Log($"[Boss] {bossConfig.bossName} đã chết! Drop {bossConfig.buffCardDropCount} buff cards.");
        }
    }

    protected virtual void OnPhase1() { }
    protected virtual void OnPhase2() { }
    protected virtual void OnPhase3() { }

    protected void ShootRadial(int bulletCount, float damageOverride = -1)
    {
        if (bossConfig == null) return;
        float dmg = damageOverride > 0 ? damageOverride : bossConfig.bossProjectileDamage;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = (360f / bulletCount) * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            SpawnBossProjectile(dir, dmg);
        }
        bossShootTimer = bossConfig.bossShootCooldown;
    }

    protected void ShootFanAtPlayer(int bulletCount, float spreadAngle = 30f, float damageOverride = -1)
    {
        if (bossConfig == null || player == null) return;
        float dmg = damageOverride > 0 ? damageOverride : bossConfig.bossProjectileDamage;

        Vector3 baseDir = (player.position - transform.position).normalized;
        baseDir.y = 0f;
        float totalSpread = spreadAngle * (bulletCount - 1);
        float startAngle  = -totalSpread / 2f;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = startAngle + spreadAngle * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            SpawnBossProjectile(dir, dmg);
        }
        bossShootTimer = bossConfig.bossShootCooldown;
    }

    private void SpawnBossProjectile(Vector3 direction, float damage)
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position + Vector3.up * 2f : transform.position + Vector3.up * 2f;
        GameObject obj = ObjectPool.Instance.Spawn(PoolType.EnemyProjectile, spawnPos, Quaternion.LookRotation(direction));
        if (obj == null) return;
        Projectile proj = obj.GetComponent<Projectile>();
        proj?.Initialize(damage, bossConfig.bossProjectileSpeed, bossConfig.bossProjectileLifetime,
            direction, LayerMask.GetMask("Player"), gameObject);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (bossConfig != null)
        {
            Gizmos.color = new Color(1f, 0f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, (enemyData?.attackRange ?? 5f) * 1.5f);
        }
    }
}
