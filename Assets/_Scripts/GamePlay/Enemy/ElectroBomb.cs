using UnityEngine;
using System.Collections;

public class ElectroBomb : MonoBehaviour
{
    private float damage;
    private float dropDuration;
    private Vector3 startPos;
    private Vector3 targetPos;
    private LayerMask damageLayer;
    private GameObject owner;
    private bool isBigBomb;

    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private PoolType smallBombPoolType = PoolType.ElectroBomb_Small;
    [SerializeField] private int smallBombCount = 6;
    [SerializeField] private float smallBombSpreadRadius = 5f;

    public void Initialize(float damageAmount, float duration, Vector3 target, LayerMask layer, GameObject sourceOwner, bool isBig)
    {
        damage = damageAmount;
        dropDuration = duration;
        startPos = transform.position;
        targetPos = target;
        damageLayer = layer;
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
            
            // Apply ease-in for a gravity effect: t * t
            transform.position = Vector3.Lerp(startPos, targetPos, t * t);
            yield return null;
        }

        transform.position = targetPos;
        Explode();
    }

    private void Explode()
    {
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
        PoolType poolType = isBigBomb ? PoolType.ElectroBomb_Big : PoolType.ElectroBomb_Small;
        ObjectPool.Instance.Despawn(gameObject, poolType);
    }

    private void SpawnSmallBombs()
    {
        for (int i = 0; i < smallBombCount; i++)
        {
            // Random point around explosion
            Vector2 rand2D = Random.insideUnitCircle * smallBombSpreadRadius;
            Vector3 spawnTarget = targetPos + new Vector3(rand2D.x, 0, rand2D.y);

            // Spawn Warning Circle for small bomb
            GameObject warningObj = ObjectPool.Instance.Spawn(PoolType.WarningCircle, spawnTarget, Quaternion.identity);
            if (warningObj != null)
            {
                WarningCircle wc = warningObj.GetComponent<WarningCircle>();
                if (wc != null)
                {
                    wc.OnWarningComplete.RemoveAllListeners();
                    wc.StartWarning(dropDuration * 0.7f); // slightly faster than big bomb
                }
            }

            // Spawn Small Bomb
            Vector3 smallStartPos = spawnTarget + Vector3.up * (transform.position.y - targetPos.y) * 0.7f; // starting slightly lower
            GameObject smallBombObj = ObjectPool.Instance.Spawn(smallBombPoolType, smallStartPos, Quaternion.identity);
            
            ElectroBomb smallBomb = smallBombObj.GetComponent<ElectroBomb>();
            if (smallBomb != null)
            {
                // Damaged decreased for small bombs, 1/3 of the big bomb
                smallBomb.Initialize(damage * 0.33f, dropDuration * 0.7f, spawnTarget, damageLayer, owner, false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
