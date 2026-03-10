using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged;
    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;
    public UnityEvent<float> OnHeal;

    private bool isDead;
    private PlayerData playerData;
    private HealthBarUIBase healthBarUI;

    public static PlayerHealth Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        playerData = GetComponent<PlayerData>();
        healthBarUI = GetComponentInChildren<HealthBarUIBase>();
    }

    private void Start()
    {
        if (playerData != null)
        {
            playerData.currentHealth = playerData.GetMaxHealth();

            OnHealthChanged?.Invoke(playerData.currentHealth, playerData.GetMaxHealth());
        }
        isDead = false;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead || playerData == null) return;

        playerData.currentHealth -= damage;
        playerData.currentHealth = Mathf.Max(0f, playerData.currentHealth);

        OnHealthChanged?.Invoke(playerData.currentHealth, playerData.GetMaxHealth());
        OnTakeDamage?.Invoke();

        if (DamageTextSpawner.Instance != null && damage > 0)
        {
            Vector3 spawnPos = healthBarUI != null ? healthBarUI.transform.position : transform.position + Vector3.up * 1f;
            DamageTextSpawner.Instance.Spawn(damage, spawnPos, isHeal: false, isPlayer: true);
        }

        if (playerData.currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead || playerData == null) return;

        playerData.currentHealth += amount;
        playerData.currentHealth = Mathf.Min(playerData.currentHealth, playerData.GetMaxHealth());

        OnHealthChanged?.Invoke(playerData.currentHealth, playerData.GetMaxHealth());
        OnHeal?.Invoke(amount);

        if (DamageTextSpawner.Instance != null && amount > 0)
        {
            Vector3 spawnPos = healthBarUI != null ? healthBarUI.transform.position : transform.position + Vector3.up * 1f;
            DamageTextSpawner.Instance.Spawn(amount, spawnPos, isHeal: true, isPlayer: false);
        }

        Debug.Log($"Player healed {amount}. Current Health: {playerData.currentHealth}/{playerData.GetMaxHealth()}");
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (playerData == null) return;

        playerData.maxHealth += amount;
        playerData.currentHealth += amount;
        OnHealthChanged?.Invoke(playerData.currentHealth, playerData.GetMaxHealth());
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        OnDeath?.Invoke();
        Debug.Log("Player died!");

        if (PlayerLevelSystem.Instance != null && Roguelike.Systems.Leaderboard.PlayFabLeaderboardManager.Instance != null)
        {
            int finalScore = Mathf.FloorToInt(PlayerLevelSystem.Instance.GetTotalExpGained());
            Roguelike.Systems.Leaderboard.PlayFabLeaderboardManager.Instance.SubmitScore(finalScore);
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public float GetCurrentHealth()
    {
        return playerData != null ? playerData.currentHealth : 0f;
    }

    public float GetMaxHealth()
    {
        return playerData != null ? playerData.GetMaxHealth() : 0f;
    }

    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
    }
}
