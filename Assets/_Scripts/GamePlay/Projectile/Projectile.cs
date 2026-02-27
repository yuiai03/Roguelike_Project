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

    protected virtual void Awake()
    {
        // Tắt gravity nếu có Rigidbody — đạn di chuyển bằng transform, không cần physics
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

        // Reset timer when initialized
        timer = 0f;

        // Rotate projectile to face direction
        if (this.direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(this.direction);
        }
    }

    protected virtual void Update()
    {
        // Move projectile
        transform.position += direction * speed * Time.deltaTime;

        // Check lifetime
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            DispawnProjectile();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == owner) return;

        // Check if hit target layer
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            OnHit(other);
            return;
        }

        // Check if hit obstacle
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            Debug.Log($"Projectile hit obstacle: {other.gameObject.name}");
            DispawnProjectile();
            return;
        }

        // Ignore everything else (other enemies, other players, zones, etc.)
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
