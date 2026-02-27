using UnityEngine;
using System.Collections;

/// <summary>
/// Đạn của player — hỗ trợ AoE explosion và pierce.
/// </summary>
public class PlayerProjectile : Projectile
{
    // AoE
    private bool isAoEEnabled;
    private float aoeRadius;
    private float aoeDamagePercent;
    private float aoeDamageFlat; // flat damage AoE (>0 thì ưu tiên dùng, bỏ qua percent)

    // Pierce: số lần xuyên còn lại (-1 = không xuyên)
    private int pierceCount;

    // Frost layer mask (enemy layer)
    private LayerMask enemyLayer;

    public void InitializeExtra(
        bool aoeEnabled, float aoeRad, float aoeDmgPct, float aoeDmgFlat,
        int pierce,
        LayerMask enemyMask)
    {
        isAoEEnabled     = aoeEnabled;
        aoeRadius        = aoeRad;
        aoeDamagePercent = aoeDmgPct;
        aoeDamageFlat    = aoeDmgFlat;
        pierceCount      = pierce;
        enemyLayer       = enemyMask;
    }

    protected override void OnHit(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null || damageable.IsDead()) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        damageable.TakeDamage(damage, hitPoint, direction);

        // AoE Explosion
        if (isAoEEnabled)
        {
            TriggerAoE(hitPoint);
        }

        // Pierce
        if (pierceCount > 0)
        {
            pierceCount--;
            return;
        }

        DispawnProjectile();
    }

    private void TriggerAoE(Vector3 center)
    {
        // Hiệu ứng nổ VFX
        ObjectPool.Instance.Spawn(PoolType.AoEExplosionVFX, center, Quaternion.identity);

        // Ưu tiên dùng flat damage nếu có, fallback về percent
        float aoeDmg = (aoeDamageFlat > 0f) ? aoeDamageFlat : damage * aoeDamagePercent;
        Collider[] hits = Physics.OverlapSphere(center, aoeRadius, enemyLayer);
        foreach (Collider col in hits)
        {
            IDamageable dmg = col.GetComponent<IDamageable>();
            if (dmg != null && !dmg.IsDead())
            {
                Vector3 dir = (col.transform.position - center).normalized;
                dmg.TakeDamage(aoeDmg, center, dir);
            }
        }
    }

    protected override void DispawnProjectile()
    {
        ObjectPool.Instance.Despawn(gameObject, PoolType.PlayerProjectile);
    }
}
