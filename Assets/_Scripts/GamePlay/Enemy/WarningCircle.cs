using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class WarningCircle : MonoBehaviour
{
    [SerializeField] private Transform outerCircle;
    [SerializeField] private Transform innerCircle;
    [SerializeField] private float defaultDuration = 2f;
    
    public UnityEvent OnWarningComplete;
    
    private Coroutine warningCoroutine;

    private Vector3 originalOuterScale;
    private Vector3 originalInnerScale;

    private void Awake()
    {
        if (outerCircle != null) originalOuterScale = outerCircle.localScale;
        if (innerCircle != null) originalInnerScale = innerCircle.localScale;
    }

    private void OnDisable()
    {
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningCoroutine = null;
        }
        if (outerCircle != null) outerCircle.localScale = originalOuterScale;
        if (innerCircle != null) innerCircle.localScale = originalInnerScale;
    }

    /// <param name="duration">Thời gian animation. Dùng defaultDuration nếu <= 0.</param>
    /// <param name="targetScale">Scale đích của circle. Nếu là null, dùng outerCircle.localScale mặc định.</param>
    public void StartWarning(float duration = -1f, float? targetScale = null)
    {
        if (duration <= 0f) duration = defaultDuration;
        
        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
            
        warningCoroutine = StartCoroutine(WarningRoutine(duration, targetScale));
    }

    private IEnumerator WarningRoutine(float duration, float? overrideScale = null)
    {
        if (outerCircle == null || innerCircle == null)
        {
            Debug.LogError("WarningCircle is missing references to outer or inner circles.");
            yield break;
        }

        float multiplier = overrideScale ?? 1f;
        Vector3 outerTarget = originalOuterScale * multiplier;
        Vector3 innerTarget = originalInnerScale * multiplier;
        outerCircle.localScale = outerTarget;
        innerCircle.localScale = Vector3.zero;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            innerCircle.localScale = Vector3.Lerp(Vector3.zero, innerTarget, t);
            yield return null;
        }

        innerCircle.localScale = innerTarget;
        OnWarningComplete?.Invoke();
        
        PoolType poolType = GetComponent<PoolTypeConfig>()?.poolType ?? PoolType.WarningCircle;
        ObjectPool.Instance.Despawn(gameObject, poolType);
    }
}
