using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hiển thị Level, Exp Bar và Wave hiện tại
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    [Header("Level Display")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Exp Bar")]
    [SerializeField] private Image expBarFill;
    [SerializeField] private TextMeshProUGUI expText;

    [Header("Wave Display")]
    [SerializeField] private TextMeshProUGUI waveText;

    private PlayerLevelSystem levelSystem => PlayerLevelSystem.Instance;
    private WaveSpawner waveSpawner => WaveSpawner.Instance;

    private void Start()
    {
        // Subscribe to events
        if (levelSystem != null)
        {
            levelSystem.OnLevelChanged.AddListener(UpdateLevel);
            levelSystem.OnExpChanged.AddListener(UpdateExp);

            // Initial update
            UpdateLevel(levelSystem.GetCurrentLevel(), 999);
            UpdateExp(levelSystem.GetCurrentExp(), levelSystem.GetExpToNextLevel());
        }

        if (waveSpawner != null)
        {
            waveSpawner.OnWaveStart.AddListener(UpdateWave);

            // Initial update
            UpdateWave(waveSpawner.GetCurrentWave());
        }
    }

    private void Update()
    {
        // Update wave in real-time (optional, nếu wave có thể thay đổi mà không trigger event)
        if (waveSpawner != null)
        {
            int currentWave = waveSpawner.GetCurrentWave();
            int totalWaves = waveSpawner.GetTotalWaves();

            if (waveText != null)
            {
                waveText.text = $"Wave: {currentWave}/{totalWaves}";
            }
        }
    }

    private void UpdateLevel(int currentLevel, int maxLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"Level {currentLevel}";
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
            expText.text = $"{Mathf.Floor(currentExp)}/{Mathf.Floor(maxExp)}";
        }
    }

    private void UpdateWave(int waveNumber)
    {
        if (waveText != null && waveSpawner != null)
        {
            int totalWaves = waveSpawner.GetTotalWaves();
            waveText.text = $"Wave: {waveNumber}/{totalWaves}";
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
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
