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
