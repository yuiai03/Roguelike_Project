using UnityEngine;
using System.Collections;

public class ElectroBomb : MonoBehaviour
{
    private const float WarningVisualLift = 0.05f;

    private float damage;
    private float dropDuration;
    private Vector3 startPos;
    private Vector3 targetPos;
    private LayerMask damageLayer;
    private LayerMask groundMask;
    private GameObject owner;
    private bool isBigBomb;

    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private PoolType smallBombPoolType = PoolType.Boss_ElectroBomb_Small;
    [SerializeField] private int smallBombCount = 6;
    [SerializeField] private float smallBombSpreadRadius = 5f;

    public void Initialize(
        float damageAmount,
        float duration,
        Vector3 target,
        LayerMask layer,
        LayerMask groundLayerMask,
        GameObject sourceOwner,
        bool isBig)
    {
        damage = damageAmount;
        dropDuration = duration;
        startPos = transform.position;
        targetPos = Utils.GetGroundPosition(target, groundLayerMask);
        damageLayer = layer;
        groundMask = groundLayerMask;
        owner = sourceOwner;
        isBigBomb = isBig;

        StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        float timer = 0f;
        while (timer < dropDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / dropDuration);
            
            // XZ interpolation
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            
            // Y interpolation (Parabola)
            float heightScale = isBigBomb ? 5f : 3f; // Peak height adjusting
            float parabolaY = Mathf.Sin(t * Mathf.PI) * heightScale; 
            currentPos.y += parabolaY;

            transform.position = currentPos;
            yield return null;
        }

        transform.position = targetPos;
        Explode();
    }

    private void Explode()
    {
        AudioManager.Instance?.PlayWorldSfx(AudioCue.BossBoom);

        // AoE Damage
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, damageLayer);
        foreach (Collider hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead())
            {
                Vector3 hitDir = (hit.transform.position - transform.position).normalized;
                hitDir.y = 0;
                damageable.TakeDamage(damage, hit.ClosestPoint(transform.position), hitDir);
            }
        }

        // Optional VFX / Screen shake
        // ...

        if (isBigBomb)
        {
            SpawnSmallBombs();
        }

        // Despawn this bomb
        PoolType poolType = isBigBomb ? PoolType.Boss_ElectroBomb_Big : PoolType.Boss_ElectroBomb_Small;
        ObjectPool.Instance.Despawn(gameObject, poolType);
    }

    private void SpawnSmallBombs()
    {
        for (int i = 0; i < smallBombCount; i++)
        {
            // Random point around explosion
            Vector2 rand2D = Random.insideUnitCircle * smallBombSpreadRadius;
            Vector3 spawnTarget = targetPos + new Vector3(rand2D.x, 0f, rand2D.y);
            spawnTarget = Utils.GetGroundPosition(spawnTarget, groundMask);
            Vector3 warningSpawnPos = spawnTarget + Vector3.up * WarningVisualLift;

            // Spawn Warning Circle for small bomb
            GameObject warningObj = ObjectPool.Instance.Spawn(PoolType.Boss_WarningCircle, warningSpawnPos, Quaternion.identity);
            if (warningObj != null)
            {
                WarningCircle wc = warningObj.GetComponent<WarningCircle>();
                if (wc != null)
                {
                    wc.OnWarningComplete.RemoveAllListeners();
                    wc.StartWarning(dropDuration * 0.7f); // slightly faster than big bomb
                }
            }

            // Spawn Small Bomb starting from the big bomb's current impact position
            Vector3 smallStartPos = transform.position;
            GameObject smallBombObj = ObjectPool.Instance.Spawn(smallBombPoolType, smallStartPos, Quaternion.identity);
            
            ElectroBomb smallBomb = smallBombObj.GetComponent<ElectroBomb>();
            if (smallBomb != null)
            {
                // Damage decreased for small bombs, 1/3 of the big bomb
                smallBomb.Initialize(damage * 0.33f, dropDuration * 0.7f, spawnTarget, damageLayer, groundMask, owner, false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
