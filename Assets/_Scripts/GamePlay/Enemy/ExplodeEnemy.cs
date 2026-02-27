using UnityEngine;
using System.Collections;

/// <summary>
/// Exploder Enemy — Kamikaze.
/// Khi vào tầm: dừng lại, show indicator 0.8s → lao thẳng về player →
/// nổ khi chạm vật cản/hết tầm. Nếu bị giết trong lúc charge → nổ nhỏ.
/// </summary>
public class ExplodeEnemy : Enemy
{
    [Header("Exploder Visual")]
    [SerializeField] private LineRenderer chargeIndicator; // Gán LineRenderer trong Inspector
    [SerializeField] private ParticleSystem explosionParticle;

    private ExplodeEnemyConfig explodeConfig;

    private enum ExploderState { Idle, Warning, Charging, Exploded }
    private ExploderState exploderState = ExploderState.Idle;

    private Vector3 chargeDirection;
    private Vector3 chargeStartPos;
    private bool hasExploded;

    protected override void Awake()
    {
        base.Awake();
        explodeConfig = GetComponent<EnemyData>()?.GetConfig<ExplodeEnemyConfig>();
    }

    protected override void UpdateAI()
    {
        if (player == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (exploderState)
        {
            case ExploderState.Idle:
                if (distanceToPlayer <= (explodeConfig != null ? explodeConfig.detectionRange : 6f))
                    StartCoroutine(ChargeSequence());
                else
                    MoveTowardsPlayer();
                break;

            case ExploderState.Charging:
                PerformCharge();
                break;
        }
    }

    private IEnumerator ChargeSequence()
    {
        if (exploderState != ExploderState.Idle) yield break;
        exploderState = ExploderState.Warning;

        // Hướng lao = hướng đến player tại điểm bắt đầu cảnh báo
        chargeDirection = (player.position - transform.position).normalized;
        chargeDirection.y = 0f;
        chargeStartPos  = transform.position;

        // Hiển thị indicator
        ShowChargeIndicator();

        float warningTime = explodeConfig != null ? explodeConfig.warningDuration : 0.8f;
        yield return new WaitForSeconds(warningTime);

        HideChargeIndicator();

        if (isDead) yield break;

        exploderState = ExploderState.Charging;
    }

    private void PerformCharge()
    {
        float speed = explodeConfig != null ? explodeConfig.chargeSpeed : 18f;
        float maxDist = explodeConfig != null ? explodeConfig.chargeDistance : 8f;

        Vector3 move = chargeDirection * speed * Time.deltaTime;
        if (controller != null && controller.enabled)
            controller.Move(move);
        else
            transform.position += move;

        // Kiểm tra đã đi đủ khoảng cách chưa
        if (Vector3.Distance(transform.position, chargeStartPos) >= maxDist)
        {
            Explode(1f);
        }
    }

    private void ShowChargeIndicator()
    {
        if (chargeIndicator == null) return;
        float chargeMaxDist = explodeConfig != null ? explodeConfig.chargeDistance : 8f;
        chargeIndicator.enabled = true;
        chargeIndicator.positionCount = 2;
        chargeIndicator.SetPosition(0, transform.position + Vector3.up * 0.5f);
        chargeIndicator.SetPosition(1, transform.position + chargeDirection * chargeMaxDist + Vector3.up * 0.5f);
    }

    private void HideChargeIndicator()
    {
        if (chargeIndicator != null)
            chargeIndicator.enabled = false;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (exploderState != ExploderState.Charging) return;
        if (hit.gameObject.CompareTag("Player") || hit.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            Explode(1f);
        }
    }

    private void Explode(float damageMult)
    {
        if (hasExploded) return;
        hasExploded = true;
        exploderState = ExploderState.Exploded;
        HideChargeIndicator();

        float radius = explodeConfig != null ? explodeConfig.explosionRadius : 3f;
        float baseDmg = explodeConfig != null ? explodeConfig.explosionDamage : 50f;
        float dmg = baseDmg * damageMult;

        // Spawn explosion particle
        if (explosionParticle != null)
            Instantiate(explosionParticle, transform.position, Quaternion.identity);

        // Gây damage AoE
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var col in hits)
        {
            if (col.CompareTag("Player"))
            {
                IDamageable playerDmg = col.GetComponent<IDamageable>();
                if (playerDmg != null && !playerDmg.IsDead())
                {
                    Vector3 dir = (col.transform.position - transform.position).normalized;
                    playerDmg.TakeDamage(dmg, transform.position, dir);
                    Debug.Log($"[ExplodeEnemy] Nổ! Gây {dmg:F0} damage cho player");
                }
            }
        }

        Die();
    }

    /// <summary>Override TakeDamage — nếu bị giết khi đang charge → nổ nhỏ</summary>
    public override void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        base.TakeDamage(damage, hitPoint, hitDirection);
        if (isDead && exploderState == ExploderState.Charging && !hasExploded)
        {
            float mult = explodeConfig != null ? explodeConfig.deathExplosionMult : 0.5f;
            Explode(mult);
        }
    }

    private void OnEnable()
    {
        exploderState = ExploderState.Idle;
        hasExploded = false;
        HideChargeIndicator();
    }

    void OnDrawGizmosSelected()
    {
        if (explodeConfig == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explodeConfig.detectionRange);
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, explodeConfig.explosionRadius);
    }
}
