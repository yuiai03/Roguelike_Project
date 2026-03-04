using UnityEngine;
using UnityEngine.Events;

public class PlayerLevelSystem : Singleton<PlayerLevelSystem>
{
    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float expToNextLevel = 100f;
    [SerializeField] private float expScalingFactor = 1.1f; 

    [Header("Leaderboard Data")]
    public float totalExpGained = 0f; 

    [Header("Events")]
    public UnityEvent<int> OnLevelUp; 
    public UnityEvent<float, float> OnExpChanged; 
    public UnityEvent<int, int> OnLevelChanged; 

    private void Start()
    {

        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        OnLevelChanged?.Invoke(currentLevel, 999); 
    }

    public void AddExp(float amount)
    {
        currentExp += amount;
        totalExpGained += amount; 
        OnExpChanged?.Invoke(currentExp, expToNextLevel);

        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevel++;

        expToNextLevel = Mathf.Floor(expToNextLevel * expScalingFactor);

        Debug.Log($"=== LEVEL UP! Now Level {currentLevel} ===");
        Debug.Log($"Next level requires: {expToNextLevel} EXP");

        OnLevelChanged?.Invoke(currentLevel, 999);
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        OnLevelUp?.Invoke(currentLevel);

        Time.timeScale = 0f;
    }

    public int GetCurrentLevel() => currentLevel;
    public float GetCurrentExp() => currentExp;
    public float GetExpToNextLevel() => expToNextLevel;
    public float GetExpProgress() => currentExp / expToNextLevel;
    public float GetTotalExpGained() => totalExpGained;

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
