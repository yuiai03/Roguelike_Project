using UnityEngine;

public class EnemyProjectile : Projectile
{
    protected override void DispawnProjectile()
    {
        ObjectPool.Instance.Despawn(gameObject, PoolType.EnemyProjectile);
    }
}
