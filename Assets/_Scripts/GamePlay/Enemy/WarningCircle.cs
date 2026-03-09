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

    public void StartWarning(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        
        if (warningCoroutine != null)
            StopCoroutine(warningCoroutine);
            
        warningCoroutine = StartCoroutine(WarningRoutine(duration));
    }

    private IEnumerator WarningRoutine(float duration)
    {
        if (outerCircle == null || innerCircle == null)
        {
            Debug.LogError("WarningCircle is missing references to outer or inner circles.");
            yield break;
        }

        Vector3 targetScale = outerCircle.localScale;
        innerCircle.localScale = Vector3.zero;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            innerCircle.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }

        innerCircle.localScale = targetScale;
        OnWarningComplete?.Invoke();
        
        PoolType poolType = GetComponent<PoolTypeConfig>()?.poolType ?? PoolType.WarningCircle;
        ObjectPool.Instance.Despawn(gameObject, poolType);
    }
}
