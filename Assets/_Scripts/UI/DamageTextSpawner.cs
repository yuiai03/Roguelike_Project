using UnityEngine;

public class DamageTextSpawner : MonoBehaviour
{
    public static DamageTextSpawner Instance { get; private set; }

    [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0.5f, 0f);
    [SerializeField] private float spawnRadius = 0.5f; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void Spawn(float amount, Vector3 worldPos, bool isHeal = false, bool isPlayer = false, bool isCrit = false)
    {
        if (amount <= 0 || ObjectPool.Instance == null) return;

        Vector3 radialOffset = new Vector3(Random.Range(-spawnRadius, spawnRadius), 0f, Random.Range(-spawnRadius, spawnRadius));

        Vector3 upOffset = new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(0f, randomOffset.y),
            0f
        );

        GameObject obj = ObjectPool.Instance.Spawn(PoolType.DamageText, worldPos + radialOffset + upOffset, Quaternion.identity);
        if (obj != null)
        {
            DamageText textComp = obj.GetComponent<DamageText>();
            if (textComp != null)
            {
                textComp.Show(amount, isHeal, isPlayer, isCrit);
            }
        }
    }
}
