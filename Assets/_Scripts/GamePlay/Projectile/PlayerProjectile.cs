using UnityEngine;
using System.Collections;

public class PlayerProjectile : Projectile
{

    private bool isAoEEnabled;
    private float aoeRadius;
    private float aoeDamagePercent;
    private float aoeDamageFlat; 

    private int pierceCount;

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

        if (isAoEEnabled)
        {
            TriggerAoE(hitPoint);
        }

        if (pierceCount > 0)
        {
            pierceCount--;
            return;
        }

        DispawnProjectile();
    }

    private void TriggerAoE(Vector3 center)
    {

        ObjectPool.Instance.Spawn(PoolType.AoEExplosionVFX, center, Quaternion.identity);

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
