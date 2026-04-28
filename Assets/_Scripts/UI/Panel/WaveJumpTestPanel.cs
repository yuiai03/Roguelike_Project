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
    [SerializeField] private TextMeshProUGUI currentWaveText;
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

        RefreshWaveLabel();
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
        if (!IsOpen)
        {
            return;
        }

        Hide(() =>
        {
            if (background != null)
            {
                background.SetActive(false);
            }

            EventSystem.current?.SetSelectedGameObject(null);
            RestoreGameplay();
        });
    }

    private void HandleJumpButtonClicked()
    {
        if (waveInputField == null)
        {
            SetStatus("Thiếu input field.");
            return;
        }

        if (!int.TryParse(waveInputField.text, out int targetWave) || targetWave < 1)
        {
            SetStatus("Nhập wave >= 1.");
            waveInputField.Select();
            waveInputField.ActivateInputField();
            return;
        }

        WaveSpawner waveSpawner = WaveSpawner.Instance;
        if (waveSpawner == null)
        {
            SetStatus("Không tìm thấy WaveSpawner.");
            return;
        }

        if (!waveSpawner.JumpToWave(targetWave))
        {
            SetStatus($"Không thể chuyển tới wave {targetWave}.");
            return;
        }

        ClosePanel();
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

        return LoadingUIManager.Instance == null || !LoadingUIManager.Instance.IsBlocking;
    }

    private void RefreshWaveLabel()
    {
        if (currentWaveText == null)
        {
            return;
        }

        WaveSpawner waveSpawner = WaveSpawner.Instance;
        currentWaveText.text = waveSpawner == null
            ? "Wave hien tai: --"
            : WaveSpawner.FormatWaveLabel(waveSpawner.GetCurrentWave(), waveSpawner.GetTotalWaves());
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

        if (currentWaveText == null)
        {
            Transform waveLabelTransform = transform.Find("Menu/CurrentWaveText");
            if (waveLabelTransform != null)
            {
                currentWaveText = waveLabelTransform.GetComponent<TextMeshProUGUI>();
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
    }
}
