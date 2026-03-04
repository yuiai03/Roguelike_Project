using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Roguelike.Systems.Leaderboard;

/// <summary>
/// Quan ly ChallengePanel -- co 2 obj con: bg (background) va menu (noi dung).
/// </summary>
public class ChallengePanel : PanelBase
{
    [Header("Tham chieu trong Menu")]

    [SerializeField] private GameObject bg;
    [SerializeField] private Button startGameButton;

    [Header("Cai dat")]
    [SerializeField] private float tutorialDelayTime = 1f;

    public static Action OnClosed;
    public static Action onGameStart;

    private Coroutine delayCoroutine;

    protected override void Awake()
    {
        base.Awake();

        if (bg != null) bg.GetComponent<Button>().onClick.AddListener(Dismiss);

        if (startGameButton != null) startGameButton.onClick.AddListener(StartGame);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
    }

    public void ShowTutorial()
    {
        GameUI.Instance?.InteractPanel?.Hide();

        PlayerController.Instance.SetInputActive(false);

        bg.SetActive(true);
        startGameButton.gameObject.SetActive(false);

        Show(onComplete: () =>
        {
            if (delayCoroutine != null) StopCoroutine(delayCoroutine);
            delayCoroutine = StartCoroutine(ShowStartButtonAfterDelay());
        });
    }


    private IEnumerator ShowStartButtonAfterDelay()
    {
        yield return new WaitForSecondsRealtime(tutorialDelayTime);
        if (startGameButton != null)
        {
            CanvasGroup cg = GetOrAddCG(startGameButton.gameObject);
            startGameButton.gameObject.SetActive(true);
            cg.alpha = 0f;
            cg.DOFade(1f, 0.4f).SetUpdate(true);
        }
        delayCoroutine = null;
    }

    public void Dismiss()
    {
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
            delayCoroutine = null;
        }

        Hide(() =>
        {
            PlayerController.Instance.SetInputActive(true);
            OnClosed?.Invoke();

            bg.SetActive(false);
        });
    }

    public void StartGame()
    {
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetInputActive(true);

        if (WaveSpawner.Instance != null)
        {
            onGameStart?.Invoke();
            WaveSpawner.Instance.StartNextWave();
        }
        else
        {
            Debug.LogError("WaveSpawner Instance khong tim thay!");
        }

        bg.SetActive(false);
        menu.SetActive(false);
    }
}