using UnityEngine;

public enum SpiritType
{
    Pierce,
    Explosion,
    Healing,
    TripleShot,
}

public class Spirit : MonoBehaviour
{
    [Header("Spirit Config")]
    public SpiritType spiritType;
    [HideInInspector] public PoolType poolType;

    [Header("Follow / Hover")]
    public float followSpeed = 5f;
    public float minFollowDist = 1.5f;
    public float maxFollowDist = 3.5f;
    public float hoverHeight = 1.5f;
    public float hoverAmplitude = 0.3f;
    public float hoverFrequency = 2f;

    [Header("Orbit & Drift")]
    [Tooltip("Orbit speed around the player in degrees per second.")]
    public float orbitSpeed = 30f;
    [Range(0f, 1f)]
    [Tooltip("Random x/z drift amount as a ratio of follow distance.")]
    public float driftAmplitude = 0.4f;
    [Tooltip("How quickly drift changes over time.")]
    public float driftFrequency = 0.35f;

    [Header("Attack")]
    public float attackInterval = 4f;
    public float attackRange = 15f;
    [Tooltip("Fallback power when PlayerData is unavailable.")]
    public float damage = 30f;
    public float projectileSpeed = 18f;
    public LayerMask enemyLayer;

    [Header("AoE (Explosion type)")]
    public float aoeRadius = 4f;

    [Header("Healing")]
    [SerializeField] private float healingInterval = 3.5f;
    [SerializeField] private float fullHealthTolerance = 0.01f;

    [Header("Triple Shot")]
    [SerializeField] private int tripleShotProjectileCount = 3;
    [SerializeField] private float tripleShotSpreadAngle = 12f;
    [SerializeField] private float retargetRetryFactor = 0.3f;

    private Transform player;
    private PlayerHealth ownerHealth;
    private float idOffset;
    private float attackTimer;
    private Vector3 currentVelocity;
    private bool isShooting;
    private PlayerData ownerData;
    private float attackDamageMultiplier = 1f;

    public float AttackDamageMultiplier => attackDamageMultiplier;

    public void Initialize(Transform playerTransform, float startAngle, LayerMask layer, PlayerData damageOwner, float atkMultiplier)
    {
        player = playerTransform;
        ownerHealth = playerTransform != null ? playerTransform.GetComponent<PlayerHealth>() : PlayerHealth.Instance;
        idOffset = startAngle;
        enemyLayer = layer;
        SetDamageSource(damageOwner, atkMultiplier);
        attackTimer = Random.Range(0f, GetPrimaryInterval());

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    public void SetDamageSource(PlayerData damageOwner, float atkMultiplier)
    {
        ownerData = damageOwner;
        attackDamageMultiplier = atkMultiplier;
    }

    void Update()
    {
        if (player == null) return;

        MoveToFollowTarget();

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        TryPerformAction();
    }

    private void MoveToFollowTarget()
    {
        float t = Time.time;

        float angle = idOffset + t * orbitSpeed;
        float rad = angle * Mathf.Deg2Rad;

        float targetDist = Mathf.Lerp(minFollowDist, maxFollowDist,
            Mathf.PerlinNoise(t * 0.15f + idOffset + 5f, 0f));

        Vector3 orbitPos = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * targetDist;

        float driftAmp = targetDist * driftAmplitude;
        float noiseX = (Mathf.PerlinNoise(t * driftFrequency + idOffset * 0.13f, 0f) * 2f - 1f) * driftAmp;
        float noiseZ = (Mathf.PerlinNoise(0f, t * driftFrequency + idOffset * 0.13f + 7.3f) * 2f - 1f) * driftAmp;

        Vector3 goal = player.position + orbitPos + new Vector3(noiseX, 0f, noiseZ);

        float bob = Mathf.Sin(t * hoverFrequency + idOffset) * hoverAmplitude;
        goal.y = player.position.y + hoverHeight + bob;

        transform.position = Vector3.SmoothDamp(transform.position, goal, ref currentVelocity, 1f / followSpeed);

        Vector3 flatVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        if (flatVel.sqrMagnitude > 0.05f && !isShooting)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatVel.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
        }
    }

    private void TryPerformAction()
    {
        if (spiritType == SpiritType.Healing)
        {
            PerformHealingTick();
            attackTimer = GetPrimaryInterval();
            return;
        }

        Transform enemy = FindNearestEnemy();
        if (enemy != null)
        {
            if (spiritType == SpiritType.TripleShot)
            {
                FireTripleShot(enemy);
            }
            else
            {
                FireSingleProjectile(enemy);
            }

            attackTimer = GetPrimaryInterval();
            return;
        }

        attackTimer = GetRetryInterval();
    }

    private void PerformHealingTick()
    {
        if (ownerHealth == null || ownerHealth.IsDead() || !PlayerNeedsHealing()) return;

        float healAmount = ResolveDamageOrPower();
        if (healAmount > 0f)
        {
            ownerHealth.Heal(healAmount);
        }
    }

