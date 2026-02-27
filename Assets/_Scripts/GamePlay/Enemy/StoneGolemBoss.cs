using UnityEngine;
using System.Collections;

/// <summary>
/// Boss Wave 10 — Stone Golem.
/// P1: melee + 3 đạn quạt/3s
/// P2 (60%): +30% tốc độ, triệu hồi 2 melee minion
/// P3 (30%): bắn 6 đạn tròn/2s + ground slam/5s
/// </summary>
public class StoneGolemBoss : BossEnemy
{
    [Header("Stone Golem")]
    [SerializeField] private PoolType minionPoolType = PoolType.MeleeEnemy;
    [SerializeField] private float slamRadius   = 5f;
    [SerializeField] private float slamDamage   = 60f;
    [SerializeField] private float slamCooldown = 5f;

    private float slamTimer;
    private bool minionSpawned;

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        // P1/P2: bắn quạt định kỳ
        if (currentPhase < 3 && bossShootTimer <= 0f)
        {
            int bullets = currentPhase == 1 ? 3 : 5;
            ShootFanAtPlayer(bullets, 20f);
        }

        // P3: bắn vòng tròn + slam
        if (currentPhase == 3)
        {
            if (bossShootTimer <= 0f)
                ShootRadial(6);

            slamTimer -= Time.deltaTime;
            if (slamTimer <= 0f)
                StartCoroutine(GroundSlam());
        }
    }

    protected override void OnPhase2()
    {
        if (minionSpawned) return;
        minionSpawned = true;
        // Spawn 2 melee minion
        for (int i = 0; i < 2; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            ObjectPool.Instance?.Spawn(minionPoolType, transform.position + offset, Quaternion.identity);
        }
        Debug.Log("[StoneGolem] Triệu hồi 2 minion!");
    }

    protected override void OnPhase3()
    {
        slamTimer = slamCooldown;
        Debug.Log("[StoneGolem] Phase 3: Bắn vòng tròn + Ground Slam!");
    }

    private IEnumerator GroundSlam()
    {
        slamTimer = slamCooldown;
        Debug.Log("[StoneGolem] Ground Slam!");
        yield return new WaitForSeconds(0.5f); // windup

        Collider[] hits = Physics.OverlapSphere(transform.position, slamRadius);
        foreach (var col in hits)
        {
            if (col.CompareTag("Player"))
            {
                IDamageable dmg = col.GetComponent<IDamageable>();
                Vector3 dir = (col.transform.position - transform.position).normalized;
                dmg?.TakeDamage(slamDamage, transform.position, dir);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}
