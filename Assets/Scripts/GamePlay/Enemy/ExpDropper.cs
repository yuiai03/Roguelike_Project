using UnityEngine;

/// <summary>
/// Component tự động drop exp khi enemy chết
/// Attach vào enemy prefab
/// </summary>
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(EnemyData))]
public class ExpDropper : MonoBehaviour
{
    private Enemy enemy;
    private EnemyData enemyData;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyData = GetComponent<EnemyData>();
    }

    private void OnEnable()
    {
        if (enemy != null)
        {
            enemy.OnDeath.AddListener(DropExp);
        }
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnDeath.RemoveListener(DropExp);
        }
    }

    private void DropExp()
    {
        if (enemyData == null)
        {
            Debug.LogWarning($"EnemyData not found on {gameObject.name}");
            return;
        }

        PlayerLevelSystem levelSystem = PlayerLevelSystem.Instance;
        if (levelSystem == null)
        {
            Debug.LogWarning("PlayerLevelSystem instance not found!");
            return;
        }

        int expToDrop = enemyData.expValue;
        levelSystem.AddExp(expToDrop);

        Debug.Log($"{gameObject.name} dropped {expToDrop} EXP!");
    }
}
