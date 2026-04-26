using UnityEngine;

public class PlayerProjectile : Projectile
{

    private bool isAoEEnabled;
    private float aoeRadius;
    private float aoeAtkMultiplier;

    private int pierceCount;

    private LayerMask enemyLayer;
    private PlayerData ownerData;

    public void InitializeExtra(
        bool aoeEnabled, float aoeRad, float aoeAtkMult,
        int pierce,
        LayerMask enemyMask)
    {
        isAoEEnabled     = aoeEnabled;
        aoeRadius        = aoeRad;
        aoeAtkMultiplier = aoeAtkMult;
        pierceCount      = pierce;
        enemyLayer       = enemyMask;
        ownerData        = owner != null ? owner.GetComponent<PlayerData>() : null;
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

        float aoeDmg = ownerData != null
            ? ownerData.GetScaledAttackDamage(aoeAtkMultiplier)
            : damage * aoeAtkMultiplier;
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
