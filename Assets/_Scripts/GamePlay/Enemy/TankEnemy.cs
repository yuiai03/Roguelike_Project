using UnityEngine;
using System.Collections;

/// <summary>
/// Tank Enemy — HP x2, shield theo chu kỳ phản đạn. 
/// Khi shield active: đạn player bị phản ngược về phía player.
/// </summary>
public class TankEnemy : Enemy
{
    [Header("Tank Visual")]
    [SerializeField] private GameObject shieldVisual; // Assign shield mesh/particle trong Inspector
    [SerializeField] private Color shieldColor = new Color(0.3f, 0.8f, 1f, 0.6f);

    // Config riêng
    private TankEnemyConfig tankConfig;

    // Shield state
    private bool isShieldActive;
    private float shieldTimer;
    private Material shieldMaterial;

    // Layer để bắn đạn phản về phía player
    [SerializeField] private LayerMask playerLayer;

    protected override void Awake()
    {
        base.Awake();
        tankConfig = GetComponent<EnemyData>()?.GetConfig<TankEnemyConfig>();
        if (shieldVisual != null)
        {
            shieldMaterial = shieldVisual.GetComponent<Renderer>()?.material;
            shieldVisual.SetActive(false);
        }
    }

    protected override void Update()
    {
        base.Update();
        UpdateShieldCycle();
    }

    private void UpdateShieldCycle()
    {
        if (isDead || tankConfig == null) return;

        shieldTimer -= Time.deltaTime;
        if (shieldTimer <= 0f)
        {
            if (isShieldActive)
                DeactivateShield();
            else
                ActivateShield();
        }
    }

    private void ActivateShield()
    {
        isShieldActive = true;
        shieldTimer = tankConfig.shieldCycleActive;
        if (shieldVisual != null) shieldVisual.SetActive(true);
        Debug.Log("[TankEnemy] Shield ON");
    }

    private void DeactivateShield()
    {
        isShieldActive = false;
        shieldTimer = tankConfig.shieldCycleNormal;
        if (shieldVisual != null) shieldVisual.SetActive(false);
        Debug.Log("[TankEnemy] Shield OFF");
    }

    public override void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        if (isShieldActive)
        {
            // Phản damage ngược lại player
            ReflectDamageToPlayer(damage, hitDirection);
            // Vẫn flash để player biết đạn trúng shield
            if (gameObject.activeInHierarchy)
                StartCoroutine(FlashDamage());
            return;
        }

        // Không có shield: nhận damage bình thường
        base.TakeDamage(damage, hitPoint, hitDirection);
    }

    private void ReflectDamageToPlayer(float originalDamage, Vector3 incomingDirection)
    {
        if (tankConfig == null) return;

        float reflectDamage = originalDamage * tankConfig.reflectDamagePercent;

        // Tìm player
        Collider[] players = Physics.OverlapSphere(transform.position, 50f, playerLayer);
        foreach (var col in players)
        {
            IDamageable playerDmg = col.GetComponent<IDamageable>();
            if (playerDmg != null && !playerDmg.IsDead())
            {
                // Hướng từ tank về phía player
                Vector3 reflectDir = (col.transform.position - transform.position).normalized;
                playerDmg.TakeDamage(reflectDamage, transform.position, reflectDir);
                Debug.Log($"[TankEnemy] Phản {reflectDamage:F0} damage về player!");
                return;
            }
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        // Reset shield state mỗi khi spawn
        isShieldActive = false;
        shieldTimer = tankConfig != null ? tankConfig.shieldCycleNormal : 4f;
        if (shieldVisual != null) shieldVisual.SetActive(false);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (isShieldActive)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 1.2f);
        }
    }
}
