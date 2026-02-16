using UnityEngine;

public class PlayerProjectile : Projectile
{
    protected override void DispawnProjectile()
    {
        ObjectPool.Instance.Despawn(gameObject, PoolType.PlayerProjectile);
    }
}
