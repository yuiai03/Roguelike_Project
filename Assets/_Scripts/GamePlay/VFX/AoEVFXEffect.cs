using UnityEngine;

/// <summary>
/// Quản lý VFX nổ AoE. 
/// Tự động trả về ObjectPool sau khoảng thời gian duration.
/// </summary>
public class AoEVFXEffect : MonoBehaviour
{
    [SerializeField] private float duration = 1.5f;

    private void OnEnable()
    {
        ObjectPool.Instance.DespawnAfterDelay(gameObject, PoolType.AoEExplosionVFX, duration);
    }
}
