using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    private readonly Dictionary<Collider, float> hitTimers = new Dictionary<Collider, float>();

    private void Awake()
    {
        ballRenderer = GetComponentInChildren<Renderer>();
        if (ballRenderer != null)
        {

            instanceMaterial = ballRenderer.material;
            originalColor = instanceMaterial.color;
        }
    }

    private void Update()
    {

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

        if (hitTimers.ContainsKey(other)) return;

        if (other.CompareTag("Player")) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null) target = other.GetComponentInParent<IDamageable>();
        if (target == null || target.IsDead()) return;

        Vector3 dir = (other.transform.position - transform.position).normalized;

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

        if (instanceMaterial != null)
            Destroy(instanceMaterial);
    }
}
