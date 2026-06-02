using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
    [SerializeField] private string startButtonText = "B\u1EAFt \u0110\u1EA7u";
    [SerializeField] private string continueButtonText = "Ti\u1EBFp t\u1EE5c";

    public static Action OnClosed;
    public static Action onGameStart;

    private enum TutorialMode
    {
        StartChallenge,
        Onboarding
    }

    private Coroutine delayCoroutine;
    private TutorialMode currentMode = TutorialMode.StartChallenge;
    private Action onboardingContinueCallback;
    private TextMeshProUGUI startGameButtonLabel;

    protected override void Awake()
    {
        base.Awake();

        if (bg != null)
        {
            Button bgButton = bg.GetComponent<Button>();
            if (bgButton != null)
            {
                bgButton.onClick.AddListener(Dismiss);
            }
        }

        if (startGameButton != null)
        {
            startGameButtonLabel = startGameButton.GetComponentInChildren<TextMeshProUGUI>(true);
            startGameButton.onClick.AddListener(HandleActionButtonClicked);
            startGameButton.gameObject.SetActive(false);
        }
    }

    public void ShowTutorial()
    {
        onboardingContinueCallback = null;
        ShowTutorial(TutorialMode.StartChallenge);
    }

    public void ShowOnboardingTutorial(Action onContinue = null)
    {
        onboardingContinueCallback = onContinue;
        ShowTutorial(TutorialMode.Onboarding);
    }

    private void ShowTutorial(TutorialMode mode)
    {
        GameUI.Instance?.InteractPanel?.Hide();

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(false);
        }

        currentMode = mode;
        SetActionButtonText(mode == TutorialMode.Onboarding ? continueButtonText : startButtonText);

        if (bg != null)
        {
            bg.SetActive(true);
        }

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(false);
        }

        Show(onComplete: () =>
        {
            if (delayCoroutine != null)
            {
                StopCoroutine(delayCoroutine);
            }

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
            AudioManager.Instance?.PlayUISfx(AudioCue.ChallengeReady);
        }

        delayCoroutine = null;
    }

    public void Dismiss()
    {
        if (currentMode == TutorialMode.Onboarding)
        {
            return;
        }

        CloseTutorial(restoreInput: true, notifyClosed: true);
    }

    public void StartGame()
    {
        AudioManager.Instance?.PlayUISfx(AudioCue.ChallengeStart);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(true);
        }

        if (WaveSpawner.Instance != null)
        {
            onGameStart?.Invoke();
            WaveSpawner.Instance.StartNextWave();
        }
        else
        {
            Debug.LogError("WaveSpawner Instance khong tim thay!");
        }

        if (bg != null)
        {
            bg.SetActive(false);
        }

        if (menu != null)
        {
            menu.SetActive(false);
        }
    }

    private void HandleActionButtonClicked()
    {
        if (currentMode == TutorialMode.Onboarding)
        {
            AudioManager.Instance?.PlayUISfx(AudioCue.UiButtonClick);
            Action callback = onboardingContinueCallback;
            onboardingContinueCallback = null;
            CloseTutorial(restoreInput: true, notifyClosed: false, onClosed: callback);
            return;
        }

        StartGame();
    }

    private void CloseTutorial(bool restoreInput, bool notifyClosed, Action onClosed = null)
    {
        if (delayCoroutine != null)
        {
            StopCoroutine(delayCoroutine);
            delayCoroutine = null;
        }

        Hide(() =>
        {
            if (restoreInput && PlayerController.Instance != null)
            {
                PlayerController.Instance.SetInputActive(true);
            }

            if (notifyClosed)
            {
                OnClosed?.Invoke();
            }

            if (bg != null)
            {
                bg.SetActive(false);
            }

            onClosed?.Invoke();
        });
    }

    private void SetActionButtonText(string text)
    {
        if (startGameButtonLabel == null && startGameButton != null)
        {
            startGameButtonLabel = startGameButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (startGameButtonLabel != null)
        {
            startGameButtonLabel.text = text;
        }
    }
}
