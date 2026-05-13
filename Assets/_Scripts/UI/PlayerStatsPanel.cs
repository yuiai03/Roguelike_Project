using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsPanel : PanelBase
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Health Bar")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Level Display")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Exp Bar")]
    [SerializeField] private Image expBarFill;
    [SerializeField] private TextMeshProUGUI expText;

    [Header("Wave Display")]
    [SerializeField] private TextMeshProUGUI waveText;

    private PlayerLevelSystem boundLevelSystem;
    private WaveSpawner boundWaveSpawner;
    private PlayerHealth boundPlayerHealth;
    private System.Action startGameHandler;

    private void Start()
    {
        startGameHandler = HandleGameStart;
        ChallengePanel.onGameStart += startGameHandler;

        ResetForReplay();
    }

    private void Update()
    {
        if (!IsOpen || boundWaveSpawner == null || waveText == null)
        {
            return;
        }

        waveText.text = WaveSpawner.FormatWaveLabel(boundWaveSpawner.GetCurrentWave(), boundWaveSpawner.GetTotalWaves());
    }

    public void ResetForReplay()
    {
        UnbindRuntimeReferences();
        HideImmediate();
    }

    private void HandleGameStart()
    {
        BindRuntimeReferences();
        Show();
    }

    private void BindRuntimeReferences()
    {
        UnbindRuntimeReferences();

        boundPlayerHealth = PlayerHealth.Instance != null ? PlayerHealth.Instance : playerHealth;
        boundLevelSystem = PlayerLevelSystem.Instance;
        boundWaveSpawner = WaveSpawner.Instance;
        playerHealth = boundPlayerHealth;

        if (boundPlayerHealth != null)
        {
            UpdateHealthBar(boundPlayerHealth.GetCurrentHealth(), boundPlayerHealth.GetMaxHealth());
            boundPlayerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
        }

        if (boundLevelSystem != null)
        {
            UpdateLevel(boundLevelSystem.GetCurrentLevel(), 999);
            UpdateExp(boundLevelSystem.GetCurrentExp(), boundLevelSystem.GetExpToNextLevel());
            boundLevelSystem.OnLevelChanged.AddListener(UpdateLevel);
            boundLevelSystem.OnExpChanged.AddListener(UpdateExp);
        }

        if (boundWaveSpawner != null)
        {
            UpdateWave(boundWaveSpawner.GetCurrentWave());
            boundWaveSpawner.OnWaveStart.AddListener(UpdateWave);
        }
    }

    private void UnbindRuntimeReferences()
    {
        if (boundPlayerHealth != null)
        {
            boundPlayerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }

        if (boundLevelSystem != null)
        {
            boundLevelSystem.OnLevelChanged.RemoveListener(UpdateLevel);
            boundLevelSystem.OnExpChanged.RemoveListener(UpdateExp);
        }

        if (boundWaveSpawner != null)
        {
            boundWaveSpawner.OnWaveStart.RemoveListener(UpdateWave);
        }

        boundPlayerHealth = null;
        boundLevelSystem = null;
        boundWaveSpawner = null;
    }

    private void UpdateLevel(int currentLevel, int maxLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv.{currentLevel}";
        }
    }

    private void UpdateExp(float currentExp, float maxExp)
    {
        if (expBarFill != null)
        {
            expBarFill.fillAmount = maxExp > 0f ? Mathf.Clamp01(currentExp / maxExp) : 0f;
        }

        if (expText != null)
        {
            float totalExp = boundLevelSystem != null ? boundLevelSystem.GetTotalExpGained() : currentExp;
            float totalExpRequired = boundLevelSystem != null ? boundLevelSystem.GetTotalExpRequiredForNextLevel() : maxExp;
            expText.text = $"{Utils.FormatWholeNumber(totalExp)}/{Utils.FormatWholeNumber(totalExpRequired)}";
        }
    }

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }

        if (healthText != null)
        {
            healthText.text = $"{Utils.FormatWholeNumber(currentHealth)}/{Utils.FormatWholeNumber(maxHealth)}";
        }
    }

    private void UpdateWave(int waveNumber)
    {
        if (waveText != null && boundWaveSpawner != null)
        {
            waveText.text = WaveSpawner.FormatWaveLabel(waveNumber, boundWaveSpawner.GetTotalWaves());
        }
    }

    private void OnDestroy()
    {
        if (startGameHandler != null)
        {
            ChallengePanel.onGameStart -= startGameHandler;
        }

        UnbindRuntimeReferences();
    }
}
