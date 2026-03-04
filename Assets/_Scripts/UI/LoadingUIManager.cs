using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using Roguelike.Systems.Leaderboard;

public class LoadingUIManager : Singleton<LoadingUIManager>
{
    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private CanvasGroup loadingCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private void Start()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 1f;
                loadingCanvasGroup.blocksRaycasts = true;
            }
        }

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
        StartCoroutine(FadeOutLoading());
    }

    private IEnumerator FadeOutLoading()
    {
        if (loadingCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                yield return null;
            }
            loadingCanvasGroup.blocksRaycasts = false;
        }
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    public void ShowLoadingAndRestart()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 1f;
                loadingCanvasGroup.blocksRaycasts = true;
            }
        }
        
        StartCoroutine(RestartSceneRoutine());
    }

    private IEnumerator RestartSceneRoutine()
    {
        yield return null;
        
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
