using UnityEngine;
using System.Collections.Generic;

public enum PoolType
{
    None = 0,
    PlayerProjectile = 1,
    EnemyProjectile = 2,
    MeleeEnemy = 3,
    RangedEnemy = 4,
}

[System.Serializable]
public class Pool
{
    public PoolType poolType;
    public int initialSize = 10;
    public GameObject prefab;
}

public class ObjectPool : MonoBehaviour
{
    private Dictionary<PoolType, Queue<GameObject>> poolDictionary;
    private Dictionary<PoolType, Pool> poolConfigs;
    private Dictionary<PoolType, Transform> poolParents;

    [SerializeField] private List<Pool> pools = new List<Pool>();

    public static ObjectPool Instance;

    void Awake()
    {
        Instance = this;
        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<PoolType, Queue<GameObject>>();
        poolConfigs = new Dictionary<PoolType, Pool>();
        poolParents = new Dictionary<PoolType, Transform>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null) continue;

            Queue<GameObject> objectPool = new Queue<GameObject>();
            poolConfigs[pool.poolType] = pool;

            GameObject parentObj = new GameObject($"Pool_{pool.poolType}");
            Transform parent = parentObj.transform;
            parent.SetParent(transform);
            poolParents[pool.poolType] = parent;

            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewObject(pool.prefab, parent);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.poolType, objectPool);
        }
    }

    private GameObject CreateNewObject(GameObject prefab, Transform parent)
    {
        GameObject obj = Instantiate(prefab, parent);
        obj.SetActive(false);
        return obj;
    }

    public GameObject Spawn(PoolType poolType, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(poolType))
        {
            Debug.LogWarning($"Pool with type '{poolType}' doesn't exist!");
            return null;
        }

        GameObject objectToSpawn;
        Queue<GameObject> pool = poolDictionary[poolType];

        if (pool.Count > 0)
        {
            objectToSpawn = pool.Dequeue();
        }
        else
        {
            Pool poolConfig = poolConfigs[poolType];

            objectToSpawn = CreateNewObject(poolConfig.prefab, poolParents[poolType]);
        }

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        return objectToSpawn;
    }

    public GameObject Spawn(PoolType poolType, Vector3 position)
    {
        return Spawn(poolType, position, Quaternion.identity);
    }

    public void Despawn(GameObject obj, PoolType poolType)
    {
        if (obj == null) return;

        if (!poolDictionary.ContainsKey(poolType))
        {
            Debug.LogWarning($"Pool with type '{poolType}' doesn't exist!");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);

        Pool poolConfig = poolConfigs[poolType];
        obj.transform.SetParent(poolParents[poolType]);
        poolDictionary[poolType].Enqueue(obj);
    }

    public void DespawnAfterDelay(GameObject obj, PoolType poolType, float delay)
    {
        if (obj != null)
        {
            StartCoroutine(DespawnCoroutine(obj, poolType, delay));
        }
    }

    private System.Collections.IEnumerator DespawnCoroutine(GameObject obj, PoolType poolType, float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn(obj, poolType);
    }
}
