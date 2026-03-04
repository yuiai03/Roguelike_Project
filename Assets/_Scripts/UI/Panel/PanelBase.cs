using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Base class cho tất cả các Panel.
/// - Root gameObject: bật/tắt toàn bộ panel (kể cả bg).
/// - menu child: CanvasGroup fade in/out để animate nội dung.
/// </summary>
public abstract class PanelBase : MonoBehaviour
{
    [Header("Panel Base")]
    [SerializeField] protected GameObject menu;
    [SerializeField] protected float showDuration = 0.3f;
    [SerializeField] protected float hideDuration = 0.1f;

    public bool IsOpen => menu.activeSelf;

    protected virtual void Awake()
    {
        menu.SetActive(false);
    }

    public virtual void Show(Action onComplete = null)
    {
        menu.SetActive(true);

        CanvasGroup canvasGroup = GetOrAddCG(gameObject);
        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.DOFade(1f, showDuration).SetUpdate(true).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    public virtual void Hide(Action onComplete = null)
    {
        CanvasGroup canvasGroup = GetOrAddCG(gameObject);
        DOTween.Kill(canvasGroup);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(0f, hideDuration).SetUpdate(true).OnComplete(() =>
        {
            menu.SetActive(false);
            onComplete?.Invoke();
        });
    }

    protected CanvasGroup GetOrAddCG(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
}
