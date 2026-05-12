using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WaveJumpTestPanel : PanelBase
{
    [Header("References")]
    [SerializeField] private GameObject background;
    [SerializeField] private TMP_InputField waveInputField;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button addExpButton;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button levelUpFiveButton;
    [SerializeField] private TextMeshProUGUI currentWaveText;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI statusText;

    protected override void Awake()
    {
        ResolveReferences();
        base.Awake();

        CanvasGroup canvasGroup = GetOrAddCG(gameObject);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (background != null)
        {
            background.SetActive(false);
        }

        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(HandleJumpButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (addExpButton != null)
        {
            addExpButton.onClick.AddListener(HandleAddExpButtonClicked);
        }

        if (levelUpButton != null)
        {
            levelUpButton.onClick.AddListener(HandleLevelUpButtonClicked);
        }

        if (levelUpFiveButton != null)
        {
            levelUpFiveButton.onClick.AddListener(HandleLevelUpFiveButtonClicked);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (!IsOpen)
        {
            if ((keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame) && CanOpenPanel())
            {
                OpenPanel();
            }

            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
            return;
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            HandleJumpButtonClicked();
        }
    }

    public void OpenPanel()
    {
        if (IsOpen || !CanOpenPanel())
        {
            return;
        }

        Time.timeScale = 0f;
        PlayerController.Instance?.SetInputActive(false);
        GameUI.Instance?.InteractPanel?.Hide();

        if (background != null)
        {
            background.SetActive(true);
        }

        RefreshPanelState();
        SetStatus(string.Empty);

        Show(() =>
        {
            if (waveInputField == null)
            {
                return;
            }

            waveInputField.text = string.Empty;
            waveInputField.Select();
            waveInputField.ActivateInputField();
            EventSystem.current?.SetSelectedGameObject(waveInputField.gameObject);
        });
    }

    public void ClosePanel()
    {
        ClosePanelInternal(true, null);
    }

    private void HandleJumpButtonClicked()
    {
        if (waveInputField == null)
        {
            SetStatus("Missing wave input field.");
            return;
        }

        if (!int.TryParse(waveInputField.text, out int targetWave) || targetWave < 1)
        {
            SetStatus("Enter a wave >= 1.");
            waveInputField.Select();
            waveInputField.ActivateInputField();
            return;
        }

        WaveSpawner waveSpawner = WaveSpawner.Instance;
        if (waveSpawner == null)
        {
            SetStatus("WaveSpawner not found.");
            return;
        }

        if (!waveSpawner.JumpToWave(targetWave))
        {
            SetStatus($"Could not jump to wave {targetWave}.");
            return;
        }

        ClosePanel();
    }

    private void HandleAddExpButtonClicked()
    {
        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem == null)
        {
            SetStatus("PlayerLevelSystem not found.");
            return;
        }

        bool willLevelUp = levelSystem.GetCurrentExp() + 50f >= levelSystem.GetExpToNextLevel();
        if (willLevelUp)
        {
            ClosePanelInternal(false, () => levelSystem.AddExp(50f));
            return;
        }

        levelSystem.AddExp(50f);
        RefreshPanelState();
        SetStatus("+50 EXP granted.");
    }

    private void HandleGrantLevelsButtonClicked(int levelCount)
    {
        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem == null)
        {
            SetStatus("PlayerLevelSystem not found.");
            return;
        }

        ClosePanelInternal(false, () => levelSystem.GrantLevels(levelCount));
    }

    private void HandleLevelUpButtonClicked()
    {
        HandleGrantLevelsButtonClicked(1);
    }

    private void HandleLevelUpFiveButtonClicked()
    {
        HandleGrantLevelsButtonClicked(5);
    }

    private void ClosePanelInternal(bool restoreGameplay, Action onClosed)
    {
        if (!IsOpen)
        {
            onClosed?.Invoke();
            return;
        }

        Hide(() =>
        {
            if (background != null)
            {
                background.SetActive(false);
            }

            EventSystem.current?.SetSelectedGameObject(null);

            if (restoreGameplay)
            {
                RestoreGameplay();
            }

            onClosed?.Invoke();
        });
    }

    private bool CanOpenPanel()
    {
        if (PlayerHealth.Instance != null && PlayerHealth.Instance.IsDead())
        {
            return false;
        }

        GameUI ui = GameUI.Instance;
        if (ui != null)
        {
            if (ui.NameInputPanel != null && ui.NameInputPanel.IsOpen) return false;
            if (ui.ChallengePanel != null && ui.ChallengePanel.IsOpen) return false;
            if (ui.CardSelectionPanel != null && ui.CardSelectionPanel.IsOpen) return false;
            if (ui.LeaderboardPanel != null && ui.LeaderboardPanel.IsOpen) return false;
            if (ui.PauseMenuPanel != null && ui.PauseMenuPanel.IsOpen) return false;
        }

        if (LoadingUIManager.Instance != null && LoadingUIManager.Instance.IsBlocking)
        {
            return false;
        }

        WaveSpawner waveSpawner = WaveSpawner.Instance;
        return waveSpawner != null && waveSpawner.IsWaveActive();
    }

    private void RefreshPanelState()
    {
        RefreshWaveLabel();
        RefreshLevelLabel();
    }

    private void RefreshWaveLabel()
    {
        if (currentWaveText == null)
        {
            return;
        }

        WaveSpawner waveSpawner = WaveSpawner.Instance;
        currentWaveText.text = waveSpawner == null
            ? "Current Wave: --"
            : WaveSpawner.FormatWaveLabel(waveSpawner.GetCurrentWave(), waveSpawner.GetTotalWaves());
    }

    private void RefreshLevelLabel()
    {
        if (currentLevelText == null)
        {
            return;
        }

        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        currentLevelText.text = levelSystem == null
            ? "Current Level: --"
            : $"Current Level: {levelSystem.GetCurrentLevel()}";
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void RestoreGameplay()
    {
        Time.timeScale = 1f;

        if (PlayerController.Instance != null && (PlayerHealth.Instance == null || !PlayerHealth.Instance.IsDead()))
        {
            PlayerController.Instance.SetInputActive(true);
        }
    }

    private void ResolveReferences()
    {
        if (menu == null)
        {
            Transform menuTransform = transform.Find("Menu");
            if (menuTransform != null)
            {
                menu = menuTransform.gameObject;
            }
        }

        if (background == null)
        {
            Transform backgroundTransform = transform.Find("Bg");
            if (backgroundTransform != null)
            {
                background = backgroundTransform.gameObject;
            }
        }

        if (waveInputField == null)
        {
            waveInputField = GetComponentInChildren<TMP_InputField>(true);
        }

        if (jumpButton == null)
        {
            Transform jumpTransform = transform.Find("Menu/JumpButton");
            if (jumpTransform != null)
            {
                jumpButton = jumpTransform.GetComponent<Button>();
            }
        }

        if (closeButton == null)
        {
            Transform closeTransform = transform.Find("Menu/CloseButton");
            if (closeTransform != null)
            {
                closeButton = closeTransform.GetComponent<Button>();
            }
        }

        if (addExpButton == null)
        {
            Transform addExpTransform = transform.Find("Menu/AddExpButton");
            if (addExpTransform != null)
            {
                addExpButton = addExpTransform.GetComponent<Button>();
            }
        }

        if (levelUpButton == null)
        {
            Transform levelUpTransform = transform.Find("Menu/LevelUpButton");
            if (levelUpTransform != null)
            {
                levelUpButton = levelUpTransform.GetComponent<Button>();
            }
        }

        if (levelUpFiveButton == null)
        {
            Transform levelUpFiveTransform = transform.Find("Menu/LevelUpFiveButton");
            if (levelUpFiveTransform != null)
            {
                levelUpFiveButton = levelUpFiveTransform.GetComponent<Button>();
            }
        }

        if (currentWaveText == null)
        {
            Transform waveLabelTransform = transform.Find("Menu/CurrentWaveText");
            if (waveLabelTransform != null)
            {
                currentWaveText = waveLabelTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (currentLevelText == null)
        {
            Transform levelLabelTransform = transform.Find("Menu/CurrentLevelText");
            if (levelLabelTransform != null)
            {
                currentLevelText = levelLabelTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (statusText == null)
        {
            Transform statusLabelTransform = transform.Find("Menu/StatusText");
            if (statusLabelTransform != null)
            {
                statusText = statusLabelTransform.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void OnDestroy()
    {
        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveListener(HandleJumpButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        if (addExpButton != null)
        {
            addExpButton.onClick.RemoveListener(HandleAddExpButtonClicked);
        }

        if (levelUpButton != null)
        {
            levelUpButton.onClick.RemoveListener(HandleLevelUpButtonClicked);
        }

        if (levelUpFiveButton != null)
        {
            levelUpFiveButton.onClick.RemoveListener(HandleLevelUpFiveButtonClicked);
        }
    }
}
