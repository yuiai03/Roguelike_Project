using UnityEngine;

/// <summary>
/// Loại tinh linh — xác định cơ chế tấn công
/// </summary>
public enum SpiritType
{
    Pierce,     // Bắn đạn xuyên qua enemy
    Explosion,  // Bắn đạn nổ AoE
}

/// <summary>
/// Tinh linh luôn di chuyển theo player, bắn đạn vào enemy gần nhất theo timer độc lập.
/// </summary>
public class Spirit : MonoBehaviour
{
    [Header("Spirit Config")]
    public SpiritType spiritType;
    [HideInInspector] public PoolType poolType;

    [Header("Follow / Hover")]
    public float followSpeed    = 5f;
    public float minFollowDist  = 1.5f;
    public float maxFollowDist  = 3.5f;
    public float hoverHeight    = 1.5f;
    public float hoverAmplitude = 0.3f;
    public float hoverFrequency = 2f;

    [Header("Attack")]
    public float attackInterval  = 4f;
    public float attackRange     = 15f;
    public float damage          = 30f;
    public float projectileSpeed = 18f;
    public LayerMask enemyLayer;

    [Header("AoE (Explosion type)")]
    public float aoeRadius = 4f;

    // Runtime
    private Transform player;
    private float idOffset;
    private float attackTimer;
    private Vector3 currentVelocity;

    // ─────────────────────────────────────────────────────────────

    public void Initialize(Transform playerTransform, float startAngle, LayerMask layer)
    {
        player      = playerTransform;
        idOffset    = startAngle;
        enemyLayer  = layer;
        attackTimer = Random.Range(0f, attackInterval);

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (player == null) return;

        // Luôn di chuyển theo player — KHÔNG dừng lại khi bắn
        MoveToFollowTarget();

        // Timer bắn chạy độc lập
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            Transform enemy = FindNearestEnemy();
            if (enemy != null)
            {
                FireProjectile(enemy);
                attackTimer = attackInterval;
            }
            else
            {
                attackTimer = attackInterval * 0.3f;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────

    private void MoveToFollowTarget()
    {
        // Tạo góc target xoay nhẹ theo Perlin noise (mỗi tinh linh khác nhau qua idOffset)
        float driftAngle = idOffset + Mathf.PerlinNoise(Time.time * 0.2f + idOffset, 0f) * 60f - 30f;
        float rad = driftAngle * Mathf.Deg2Rad;

        // Khoảng cách dao động giữa min và max
        float targetDist = Mathf.Lerp(minFollowDist, maxFollowDist,
            Mathf.PerlinNoise(Time.time * 0.15f + idOffset + 5f, 0f));

        // Điểm đích luôn gắn với player.position
        Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * targetDist;
        Vector3 goal   = player.position + offset;

        // Bobbing Y
        float bob = Mathf.Sin(Time.time * hoverFrequency + idOffset) * hoverAmplitude;
        goal.y = player.position.y + hoverHeight + bob;

        // SmoothDamp tới điểm đích
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref currentVelocity, 1f / followSpeed);

        // Quay mặt hướng di chuyển
        Vector3 flatVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        if (flatVel.sqrMagnitude > 0.05f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatVel.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
        }
    }

    // ─────────────────────────────────────────────────────────────

    private void FireProjectile(Transform target)
    {
        if (target == null) return;

        Vector3 spawnPos   = transform.position;
        Vector3 targetFlat = target.position;
        targetFlat.y       = spawnPos.y;
        Vector3 dir        = (targetFlat - spawnPos).normalized;

        GameObject projObj = ObjectPool.Instance.Spawn(PoolType.SpiritProjectile, spawnPos, Quaternion.LookRotation(dir));
        if (projObj == null) return;

        bool isPierce = spiritType == SpiritType.Pierce;
        SpiritProjectileScript proj = projObj.GetComponent<SpiritProjectileScript>();
        if (proj != null)
            proj.Initialize(damage, projectileSpeed, 10f, dir, enemyLayer, gameObject,
                            aoe: !isPierce, aoeRad: aoeRadius, pierce: isPierce);
        else
        {
            Projectile p = projObj.GetComponent<Projectile>();
            p?.Initialize(damage, projectileSpeed, 10f, dir, enemyLayer, gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────

    private Transform FindNearestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(player.position, attackRange, enemyLayer);
        Transform  nearest = null;
        float      minDist = float.MaxValue;

        foreach (var col in enemies)
        {
            IDamageable dmg = col.GetComponent<IDamageable>();
            if (dmg == null || dmg.IsDead()) continue;
            float d = Vector3.Distance(player.position, col.transform.position);
            if (d < minDist) { minDist = d; nearest = col.transform; }
        }
        return nearest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
