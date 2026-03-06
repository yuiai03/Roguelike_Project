using UnityEngine;

public class Projectile : MonoBehaviour
{
    protected float damage;
    protected float speed;
    protected float lifetime;
    protected Vector3 direction;
    protected LayerMask targetLayer;
    protected GameObject owner;

    private float timer;
    protected TrailRenderer trailRenderer;

    protected virtual void Awake()
    {
        trailRenderer = GetComponentInChildren<TrailRenderer>();
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity  = false;
            rb.isKinematic = true;
        }
    }

    public virtual void Initialize(float damage, float speed, float lifetime, Vector3 direction, LayerMask targetLayer, GameObject owner)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.direction = direction.normalized;
        this.targetLayer = targetLayer;
        this.owner = owner;

        timer = 0f;

        if (this.direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(this.direction);
        }

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }

    protected virtual void Update()
    {

        transform.position += direction * speed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            DispawnProjectile();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;

        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            OnHit(other);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            Debug.Log($"Projectile hit obstacle: {other.gameObject.name}");
            DispawnProjectile();
            return;
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

    protected virtual void DispawnProjectile() { }
}
