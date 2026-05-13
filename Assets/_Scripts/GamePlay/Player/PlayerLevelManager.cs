using UnityEngine;
using UnityEngine.Events;

public class PlayerLevelSystem : Singleton<PlayerLevelSystem>
{
    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 0;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float expToNextLevel = 100f;
    [SerializeField] private float expScalingFactor = 1.1f;
    [SerializeField] private float totalExpRequiredForNextLevel = 100f;

    [Header("Leaderboard Data")]
    public float totalExpGained = 0f;

    [Header("Events")]
    public UnityEvent<int> OnLevelUp;
    public UnityEvent<float, float> OnExpChanged;
    public UnityEvent<int, int> OnLevelChanged;

    private System.Action startGameHandler;

    private void Start()
    {
        Time.timeScale = 1f;
        EnsureTotalExpRequiredInitialized();

        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        OnLevelChanged?.Invoke(currentLevel, 999);

        startGameHandler = LevelUp;
        ChallengePanel.onGameStart += startGameHandler;
    }

    public void AddExp(float amount)
    {
        EnsureTotalExpRequiredInitialized();

        currentExp += amount;
        totalExpGained += amount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel);

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            LevelUp();
        }
    }

    public void GrantLevels(int count)
    {
        if (count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            AddExp(Mathf.Max(0f, expToNextLevel - currentExp));
        }
    }

    private void LevelUp()
    {
        EnsureTotalExpRequiredInitialized();

        currentLevel++;
        AudioManager.Instance?.PlayWorldSfx(AudioCue.LevelUp);

        expToNextLevel = Mathf.Floor(expToNextLevel * expScalingFactor);
        totalExpRequiredForNextLevel += expToNextLevel;

        Debug.Log($"=== LEVEL UP! Now Level {currentLevel} ===");
        Debug.Log($"Next level requires: {expToNextLevel} EXP");

        OnLevelChanged?.Invoke(currentLevel, 999);
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        OnLevelUp?.Invoke(currentLevel);
    }

    public int GetCurrentLevel() => currentLevel;
    public float GetCurrentExp() => currentExp;
    public float GetExpToNextLevel() => expToNextLevel;
    public float GetExpProgress() => currentExp / expToNextLevel;
    public float GetTotalExpGained() => totalExpGained;
    public float GetTotalExpRequiredForNextLevel()
    {
        EnsureTotalExpRequiredInitialized();
        return totalExpRequiredForNextLevel;
    }

    private void EnsureTotalExpRequiredInitialized()
    {
        if (totalExpRequiredForNextLevel <= 0f || totalExpRequiredForNextLevel < expToNextLevel)
        {
            totalExpRequiredForNextLevel = expToNextLevel;
        }
    }

    [ContextMenu("Add 50 Exp")]
    public void AddExpCheat()
    {
        AddExp(50f);
    }

    [ContextMenu("Level Up")]
    public void LevelUpCheat()
    {
        GrantLevels(1);
    }

    private void OnDestroy()
    {
        if (startGameHandler != null)
        {
            ChallengePanel.onGameStart -= startGameHandler;
        }
    }
}
