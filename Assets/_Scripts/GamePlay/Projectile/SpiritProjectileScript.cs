using UnityEngine;
using System.Collections.Generic;

public class SpiritProjectileScript : Projectile
{
    private bool isExplosion;
    private float explosionRadius;
    private bool isPierce;
    private readonly HashSet<Collider> hitEnemies = new HashSet<Collider>(); 

    public void Initialize(float dmg, float spd, float life, Vector3 dir,
        LayerMask enemyMask, GameObject ownerObj, bool aoe, float aoeRad, bool pierce = false)
    {
        base.Initialize(dmg, spd, life, dir, enemyMask, ownerObj);
        isExplosion     = aoe;
        explosionRadius = aoeRad;
        isPierce        = pierce;
        hitEnemies.Clear();
    }

    protected override void OnHit(Collider other)
    {
        if (hitEnemies.Contains(other)) return; 

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null || damageable.IsDead()) return;

        hitEnemies.Add(other); 
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        damageable.TakeDamage(damage, hitPoint, direction);

        if (isExplosion)
        {

            Collider[] hits = Physics.OverlapSphere(hitPoint, explosionRadius, targetLayer);
            foreach (var col in hits)
            {
                IDamageable dmg = col.GetComponent<IDamageable>();
                if (dmg == null || dmg.IsDead() || col == other) continue;
                Vector3 dir = (col.transform.position - hitPoint).normalized;
                dmg.TakeDamage(damage, hitPoint, dir);
            }
            ObjectPool.Instance.Spawn(PoolType.AoEExplosionVFX, hitPoint, Quaternion.identity);
            DispawnProjectile(); 
        }
        else if (!isPierce)
        {
            DispawnProjectile(); 
        }

    }

    protected override void DispawnProjectile()
    {
        ObjectPool.Instance.Despawn(gameObject, PoolType.SpiritProjectile);
    }
}
