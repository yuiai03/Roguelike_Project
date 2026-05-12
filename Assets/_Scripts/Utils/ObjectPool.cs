using System.Collections.Generic;
using UnityEngine;

public enum PoolType
{
    None = 0,
    PlayerProjectile = 1,
    Enemy_Projectile = 2,
    Enemy_Melee = 3,
    Enemy_Ranged = 4,
    Enemy_SpawnCircle = 5,
    OrbitingBall = 6,
    AoEExplosionVFX = 7,
    SpiritPierce = 8,
    SpiritExplosion = 9,
    DamageText = 10,
    SpiritProjectile = 11,
    Enemy_Fly = 15,

    // LawaChurl Bosses
    Boss_Geo = 16,
    Boss_Pyro = 17,
    Boss_Electro = 18,

    // Boss Attacks & Effects
    Boss_WarningCircle = 19,
    Boss_ElectroBomb_Big = 20,
    Boss_ElectroBomb_Small = 21,
    Boss_GeoRock = 22,
    Boss_PyroEffect = 23,

    // Spirits
    SpiritHealing = 25,
    SpiritTripleShot = 26,
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
    private Dictionary<PoolType, HashSet<GameObject>> activeObjects;

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
        activeObjects = new Dictionary<PoolType, HashSet<GameObject>>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null) continue;

            Queue<GameObject> objectPool = new Queue<GameObject>();
            poolConfigs[pool.poolType] = pool;
            activeObjects[pool.poolType] = new HashSet<GameObject>();

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
        activeObjects[poolType].Add(objectToSpawn);

        return objectToSpawn;
    }

    public GameObject Spawn(PoolType poolType, Vector3 position)
    {
        return Spawn(poolType, position, Quaternion.identity);
    }

    public int GetAvailableCount(PoolType poolType)
    {
        if (poolDictionary == null || !poolDictionary.TryGetValue(poolType, out Queue<GameObject> pool))
        {
            return 0;
        }

        return pool.Count;
    }

    public void Prewarm(PoolType poolType, int requiredAvailableCount)
    {
        if (requiredAvailableCount <= 0)
        {
            return;
        }

        if (poolDictionary == null || !poolDictionary.ContainsKey(poolType))
        {
            Debug.LogWarning($"Pool with type '{poolType}' doesn't exist!");
            return;
        }

        Queue<GameObject> pool = poolDictionary[poolType];
        if (pool.Count >= requiredAvailableCount)
        {
            return;
        }

        Pool poolConfig = poolConfigs[poolType];
        Transform parent = poolParents[poolType];
        int missingCount = requiredAvailableCount - pool.Count;

        for (int i = 0; i < missingCount; i++)
        {
            GameObject obj = CreateNewObject(poolConfig.prefab, parent);
            pool.Enqueue(obj);
        }
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

        if (activeObjects.TryGetValue(poolType, out HashSet<GameObject> activeSet))
        {
            bool wasActive = activeSet.Remove(obj);
            if (!wasActive && poolDictionary[poolType].Contains(obj))
            {
                return;
            }
        }

        obj.SetActive(false);
        obj.transform.SetParent(poolParents[poolType]);
        poolDictionary[poolType].Enqueue(obj);
    }

    public void DespawnAllActiveObjects()
    {
        StopAllCoroutines();

        foreach (KeyValuePair<PoolType, HashSet<GameObject>> entry in activeObjects)
        {
            List<GameObject> objectsToDespawn = new List<GameObject>(entry.Value);
            foreach (GameObject obj in objectsToDespawn)
            {
                if (obj != null)
                {
                    Despawn(obj, entry.Key);
                }
            }
        }
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
