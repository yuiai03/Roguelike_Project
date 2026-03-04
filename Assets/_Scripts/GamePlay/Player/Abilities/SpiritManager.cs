using UnityEngine;
using System.Collections.Generic;

public class SpiritManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float followDistance = 1.5f;

    private readonly List<Spirit> spirits = new List<Spirit>();

    public void AddSpirit(SpiritType type)
    {
        if (HasSpiritOfType(type))
        {
            Debug.Log($"[SpiritManager] Đã có tinh linh loại {type}, bỏ qua.");
            return;
        }

        PoolType poolType = type == SpiritType.Pierce ? PoolType.SpiritPierce : PoolType.SpiritExplosion;
        GameObject obj = ObjectPool.Instance.Spawn(poolType, transform.position, Quaternion.identity);

        if (obj == null) return;
        obj.transform.parent = null; 

        Spirit spirit = obj.GetComponent<Spirit>();
        if (spirit == null)
        {
            Destroy(obj);
            Debug.LogError("[SpiritManager] Spirit prefab thiếu component Spirit!");
            return;
        }

        spirit.spiritType = type;
        spirit.poolType = poolType;
        spirit.enemyLayer = enemyLayer;

        float startAngle = spirits.Count > 0 ? (360f / (spirits.Count + 1)) * spirits.Count : 0f;
        spirit.Initialize(transform, startAngle, enemyLayer);

        spirits.Add(spirit);
        RecalculateOrbitAngles();

        Debug.Log($"[SpiritManager] Added {type} spirit. Total: {spirits.Count}");
    }

    private void RecalculateOrbitAngles()
    {
        if (spirits.Count == 0) return;
        float step = 360f / spirits.Count;
        for (int i = 0; i < spirits.Count; i++)
        {
            if (spirits[i] != null)
                spirits[i].Initialize(transform, step * i, enemyLayer);
        }
    }

    void Update()
    {
        spirits.RemoveAll(s => s == null);
    }

    public int GetSpiritCount() => spirits.Count;

    public bool HasSpiritOfType(SpiritType type)
    {
        foreach (var s in spirits)
        {
            if (s != null && s.spiritType == type) return true;
        }
        return false;
    }
}
