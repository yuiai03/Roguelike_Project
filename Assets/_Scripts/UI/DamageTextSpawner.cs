using UnityEngine;

/// <summary>
/// Singleton hỗ trợ gọi spawn Damage text tiện lợi từ bất cứ đâu.
/// Nằm trên object trong scene (VD: GameManager/UI Manager)
/// </summary>
public class DamageTextSpawner : MonoBehaviour
{
    public static DamageTextSpawner Instance { get; private set; }

    [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0.5f, 0f);
    [SerializeField] private float spawnRadius = 0.5f; // random theo XZ xung quanh target

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Spawn một text damage/heal tại vị trí chỉ định
    /// </summary>
    public void Spawn(float amount, Vector3 worldPos, bool isHeal = false, bool isPlayer = false, bool isCrit = false)
    {
        if (amount <= 0 || ObjectPool.Instance == null) return;

        // Random vị trí spawn xung quanh target (XZ)
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 radialOffset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Random.Range(0f, spawnRadius);

        // Thêm offset ngẫu nhiên nhỏ phía trên
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
