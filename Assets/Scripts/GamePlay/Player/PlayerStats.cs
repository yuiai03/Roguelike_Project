using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerData playerData;

    [Header("Events")]
    public UnityEvent<string, float> OnStatUpgraded;

    private PlayerHealth playerHealth;
    private PlayerController playerController;
    private PlayerAttack playerAttack;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerController = GetComponent<PlayerController>();
        playerAttack = GetComponent<PlayerAttack>();

        if (playerData == null)
        {
            if (playerController != null)
            {
                playerData = playerController.GetPlayerData();
            }
        }
    }

    void Start()
    {
        ApplyAllStats();
    }

    private void ApplyAllStats()
    {
        if (playerData == null) return;

        if (playerHealth != null)
        {
            playerHealth.SetPlayerData(playerData);
        }

        if (playerAttack != null)
        {
            playerAttack.SetPlayerData(playerData);
        }

        if (playerController != null)
        {
            playerController.SetPlayerData(playerData);
        }
    }

    public void UpgradeHealth(float percentage)
    {
        if (playerData == null) return;

        float oldMaxHealth = playerData.GetMaxHealth();
        playerData.healthMultiplier += percentage;
        float newMaxHealth = playerData.GetMaxHealth();
        
        if (playerHealth != null)
        {
            float healthIncrease = newMaxHealth - oldMaxHealth;
            playerHealth.IncreaseMaxHealth(healthIncrease);
        }
        
        OnStatUpgraded?.Invoke("Health", percentage);
        Debug.Log($"Health upgraded by {percentage * 100}%! New Max Health: {newMaxHealth}");
    }

    public void UpgradeMoveSpeed(float percentage)
    {
        if (playerData == null) return;

        playerData.moveSpeedMultiplier += percentage;
        OnStatUpgraded?.Invoke("Move Speed", percentage);
        Debug.Log($"Move Speed upgraded by {percentage * 100}%! New Speed: {playerData.GetEffectiveMoveSpeed()}");
    }

    public void UpgradeDamage(float percentage)
    {
        if (playerData == null) return;

        playerData.damageMultiplier += percentage;
        OnStatUpgraded?.Invoke("Damage", percentage);
        Debug.Log($"Damage upgraded by {percentage * 100}%! New Damage: {playerData.GetTotalDamage()}");
    }

    public void UpgradeAttackSpeed(float percentage)
    {
        if (playerData == null) return;

        playerData.attackSpeedMultiplier += percentage;
        OnStatUpgraded?.Invoke("Attack Speed", percentage);
        Debug.Log($"Attack Speed upgraded by {percentage * 100}%! New Cooldown: {playerData.GetAttackCooldown()}");
    }

    public void UpgradeAttackRange(float amount)
    {
        if (playerData == null) return;

        playerData.attackRange += amount;
        OnStatUpgraded?.Invoke("Attack Range", amount);
        Debug.Log($"Attack Range upgraded by {amount}! Current: {playerData.attackRange}");
    }

    public PlayerData GetPlayerData()
    {
        return playerData;
    }

    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
        ApplyAllStats();
    }

    public void ResetStats()
    {
        if (playerData == null) return;
        
        playerData.ResetData();
        ApplyAllStats();
        Debug.Log("Player stats reset to default!");
    }
}
