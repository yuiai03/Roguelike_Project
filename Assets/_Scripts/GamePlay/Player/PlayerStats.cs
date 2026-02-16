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

    public void UpgradeHealth(float amount)
    {
        if (playerData == null) return;

        float oldMaxHealth = playerData.GetMaxHealth();
        playerData.healthBonus += amount;
        float newMaxHealth = playerData.GetMaxHealth();

        if (playerHealth != null)
        {
            float healthIncrease = newMaxHealth - oldMaxHealth;
            playerHealth.IncreaseMaxHealth(healthIncrease);
        }

        OnStatUpgraded?.Invoke("Health", amount);
        Debug.Log($"Health upgraded by {amount}! New Max Health: {newMaxHealth}");
    }

    public void UpgradeMoveSpeed(float amount)
    {
        if (playerData == null) return;

        playerData.moveSpeedBonus += amount;
        OnStatUpgraded?.Invoke("Move Speed", amount);
        Debug.Log($"Move Speed upgraded by {amount}! New Speed: {playerData.GetEffectiveMoveSpeed()}");
    }

    public void UpgradeDamage(float amount)
    {
        if (playerData == null) return;

        playerData.damageBonus += amount;
        OnStatUpgraded?.Invoke("Damage", amount);
        Debug.Log($"Damage upgraded by {amount}! New Damage: {playerData.GetTotalDamage()}");
    }

    public void UpgradeAttackSpeed(float amount)
    {
        if (playerData == null) return;

        playerData.attackSpeedBonus += amount;
        OnStatUpgraded?.Invoke("Attack Speed", amount);
        Debug.Log($"Attack Speed upgraded by {amount}! New Cooldown: {playerData.GetAttackCooldown()}");
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
