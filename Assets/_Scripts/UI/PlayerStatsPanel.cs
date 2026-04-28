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

    private PlayerLevelSystem levelSystem => PlayerLevelSystem.Instance;
    private WaveSpawner waveSpawner => WaveSpawner.Instance;
    private System.Action startGameHandler;

    private void Start()
    {
        menu.SetActive(false);

        if (playerHealth != null)
        {
            UpdateHealthBar(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
            playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
        }

        if (levelSystem != null)
        {
            UpdateLevel(levelSystem.GetCurrentLevel(), 999);
            UpdateExp(levelSystem.GetCurrentExp(), levelSystem.GetExpToNextLevel());
            levelSystem.OnLevelChanged.AddListener(UpdateLevel);
            levelSystem.OnExpChanged.AddListener(UpdateExp);
        }

        if (waveSpawner != null)
        {
            UpdateWave(waveSpawner.GetCurrentWave());
            waveSpawner.OnWaveStart.AddListener(UpdateWave);
        }

        startGameHandler = () => Show();
        ChallengePanel.onGameStart += startGameHandler;

        menu.SetActive(false);
    }

    private void Update()
    {
        if (waveSpawner == null || waveText == null)
        {
            return;
        }

        waveText.text = WaveSpawner.FormatWaveLabel(waveSpawner.GetCurrentWave(), waveSpawner.GetTotalWaves());
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
            expBarFill.fillAmount = currentExp / maxExp;
        }

        if (expText != null)
        {
            expText.text = $"{Utils.FormatWholeNumber(currentExp)}/{Utils.FormatWholeNumber(maxExp)}";
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
        if (waveText != null && waveSpawner != null)
        {
            waveText.text = WaveSpawner.FormatWaveLabel(waveNumber, waveSpawner.GetTotalWaves());
        }
    }

    private void OnDestroy()
    {
        if (startGameHandler != null)
        {
            ChallengePanel.onGameStart -= startGameHandler;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }

        if (levelSystem != null)
        {
            levelSystem.OnLevelChanged.RemoveListener(UpdateLevel);
            levelSystem.OnExpChanged.RemoveListener(UpdateExp);
        }

        if (waveSpawner != null)
        {
            waveSpawner.OnWaveStart.RemoveListener(UpdateWave);
        }
    }
}
