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

    void Awake()
    {
        playerData = GetComponent<PlayerData>();
    }

    private void Start()
    {
        if (playerData == null)
        {
            playerData.currentHealth = playerData.GetMaxHealth();
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
