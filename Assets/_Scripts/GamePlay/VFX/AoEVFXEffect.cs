using UnityEngine;

public class AoEVFXEffect : MonoBehaviour
{
    [SerializeField] private float duration = 1.5f;

    private void OnEnable()
    {
        ObjectPool.Instance.DespawnAfterDelay(gameObject, PoolType.AoEExplosionVFX, duration);
    }
}
