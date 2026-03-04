using UnityEngine;
using System.Collections;

public class TankEnemy : Enemy
{
    [Header("Tank Visual")]
    [SerializeField] private GameObject shieldVisual; 
    [SerializeField] private Color shieldColor = new Color(0.3f, 0.8f, 1f, 0.6f);

    private TankEnemyConfig tankConfig;

    private bool isShieldActive;
    private float shieldTimer;
    private Material shieldMaterial;

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

            ReflectDamageToPlayer(damage, hitDirection);

            if (gameObject.activeInHierarchy)
                StartCoroutine(FlashDamage());
            return;
        }

        base.TakeDamage(damage, hitPoint, hitDirection);
    }

    private void ReflectDamageToPlayer(float originalDamage, Vector3 incomingDirection)
    {
        if (tankConfig == null) return;

        float reflectDamage = originalDamage * tankConfig.reflectDamagePercent;

        Collider[] players = Physics.OverlapSphere(transform.position, 50f, playerLayer);
        foreach (var col in players)
        {
            IDamageable playerDmg = col.GetComponent<IDamageable>();
            if (playerDmg != null && !playerDmg.IsDead())
            {

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
