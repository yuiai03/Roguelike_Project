using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Một quả cầu xoay quanh player. Tự động gây sát thương khi chạm enemy.
/// Được quản lý bởi OrbitingBallManager.
/// </summary>
public class OrbitingBall : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField] private Color flashColor = Color.yellow;
    [SerializeField] private float flashDuration = 0.08f;

    private Renderer ballRenderer;
    private Material instanceMaterial;
    private Color originalColor;

    // Track cooldown per collider để tránh gây damage quá nhiều lần
    private readonly Dictionary<Collider, float> hitTimers = new Dictionary<Collider, float>();

    private void Awake()
    {
        ballRenderer = GetComponentInChildren<Renderer>();
        if (ballRenderer != null)
        {
            // Tạo instance material để không ảnh hưởng tới material gốc
            instanceMaterial = ballRenderer.material;
            originalColor = instanceMaterial.color;
        }
    }

    private void Update()
    {
        // Giảm dần cooldown timer
        var keys = new List<Collider>(hitTimers.Keys);
        foreach (Collider col in keys)
        {
            if (col == null) { hitTimers.Remove(col); continue; }
            hitTimers[col] -= Time.deltaTime;
            if (hitTimers[col] <= 0f) hitTimers.Remove(col);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        // Bỏ qua nếu đang cooldown
        if (hitTimers.ContainsKey(other)) return;

        // Chỉ gây damage cho Enemy (có component IDamageable nhưng KHÔNG phải Player)
        if (other.CompareTag("Player")) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null) target = other.GetComponentInParent<IDamageable>();
        if (target == null || target.IsDead()) return;

        Vector3 dir = (other.transform.position - transform.position).normalized;
        // Truyền Vector3.zero để Enemy.ApplyKnockback bỏ qua knockback
        target.TakeDamage(damage, transform.position, Vector3.zero);

        hitTimers[other] = damageCooldown;

        if (gameObject.activeInHierarchy)
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (instanceMaterial == null) yield break;
        instanceMaterial.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        instanceMaterial.color = originalColor;
    }

    public void Initialize(float dmg)
    {
        damage = dmg;
    }

    private void OnDestroy()
    {
        // Huỷ instance material để tránh memory leak
        if (instanceMaterial != null)
            Destroy(instanceMaterial);
    }
}
