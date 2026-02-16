using UnityEngine;
using UnityEngine.Events;

public class PlayerLevelSystem : Singleton<PlayerLevelSystem>
{
    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float expToNextLevel = 100f;
    [SerializeField] private float expScalingFactor = 1.1f; // Exp cần tăng mỗi level

    [Header("Events")]
    public UnityEvent<int> OnLevelUp; // Gửi current level
    public UnityEvent<float, float> OnExpChanged; // current exp, max exp
    public UnityEvent<int, int> OnLevelChanged; // current level, max level (có thể để unlimited)

    private void Start()
    {
        // Notify initial values
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        OnLevelChanged?.Invoke(currentLevel, 999); // Unlimited levels
    }

    /// <summary>
    /// Thêm exp cho player
    /// </summary>
    public void AddExp(float amount)
    {
        currentExp += amount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel);

        // Check level up
        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
    }

    /// <summary>
    /// Level up và trigger event
    /// </summary>
    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevel++;

        // Tính exp cần cho level tiếp theo
        expToNextLevel = Mathf.Floor(expToNextLevel * expScalingFactor);

        Debug.Log($"=== LEVEL UP! Now Level {currentLevel} ===");
        Debug.Log($"Next level requires: {expToNextLevel} EXP");

        // Trigger events
        OnLevelChanged?.Invoke(currentLevel, 999);
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        OnLevelUp?.Invoke(currentLevel);

        // Pause game and show card selection
        Time.timeScale = 0f;
    }

    // Getters
    public int GetCurrentLevel() => currentLevel;
    public float GetCurrentExp() => currentExp;
    public float GetExpToNextLevel() => expToNextLevel;
    public float GetExpProgress() => currentExp / expToNextLevel;

    // Cheat for testing
    [ContextMenu("Add 50 Exp")]
    public void AddExpCheat()
    {
        AddExp(50f);
    }

    [ContextMenu("Level Up")]
    public void LevelUpCheat()
    {
        AddExp(expToNextLevel - currentExp);
    }
}
