using UnityEngine;

public class ElectroProjectile : Projectile
{
    public override void Initialize(float damage, float speed, float lifetime, Vector3 direction, LayerMask targetLayer, GameObject owner)
    {
        base.Initialize(damage, speed, lifetime, direction, targetLayer, owner);
        transform.rotation = Quaternion.identity;
    }

    protected override void DispawnProjectile() { }
}
