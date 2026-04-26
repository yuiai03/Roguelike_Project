using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Roguelike.Systems.Leaderboard;
using System;

public class LoadingUIManager : Singleton<LoadingUIManager>
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private CanvasGroup loadingCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    public bool IsBlocking => loadingPanel != null && loadingPanel.activeSelf;

    private Coroutine activeTransitionRoutine;
    private Coroutine startupFadeRoutine;
    private bool startupOverlayHidden;

    protected override void Awake()
    {
        base.Awake();
        InitializeOverlayVisible();
    }

    private void Start()
    {
        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.OnProfileLoadedEvent += HideLoading;
        }
        else
        {
            HideLoading();
        }
    }

    private void OnDestroy()
    {
        if (PlayFabLeaderboardManager.Instance != null)
        {
            PlayFabLeaderboardManager.Instance.OnProfileLoadedEvent -= HideLoading;
        }
    }

    public void HideLoading()
    {
        if (startupOverlayHidden)
        {
            return;
        }

        if (activeTransitionRoutine != null)
        {
            return;
        }

        if (startupFadeRoutine == null)
        {
            startupFadeRoutine = StartCoroutine(FadeOutLoading());
        }
    }

    private IEnumerator FadeOutLoading()
    {
        if (loadingCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                yield return null;
            }
        }

        SetOverlayHidden();
        startupFadeRoutine = null;
    }

    public void ShowLoadingAndRestart()
    {
        ShowBlackFadeAndRestart();
    }

    public void ShowBlackFadeAndRestart()
    {
        PlayBlackTransition(RestartScene, null, fadeDuration, 0f, 0f);
    }

    public void PlayBlackTransition(
        Action midSwap,
        Action onComplete,
        float fadeIn = 0.4f,
        float hold = 0.2f,
        float fadeOut = 0.4f)
    {
        if (startupFadeRoutine != null)
        {
            StopCoroutine(startupFadeRoutine);
            startupFadeRoutine = null;
        }

        if (activeTransitionRoutine != null)
        {
            StopCoroutine(activeTransitionRoutine);
        }

        activeTransitionRoutine = StartCoroutine(PlayBlackTransitionRoutine(midSwap, onComplete, fadeIn, hold, fadeOut));
    }

    private IEnumerator PlayBlackTransitionRoutine(
        Action midSwap,
        Action onComplete,
        float fadeIn,
        float hold,
        float fadeOut)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.blocksRaycasts = true;
            yield return FadeCanvasGroup(loadingCanvasGroup.alpha, 1f, fadeIn);
        }
        else if (fadeIn > 0f)
        {
            yield return new WaitForSecondsRealtime(fadeIn);
        }

        midSwap?.Invoke();

        if (hold > 0f)
        {
            yield return new WaitForSecondsRealtime(hold);
        }

        if (loadingCanvasGroup != null)
        {
            yield return FadeCanvasGroup(loadingCanvasGroup.alpha, 0f, fadeOut);
        }
        else if (fadeOut > 0f)
        {
            yield return new WaitForSecondsRealtime(fadeOut);
        }

        SetOverlayHidden();
        activeTransitionRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (loadingCanvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            loadingCanvasGroup.alpha = to;
            yield break;
        }

        float elapsedTime = 0f;
        loadingCanvasGroup.alpha = from;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(from, to, elapsedTime / duration);
            yield return null;
        }

        loadingCanvasGroup.alpha = to;
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void InitializeOverlayVisible()
    {
        startupOverlayHidden = false;

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 1f;
            loadingCanvasGroup.blocksRaycasts = true;
        }
    }

    private void SetOverlayHidden()
    {
        startupOverlayHidden = true;

        if (loadingCanvasGroup != null)
        {
            loadingCanvasGroup.alpha = 0f;
            loadingCanvasGroup.blocksRaycasts = false;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
}
