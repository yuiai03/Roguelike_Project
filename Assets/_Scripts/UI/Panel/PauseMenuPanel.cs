using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuPanel : PanelBase
{
    private enum PauseView
    {
        Hidden,
        Main,
        Settings,
        Leaderboard
    }

    [Header("HUD")]
    [SerializeField] private GameObject hudPromptRoot;
    [SerializeField] private TextMeshProUGUI hudPromptText;

    [Header("Menu")]
    [SerializeField] private Image scrimImage;
    [SerializeField] private GameObject pauseCardRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI footerText;
    [SerializeField] private GameObject mainView;
    [SerializeField] private GameObject settingsPanelRoot;
    [SerializeField] private GameObject settingsView;
    [SerializeField] private TextMeshProUGUI settingsTitleText;
    [SerializeField] private TextMeshProUGUI settingsFooterText;

    [Header("Main Buttons")]
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonLabel;

    [Header("Settings Controls")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI musicValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private Button settingsBackButton;

    [Header("Animation")]
    [SerializeField] private float viewFadeDuration = 0.18f;

    [Header("Visuals")]
    [SerializeField] private Color scrimColor = new Color(0.04f, 0.04f, 0.04f, 0.84f);
    [SerializeField] private Color accentColor = new Color(0.96f, 0.78f, 0.33f, 1f);
    [SerializeField] private Color buttonColor = new Color(0.21f, 0.21f, 0.23f, 1f);
    [SerializeField] private Color buttonHighlightColor = new Color(0.29f, 0.29f, 0.31f, 1f);

    private PauseView currentView = PauseView.Hidden;
    private bool isBusy;
    private bool gameplayHudEnabled;

    protected override void Awake()
    {
        ResolveSceneReferences();

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        base.Awake();
        ApplyStaticVisuals();
        SetHudVisible(false);
        ShowMainViewInternal(null);
        currentView = PauseView.Hidden;

        leaderboardButton.onClick.AddListener(OpenLeaderboardFromPause);
        settingsButton.onClick.AddListener(OpenSettings);
        actionButton.onClick.AddListener(HandleActionButton);
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(BackToMainFromSettings);
        }
        musicSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
        sfxSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
    }

    private void OnEnable()
    {
        LeaderboardPanel.OnClosed += HandleLeaderboardClosed;
        ChallengePanel.onGameStart += HandleGameStarted;
    }

    private void OnDisable()
    {
        LeaderboardPanel.OnClosed -= HandleLeaderboardClosed;
        ChallengePanel.onGameStart -= HandleGameStarted;
    }

    private void Update()
    {
        RefreshHudVisibility();

        if (Keyboard.current == null || isBusy || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (!IsOpen)
        {
            if (CanOpenPauseMenu())
            {
                PauseGame();
            }

            return;
        }

        switch (currentView)
        {
            case PauseView.Main:
                ResumeGame();
                break;
            case PauseView.Settings:
                BackToMainFromSettings();
                break;
            case PauseView.Leaderboard:
                CloseLeaderboardToMain();
                break;
        }
    }

    public override void Show(Action onComplete = null)
    {
        menu.SetActive(true);

        CanvasGroup canvasGroup = GetOrAddCG(menu);
        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = 0f;
        SetPauseMenuOverlayState(true);
        canvasGroup.DOFade(1f, showDuration).SetUpdate(true).OnComplete(() => onComplete?.Invoke());
    }

    public override void Hide(Action onComplete = null)
    {
        CanvasGroup canvasGroup = GetOrAddCG(menu);
        DOTween.Kill(canvasGroup);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.DOFade(0f, hideDuration).SetUpdate(true).OnComplete(() =>
        {
            menu.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private void PauseGame()
    {
        if (IsOpen)
        {
            return;
        }

        isBusy = false;
        currentView = PauseView.Main;
        UpdateActionButtonLabel();
        SetHudVisible(false);
        GameUI.Instance?.InteractPanel?.Hide();
        ShowMainViewInternal(leaderboardButton);

        AudioManager.Instance?.SetMusicDucked(true);
        AudioManager.Instance?.PlayUISfx(AudioCue.UiPauseOpen);

        Time.timeScale = 0f;
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetInputActive(false);
        }

        Show();
    }

    private void ResumeGame()
    {
        if (!IsOpen)
        {
            return;
        }

        if (GameUI.Instance?.LeaderboardPanel?.IsOpen == true)
        {
            GameUI.Instance.LeaderboardPanel.HideWithoutInputRestore();
        }

        AudioManager.Instance?.PlayUISfx(AudioCue.UiPauseClose);
        AudioManager.Instance?.SetMusicDucked(false);

        Hide(() =>
        {
            currentView = PauseView.Hidden;
            Time.timeScale = 1f;

            if (PlayerController.Instance != null && (PlayerHealth.Instance == null || !PlayerHealth.Instance.IsDead()))
            {
                PlayerController.Instance.SetInputActive(true);
            }

            RefreshHudVisibility();
        });
    }

    private void OpenSettings()
    {
        if (isBusy || currentView == PauseView.Settings)
        {
            return;
        }

        currentView = PauseView.Settings;
        AudioManager.Instance?.PlayUISfx(AudioCue.UiSubmenuOpen);

        SetPauseMenuOverlayState(true);
        SyncSlidersFromAudio();
        TransitionMenuCard(pauseCardRoot, settingsPanelRoot, musicSlider != null ? musicSlider.gameObject : null);
    }

    private void OpenLeaderboardFromPause()
    {
        LeaderboardPanel leaderboardPanel = GameUI.Instance?.LeaderboardPanel;
        if (leaderboardPanel == null)
        {
            return;
        }

        currentView = PauseView.Leaderboard;
        SetPauseMenuOverlayState(false);
        pauseCardRoot.SetActive(false);
        settingsPanelRoot.SetActive(false);
        leaderboardPanel.Show(takeInputOwnership: false);
    }

    private void CloseLeaderboardToMain()
    {
        if (GameUI.Instance?.LeaderboardPanel?.IsOpen == true)
        {
            GameUI.Instance.LeaderboardPanel.HideWithoutInputRestore();
        }
        else
        {
            ShowMainViewInternal(leaderboardButton);
        }
    }

    private void BackToMainFromSettings()
    {
        if (isBusy)
        {
            return;
        }

        currentView = PauseView.Main;
        AudioManager.Instance?.PlayUISfx(AudioCue.UiBack);
        TransitionMenuCard(settingsPanelRoot, pauseCardRoot, settingsButton != null ? settingsButton.gameObject : null);
    }

    private void HandleLeaderboardClosed()
    {
        if (currentView == PauseView.Leaderboard && IsOpen)
        {
            ShowMainViewInternal(leaderboardButton);
        }
    }

    private void HandleActionButton()
    {
        if (WaveSpawner.Instance != null && WaveSpawner.Instance.GetCurrentWave() > 0)
        {
            RestartRun();
            return;
        }

        QuitGame();
    }

    private void RestartRun()
    {
        if (isBusy)
        {
            return;
        }

        isBusy = true;
        AudioManager.Instance?.PlayUISfx(AudioCue.UiRestartConfirm);
        AudioManager.Instance?.SetMusicDucked(false);

        if (LoadingUIManager.Instance != null)
        {
            LoadingUIManager.Instance.ShowBlackFadeAndRestart();
            return;
        }

        Time.timeScale = 1f;
        GameUI.Instance?.PrepareForSceneReload();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ForceHideForSceneReload()
    {
        isBusy = false;
        currentView = PauseView.Hidden;
        SetHudVisible(false);
        AudioManager.Instance?.SetMusicDucked(false);

        if (GameUI.Instance?.LeaderboardPanel?.IsOpen == true)
        {
            GameUI.Instance.LeaderboardPanel.HideWithoutInputRestore();
        }

        if (menu != null)
        {
            CanvasGroup menuCanvasGroup = GetOrAddCG(menu);
            DOTween.Kill(menuCanvasGroup);
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.blocksRaycasts = false;
            menuCanvasGroup.interactable = false;
            menu.SetActive(false);
        }

        SetPauseMenuOverlayState(false);
        SetViewRootState(pauseCardRoot, false);
        SetViewRootState(settingsPanelRoot, false);
        if (mainView != null) mainView.SetActive(false);
        if (settingsView != null) settingsView.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
        RefreshHudVisibility();
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        System.Type editorApplicationType = System.Type.GetType("UnityEditor.EditorApplication, UnityEditor");
        editorApplicationType?.GetProperty("isPlaying")?.SetValue(null, false);
#else
        Application.Quit();
#endif
    }

    private void HandleMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
        UpdateSliderText(musicValueText, value);
    }

    private void HandleSfxVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);
        UpdateSliderText(sfxValueText, value);
    }

    private void ShowMainViewInternal(Button selectButton)
    {
        isBusy = false;
        currentView = PauseView.Main;
        UpdateActionButtonLabel();

        SetPauseMenuOverlayState(true);
        SetViewRootState(pauseCardRoot, true);
        mainView.SetActive(true);
        SetViewRootState(settingsPanelRoot, false);
        settingsView.SetActive(true);

        if (titleText != null)
        {
            titleText.text = "PAUSED";
        }

        if (footerText != null)
        {
            footerText.text = "ESC TO RESUME";
        }

        SelectObject(selectButton != null ? selectButton.gameObject : null);
    }

    private void TransitionMenuCard(GameObject fromRoot, GameObject toRoot, GameObject selectTarget)
    {
        if (fromRoot == null || toRoot == null)
        {
            SetViewRootState(fromRoot, false);
            SetViewRootState(toRoot, true);
            SelectObject(selectTarget);
            return;
        }

        isBusy = true;
        PrepareViewRoot(toRoot);

        CanvasGroup fromCanvasGroup = GetOrAddCG(fromRoot);
        CanvasGroup toCanvasGroup = GetOrAddCG(toRoot);
        float duration = Mathf.Max(0.01f, viewFadeDuration);

        DOTween.Kill(fromCanvasGroup);
        DOTween.Kill(toCanvasGroup);

        fromRoot.SetActive(true);
        fromCanvasGroup.alpha = 1f;
        fromCanvasGroup.blocksRaycasts = false;
        fromCanvasGroup.interactable = false;

        toRoot.SetActive(true);
        toCanvasGroup.alpha = 0f;
        toCanvasGroup.blocksRaycasts = false;
        toCanvasGroup.interactable = false;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(fromCanvasGroup.DOFade(0f, duration));
        sequence.AppendCallback(() => fromRoot.SetActive(false));
        sequence.Append(toCanvasGroup.DOFade(1f, duration));
        sequence.OnComplete(() =>
        {
            toCanvasGroup.blocksRaycasts = true;
            toCanvasGroup.interactable = true;
            isBusy = false;
            SelectObject(selectTarget);
        });
    }

    private void PrepareViewRoot(GameObject viewRoot)
    {
        if (viewRoot == pauseCardRoot)
        {
            if (mainView != null)
            {
                mainView.SetActive(true);
            }

            return;
        }

        if (viewRoot == settingsPanelRoot && settingsView != null)
        {
            settingsView.SetActive(true);
        }
    }

    private void SetViewRootState(GameObject viewRoot, bool isVisible)
    {
        if (viewRoot == null)
        {
            return;
        }

        CanvasGroup canvasGroup = GetOrAddCG(viewRoot);
        DOTween.Kill(canvasGroup);
        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.blocksRaycasts = isVisible;
        canvasGroup.interactable = isVisible;
        viewRoot.SetActive(isVisible);
    }

    private void RefreshHudVisibility()
    {
        bool shouldShow = gameplayHudEnabled && !IsOpen && !HasBlockingOverlay();
        SetHudVisible(shouldShow);
    }

    private void HandleGameStarted()
    {
        gameplayHudEnabled = true;
        RefreshHudVisibility();
    }

    private bool CanOpenPauseMenu()
    {
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead())
        {
            return false;
        }

        return !HasBlockingOverlay();
    }

    private bool HasBlockingOverlay()
    {
        GameUI ui = GameUI.Instance;
        if (ui != null)
        {
            if (ui.NameInputPanel != null && ui.NameInputPanel.IsOpen) return true;
            if (ui.ChallengePanel != null && ui.ChallengePanel.IsOpen) return true;
            if (ui.CardSelectionPanel != null && ui.CardSelectionPanel.IsOpen) return true;
            if (ui.LeaderboardPanel != null && ui.LeaderboardPanel.IsOpen && currentView != PauseView.Leaderboard) return true;
        }

        if (LoadingUIManager.Instance != null && LoadingUIManager.Instance.IsBlocking)
        {
            return true;
        }

        return false;
    }

    private void UpdateActionButtonLabel()
    {
        if (actionButtonLabel == null)
        {
            return;
        }

        bool inRun = WaveSpawner.Instance != null && WaveSpawner.Instance.GetCurrentWave() > 0;
        actionButtonLabel.text = inRun ? "RESTART" : "QUIT";
    }

    private void SyncSlidersFromAudio()
    {
        float music = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 1f;
        float sfx = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;

        musicSlider.SetValueWithoutNotify(music);
        sfxSlider.SetValueWithoutNotify(sfx);
        UpdateSliderText(musicValueText, music);
        UpdateSliderText(sfxValueText, sfx);
    }

    private void UpdateSliderText(TextMeshProUGUI label, float value)
    {
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    private void SetHudVisible(bool visible)
    {
        if (hudPromptRoot != null)
        {
            hudPromptRoot.SetActive(visible);
        }
    }

    private void SetPauseMenuOverlayState(bool isVisible)
    {
        if (scrimImage != null)
        {
            scrimImage.enabled = isVisible;
            scrimImage.raycastTarget = isVisible;
        }

        if (menu == null)
        {
            return;
        }

        CanvasGroup canvasGroup = GetOrAddCG(menu);
        canvasGroup.blocksRaycasts = isVisible;
        canvasGroup.interactable = isVisible;
    }

    private void SelectObject(GameObject target)
    {
        if (EventSystem.current == null || target == null || !target.activeInHierarchy)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }

    private void ResolveSceneReferences()
    {
        if (settingsBackButton == null)
        {
            settingsBackButton = FindChildComponentByName<Button>(settingsPanelRoot != null ? settingsPanelRoot.transform : null, "BackButton");
            if (settingsBackButton != null)
            {
                Debug.LogWarning("[PauseMenuPanel] Auto-resolved missing settingsBackButton reference. Re-save the scene to persist this fix.", this);
            }
        }
    }

    private static T FindChildComponentByName<T>(Transform root, string childName) where T : Component
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && string.Equals(components[i].name, childName, StringComparison.Ordinal))
            {
                return components[i];
            }
        }

        return null;
    }

    private bool ValidateReferences()
    {
        List<string> missing = new List<string>();

        if (menu == null) missing.Add(nameof(menu));
        if (hudPromptRoot == null) missing.Add(nameof(hudPromptRoot));
        if (hudPromptText == null) missing.Add(nameof(hudPromptText));
        if (scrimImage == null) missing.Add(nameof(scrimImage));
        if (pauseCardRoot == null) missing.Add(nameof(pauseCardRoot));
        if (mainView == null) missing.Add(nameof(mainView));
        if (settingsPanelRoot == null) missing.Add(nameof(settingsPanelRoot));
        if (settingsView == null) missing.Add(nameof(settingsView));
        if (leaderboardButton == null) missing.Add(nameof(leaderboardButton));
        if (settingsButton == null) missing.Add(nameof(settingsButton));
        if (actionButton == null) missing.Add(nameof(actionButton));
        if (actionButtonLabel == null) missing.Add(nameof(actionButtonLabel));
        if (musicSlider == null) missing.Add(nameof(musicSlider));
        if (sfxSlider == null) missing.Add(nameof(sfxSlider));

        if (missing.Count == 0)
        {
            return true;
        }

        Debug.LogError($"[PauseMenuPanel] Missing required scene references: {string.Join(", ", missing)}", this);
        return false;
    }

    private void ApplyStaticVisuals()
    {
        if (scrimImage != null)
        {
            scrimImage.color = scrimColor;
        }

        if (hudPromptText != null)
        {
            hudPromptText.text = "ESC TO MENU";
            hudPromptText.alignment = TextAlignmentOptions.Left;
        }

        if (settingsTitleText != null)
        {
            settingsTitleText.text = "SETTINGS";
        }

        if (settingsFooterText != null)
        {
            settingsFooterText.text = "ESC TO BACK";
            settingsFooterText.color = accentColor;
        }

        ConfigureButtonColors(leaderboardButton);
        ConfigureButtonColors(settingsButton);
        ConfigureButtonColors(actionButton);
        ConfigureButtonColors(settingsBackButton);
    }

    private void ConfigureButtonColors(Button button)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHighlightColor;
        colors.selectedColor = accentColor;
        colors.pressedColor = accentColor * 0.8f;
        colors.disabledColor = new Color(0.18f, 0.18f, 0.18f, 0.6f);
        button.colors = colors;
    }

    private void OnDestroy()
    {
        ChallengePanel.onGameStart -= HandleGameStarted;
        if (leaderboardButton != null) leaderboardButton.onClick.RemoveListener(OpenLeaderboardFromPause);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        if (actionButton != null) actionButton.onClick.RemoveListener(HandleActionButton);
        if (settingsBackButton != null) settingsBackButton.onClick.RemoveListener(BackToMainFromSettings);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
    }
}
