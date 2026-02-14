using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;
    public EnemyType enemyType;
    public int spawnWeight = 1; // T? l? spawn
    public int minWave = 1; // Wave t?i thi?u ?? spawn
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<EnemySpawnInfo> enemyPrefabs = new List<EnemySpawnInfo>();
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private float minSpawnDistance = 10f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Wave Settings")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int enemiesPerWave = 5;
    [SerializeField] private float enemyScalePerWave = 1.1f;
    [SerializeField] private float waveDelay = 5f;

    [Header("Spawn Limits")]
    [SerializeField] private int maxEnemiesAlive = 20;
    [SerializeField] private Transform spawnCenter;

    private List<Enemy> activeEnemies = new List<Enemy>();
    private float waveTimer;
    private bool waveActive;

    void Start()
    {
        if (spawnCenter == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                spawnCenter = player.transform;
            else
                spawnCenter = transform;
        }

        StartWave();
    }

    void Update()
    {
        CleanupDeadEnemies();

        if (!waveActive)
        {
            waveTimer -= Time.deltaTime;
            if (waveTimer <= 0f && activeEnemies.Count == 0)
            {
                currentWave++;
                StartWave();
            }
        }
    }

    public void StartWave()
    {
        waveActive = true;
        waveTimer = waveDelay;

        int enemiesToSpawn = enemiesPerWave + (currentWave - 1) * 2;

        Debug.Log($"=== Wave {currentWave} Starting! ===");
        Debug.Log($"Spawning {enemiesToSpawn} enemies...");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (activeEnemies.Count >= maxEnemiesAlive)
                break;

            SpawnRandomEnemy();
        }

        waveActive = false;
    }

    public void SpawnRandomEnemy()
    {
        if (enemyPrefabs.Count == 0) return;

        // L?c enemies có th? spawn theo wave
        List<EnemySpawnInfo> availableEnemies = enemyPrefabs.FindAll(e => e.minWave <= currentWave);
        if (availableEnemies.Count == 0) return;

        // Weighted random selection
        int totalWeight = 0;
        foreach (var enemy in availableEnemies)
        {
            totalWeight += enemy.spawnWeight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        EnemySpawnInfo selectedEnemy = availableEnemies[0];
        foreach (var enemy in availableEnemies)
        {
            currentWeight += enemy.spawnWeight;
            if (randomValue < currentWeight)
            {
                selectedEnemy = enemy;
                break;
            }
        }

        SpawnEnemy(selectedEnemy);
    }

    public void SpawnEnemy(EnemySpawnInfo spawnInfo)
    {
        Vector3 spawnPos = GetRandomSpawnPosition();
        if (spawnPos == Vector3.zero) return;

        GameObject enemyObj = Instantiate(spawnInfo.enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy != null)
        {
            // Scale enemy theo wave
            EnemyData data = enemy.GetEnemyData();
            if (data != null)
            {
                float waveScale = Mathf.Pow(enemyScalePerWave, currentWave - 1);
                data.maxHealth *= waveScale;
                data.contactDamage *= waveScale;
                data.projectileDamage *= waveScale;
                data.ResetHealth();
            }

            enemy.OnDeath.AddListener(() => OnEnemyDied(enemy));
            activeEnemies.Add(enemy);

            Debug.Log($"Spawned {spawnInfo.enemyType} enemy at wave {currentWave}");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        int maxAttempts = 30;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, spawnRadius);
            Vector3 spawnPos = spawnCenter.position + new Vector3(randomCircle.x, 0f, randomCircle.y) * distance;

            // Ki?m tra không spawn trong v?t c?n
            if (!Physics.CheckSphere(spawnPos, 1f, obstacleLayer))
            {
                spawnPos.y = spawnCenter.position.y;
                return spawnPos;
            }
        }

        Debug.LogWarning("Could not find valid spawn position!");
        return Vector3.zero;
    }

    private void CleanupDeadEnemies()
    {
        activeEnemies.RemoveAll(e => e == null || e.IsDead());
    }

    private void OnEnemyDied(Enemy enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }

        if (activeEnemies.Count == 0 && !waveActive)
        {
            Debug.Log($"Wave {currentWave} Complete! Next wave in {waveDelay} seconds...");
        }
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }

    public void ForceNextWave()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
        currentWave++;
        StartWave();
    }

    void OnDrawGizmosSelected()
    {
        if (spawnCenter == null)
            spawnCenter = transform;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnCenter.position, spawnRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spawnCenter.position, minSpawnDistance);
    }
}
