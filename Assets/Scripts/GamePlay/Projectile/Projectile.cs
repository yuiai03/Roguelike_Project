using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected float damage;
    protected float speed;
    protected float lifetime;
    protected Vector3 direction;
    protected LayerMask targetLayer;
    protected GameObject owner;

    public virtual void Initialize(float damage, float speed, float lifetime, Vector3 direction, LayerMask targetLayer, GameObject owner)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.direction = direction.normalized;
        this.targetLayer = targetLayer;
        this.owner = owner;

    }

    protected virtual void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;

        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            OnHit(other);
        }
        else if (!other.isTrigger)
        {
            DispawnProjectile();
        }
    }

    protected virtual void OnHit(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null && !damageable.IsDead())
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            damageable.TakeDamage(damage, hitPoint, direction);
            DispawnProjectile();
        }
    }

    protected virtual void DispawnProjectile() {}
}
