using UnityEngine;
using System.Collections;

/// <summary>
/// Boss Wave 30 — Void Titan.
/// P1: laser thẳng + melee
/// P2 (50%): tạo vùng AoE chậm trên sàn 5s
/// P3 (25%): triệu hồi 3 Exploder + 12 đạn tròn/1.5s
/// </summary>
public class VoidTitanBoss : BossEnemy
{
    [Header("Void Titan")]
    [SerializeField] private PoolType exploderPoolType = PoolType.MeleeEnemy; // đổi thành ExplodeEnemy khi có
    [SerializeField] private float laserDamage    = 40f;
    [SerializeField] private float laserRange     = 20f;
    [SerializeField] private float laserCooldown  = 4f;
    [SerializeField] private float slowZoneDuration = 5f;
    [SerializeField] private float slowZoneRadius = 6f;
    [SerializeField] private LayerMask playerLayer;

    private float laserTimer;
    private bool exploderSpawned;
    private bool slowZoneActive;

    protected override void Awake()
    {
        base.Awake();
        laserTimer = laserCooldown;

        // Void Titan dùng phase3Threshold = 0.25 (override)
        if (bossConfig != null)
            bossConfig.phase3Threshold = 0.25f;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        // P1 + P2: bắn laser định kỳ
        if (currentPhase <= 2)
        {
            laserTimer -= Time.deltaTime;
            if (laserTimer <= 0f)
                StartCoroutine(FireLaser());
        }

        // P3: bắn 12 đạn vòng tròn
        if (currentPhase == 3 && bossShootTimer <= 0f)
        {
            ShootRadial(12);
        }
    }

    private IEnumerator FireLaser()
    {
        laserTimer = laserCooldown;
        if (player == null) yield break;

        Debug.Log("[VoidTitan] Laser!");
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;

        // Raycast laser về phía player
        if (Physics.Raycast(transform.position + Vector3.up, dir, out RaycastHit hit, laserRange, playerLayer))
        {
            IDamageable dmg = hit.collider.GetComponent<IDamageable>();
            dmg?.TakeDamage(laserDamage, hit.point, dir);
        }
    }

    protected override void OnPhase2()
    {
        StartCoroutine(SpawnSlowZoneLoop());
        Debug.Log("[VoidTitan] Phase 2: AoE slow zone!");
    }

    private IEnumerator SpawnSlowZoneLoop()
    {
        while (currentPhase >= 2 && !isDead)
        {
            // Tạo slow zone tại vị trí hiện tại
            StartCoroutine(SlowZoneEffect(transform.position));
            yield return new WaitForSeconds(slowZoneDuration + 2f);
        }
    }

    private IEnumerator SlowZoneEffect(Vector3 center)
    {
        float elapsed = 0f;
        Debug.Log("[VoidTitan] Slow zone xuất hiện!");
        while (elapsed < slowZoneDuration && !isDead)
        {
            elapsed += Time.deltaTime;
            // Slow player trong vùng
            Collider[] hits = Physics.OverlapSphere(center, slowZoneRadius, playerLayer);
            foreach (var col in hits)
            {
                IFreezable freezable = col.GetComponent<IFreezable>();
                freezable?.Slow(0.5f, 0.2f); // 50% slow liên tục
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    protected override void OnPhase3()
    {
        if (exploderSpawned) return;
        exploderSpawned = true;

        // Spawn 3 Exploder
        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
            ObjectPool.Instance?.Spawn(exploderPoolType, transform.position + offset, Quaternion.identity);
        }
        Debug.Log("[VoidTitan] Phase 3: 3 Exploder + 12 đạn vòng tròn!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, slowZoneRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * laserRange);
    }
}