    private void FireSingleProjectile(Transform target)
    {
        Vector3 dir = GetDirectionToTarget(target);
        if (dir == Vector3.zero) return;

        StartCoroutine(ShootAnimationCrt(dir));
        SpawnSpiritProjectile(dir, aoe: spiritType == SpiritType.Explosion, aoeRad: aoeRadius, pierce: spiritType == SpiritType.Pierce);
    }

    private void FireTripleShot(Transform target)
    {
        Vector3 dir = GetDirectionToTarget(target);
        if (dir == Vector3.zero) return;

        StartCoroutine(ShootAnimationCrt(dir));

        int projectileCount = GetTripleShotProjectileCount();
        if (projectileCount == 1)
        {
            SpawnSpiritProjectile(dir, aoe: false, aoeRad: 0f, pierce: false);
            return;
        }

        float spreadStep = GetTripleShotSpreadAngle();
        float halfCount = (projectileCount - 1) * 0.5f;
        for (int i = 0; i < projectileCount; i++)
        {
            float offsetAngle = (i - halfCount) * spreadStep;
            Vector3 shotDirection = Quaternion.AngleAxis(offsetAngle, Vector3.up) * dir;
            SpawnSpiritProjectile(shotDirection, aoe: false, aoeRad: 0f, pierce: false);
        }
    }

    private void SpawnSpiritProjectile(Vector3 directionToFire, bool aoe, float aoeRad, bool pierce)
    {
        if (directionToFire == Vector3.zero || ObjectPool.Instance == null) return;

        Vector3 spawnPos = transform.position;
        Quaternion rotation = Quaternion.LookRotation(directionToFire);
        GameObject projObj = ObjectPool.Instance.Spawn(PoolType.SpiritProjectile, spawnPos, rotation);
        if (projObj == null) return;

        float shotPower = ResolveDamageOrPower();
        SpiritProjectileScript proj = projObj.GetComponent<SpiritProjectileScript>();
        if (proj != null)
        {
            proj.Initialize(shotPower, projectileSpeed, 10f, directionToFire, enemyLayer, gameObject,
                aoe: aoe, aoeRad: aoeRad, pierce: pierce);
        }
        else
        {
            Projectile fallbackProjectile = projObj.GetComponent<Projectile>();
            fallbackProjectile?.Initialize(shotPower, projectileSpeed, 10f, directionToFire, enemyLayer, gameObject);
        }
    }

    private float ResolveDamageOrPower()
    {
        if (ownerData != null)
        {
            return ownerData.GetScaledAttackDamage(attackDamageMultiplier);
        }

        return damage * attackDamageMultiplier;
    }

    private Vector3 GetDirectionToTarget(Transform target)
    {
        if (target == null) return Vector3.zero;

        Vector3 spawnPos = transform.position;
        Vector3 targetFlat = target.position;
        targetFlat.y = spawnPos.y;
        return (targetFlat - spawnPos).normalized;
    }

    private bool PlayerNeedsHealing()
    {
        float tolerance = fullHealthTolerance > 0f ? fullHealthTolerance : 0.01f;
        return ownerHealth.GetCurrentHealth() + tolerance < ownerHealth.GetMaxHealth();
    }

    private float GetPrimaryInterval()
    {
        if (spiritType == SpiritType.Healing)
        {
            return healingInterval > 0f ? healingInterval : 3.5f;
        }

        return attackInterval > 0f ? attackInterval : 4f;
    }

    private float GetRetryInterval()
    {
        float retryFactor = retargetRetryFactor > 0f ? retargetRetryFactor : 0.3f;
        return Mathf.Max(0.1f, GetPrimaryInterval() * retryFactor);
    }

    private int GetTripleShotProjectileCount()
    {
        return tripleShotProjectileCount > 0 ? tripleShotProjectileCount : 3;
    }

    private float GetTripleShotSpreadAngle()
    {
        return tripleShotSpreadAngle > 0f ? tripleShotSpreadAngle : 12f;
    }

    private System.Collections.IEnumerator ShootAnimationCrt(Vector3 targetDirection)
    {
        isShooting = true;

        if (targetDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(targetDirection);
        }

        Vector3 origScale = Vector3.one;
        Vector3 targetScale = origScale * 1.5f;

        float t = 0f;
        float halfDuration = 0.1f;

        while (t < halfDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(origScale, targetScale, t / halfDuration);
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, origScale, t / halfDuration);
            yield return null;
        }

        transform.localScale = origScale;
        isShooting = false;
    }

    private Transform FindNearestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(player.position, attackRange, enemyLayer);
        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var col in enemies)
        {
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead()) continue;

            float distance = Vector3.Distance(player.position, col.transform.position);
            if (distance < minDist)
            {
                minDist = distance;
                nearest = col.transform;
            }
        }

        return nearest;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
