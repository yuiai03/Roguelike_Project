using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class WaveSpawner : Singleton<WaveSpawner>
{
    [Header("Configuration")]
    [SerializeField] private WaveConfig waveConfig;

    [Header("Settings")]
    [SerializeField] private float spawnRandomRadius = 2f;
    [SerializeField] private int maxSpawnAttempts = 10;

    [Header("Current State")]
    [SerializeField] private int currentWave = 0;

    [Header("Events")]
    public UnityEvent<int> OnWaveStart;
    public UnityEvent<int> OnWaveComplete;
    public UnityEvent<int, int> OnEnemyCountChanged;
    public UnityEvent OnAllWavesComplete;

    private List<Enemy> activeEnemies = new List<Enemy>();
    private int totalEnemiesToSpawn;
    private int totalEnemiesSpawned;
    private bool isWaveActive;

    void Start()
    {
        if (waveConfig != null)
        {
            StartNextWave();
        }
        else
        {
            Debug.LogError("No WaveConfig assigned!");
        }
    }

    void Update()
    {
        CleanupDeadEnemies();

        // Check wave complete
        if (isWaveActive && totalEnemiesSpawned >= totalEnemiesToSpawn && activeEnemies.Count == 0)
        {
            CompleteWave();
        }
    }

    public void StartNextWave()
    {
        if (waveConfig == null) return;

        currentWave++;

        if (currentWave > waveConfig.waves.Count)
        {
            Debug.Log("=== ALL WAVES COMPLETED! ===");
            OnAllWavesComplete?.Invoke();
            return;
        }

        SimpleWaveData wave = waveConfig.GetWave(currentWave);
        if (wave == null)
        {
            Debug.LogError($"Wave {currentWave} not found!");
            return;
        }

        StartCoroutine(RunWave(wave));
    }

    private IEnumerator RunWave(SimpleWaveData wave)
    {
        // Preparation
        Debug.Log($"=== Wave {currentWave} Incoming! ===");
        yield return new WaitForSeconds(wave.preparationTime);

        // Start wave
        isWaveActive = true;
        totalEnemiesToSpawn = 0;
        totalEnemiesSpawned = 0;

        // Count total enemies
        foreach (EnemyGroup group in wave.enemyGroups)
        {
            totalEnemiesToSpawn += group.enemyCount;
        }

        Debug.Log($"=== Wave {currentWave} Started! ===");
        Debug.Log($"Total Enemies: {totalEnemiesToSpawn}");
        OnWaveStart?.Invoke(currentWave);

        // Spawn groups 
        for (int i = 0; i < wave.enemyGroups.Count; i++)
        {
            EnemyGroup group = wave.enemyGroups[i];
            
            if (group.spawnDelay > 0f)
            {
                Debug.Log($"Waiting {group.spawnDelay}s before spawning Group {i + 1}...");
                yield return new WaitForSeconds(group.spawnDelay);
            }

            SpawnGroup(group, i + 1);
        }
    }

    private void SpawnGroup(EnemyGroup group, int groupIndex)
    {
        if (ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool instance not found!");
            return;
        }

        Debug.Log($"Spawning Group {groupIndex}: {group.enemyCount} enemies ({group.enemyPoolType}) at {group.spawnPosition}");

        List<Vector3> usedPositions = new List<Vector3>();

        for (int i = 0; i < group.enemyCount; i++)
        {
            Vector3 spawnPos = CalculateRandomSpawnPosition(group.spawnPosition, group.spreadRadius, usedPositions);
            usedPositions.Add(spawnPos);

            SpawnEnemyFromPool(group.enemyPoolType, spawnPos);
            totalEnemiesSpawned++;
        }

        OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);
    }

    /// <summary>
    /// Tính toán vị trí spawn ngẫu nhiên xung quanh basePos, tránh các vị trí đã dùng
    /// </summary>
    private Vector3 CalculateRandomSpawnPosition(Vector3 basePos, float radius, List<Vector3> usedPositions)
    {
        if (radius <= 0f)
            return basePos;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Random góc và khoảng cách
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, radius);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );

            Vector3 candidatePos = basePos + offset;

            // Kiểm tra không quá gần với các vị trí đã spawn
            bool tooClose = false;
            foreach (Vector3 usedPos in usedPositions)
            {
                if (Vector3.Distance(candidatePos, usedPos) < spawnRandomRadius)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return candidatePos;
        }

        // Nếu không tìm được vị trí sau maxSpawnAttempts, trả về vị trí random
        float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float fallbackDistance = Random.Range(0f, radius);
        return basePos + new Vector3(
            Mathf.Cos(fallbackAngle) * fallbackDistance,
            0f,
            Mathf.Sin(fallbackAngle) * fallbackDistance
        );
    }

    /// <summary>
    /// Spawn enemy từ object pool
    /// </summary>
    private void SpawnEnemyFromPool(PoolType poolType, Vector3 position)
    {
        GameObject enemyObj = ObjectPool.Instance.Spawn(poolType, position, Quaternion.identity);

        if (enemyObj == null)
        {
            Debug.LogError($"Failed to spawn enemy from pool: {poolType}");
            return;
        }

        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy != null)
        {
            // Set pool type để enemy biết return về pool nào
            enemy.SetPoolType(poolType);

            // Apply wave scaling
            if (waveConfig.autoScale)
            {
                EnemyData data = enemy.GetEnemyData();
                if (data != null)
                {
                    float scale = Mathf.Pow(waveConfig.scalePerWave, currentWave - 1);
                    data.maxHealth *= scale;
                    data.contactDamage *= scale;
                    data.projectileDamage *= scale;
                    data.ResetHealth();
                }
            }

            enemy.OnDeath.AddListener(() => OnEnemyDied(enemy));
            activeEnemies.Add(enemy);
        }
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
            OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);

            Debug.Log($"Enemy killed! Remaining: {activeEnemies.Count}/{totalEnemiesToSpawn}");
        }
    }

    private void CompleteWave()
    {
        if (!isWaveActive) return;

        isWaveActive = false;

        Debug.Log($"=== Wave {currentWave} Complete! ===");

        OnWaveComplete?.Invoke(currentWave);

        // Auto start next wave after 5s
        Invoke(nameof(StartNextWave), 5f);
    }

    // Public methods
    public void ForceNextWave()
    {
        CancelInvoke(nameof(StartNextWave));
        KillAllEnemies();
        StartNextWave();
    }

    public void KillAllEnemies()
    {
        foreach (var enemy in activeEnemies.ToArray())
        {
            if (enemy != null && !enemy.IsDead())
            {
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();
        OnEnemyCountChanged?.Invoke(0, totalEnemiesToSpawn);
    }

    public int GetCurrentWave() => currentWave;
    public int GetTotalWaves() => waveConfig != null ? waveConfig.waves.Count : 0;
    public int GetActiveEnemyCount() => activeEnemies.Count;
    public int GetTotalEnemies() => totalEnemiesToSpawn;
    public bool IsWaveActive() => isWaveActive;

    void OnDrawGizmosSelected()
    {
        if (waveConfig == null || currentWave <= 0 || currentWave > waveConfig.waves.Count)
            return;

        SimpleWaveData wave = waveConfig.GetWave(currentWave);
        if (wave == null) return;

        // Draw each group spawn position
        int groupIndex = 0;
        foreach (EnemyGroup group in wave.enemyGroups)
        {
            Color groupColor = Color.HSVToRGB((groupIndex * 0.2f) % 1f, 0.8f, 1f);
            Gizmos.color = groupColor;

            // Draw center
            Gizmos.DrawWireSphere(group.spawnPosition, 0.5f);

            // Draw spread radius
            Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.3f);
            Gizmos.DrawWireSphere(group.spawnPosition, group.spreadRadius);

            // Draw label
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                group.spawnPosition + Vector3.up * 2f,
                $"Group {groupIndex + 1}\n{group.enemyCount} enemies ({group.enemyPoolType})\nDelay: {group.spawnDelay}s"
            );
#endif

            // Draw spawn area preview
            Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.2f);
            Gizmos.DrawSphere(group.spawnPosition, 0.5f);

            // Draw example spawn positions
            Gizmos.color = groupColor;
            int previewCount = Mathf.Min(group.enemyCount, 8);
            for (int i = 0; i < previewCount; i++)
            {
                float angle = (360f / previewCount) * i * Mathf.Deg2Rad;
                float dist = group.spreadRadius * 0.6f;
                Vector3 pos = group.spawnPosition + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                Gizmos.DrawWireSphere(pos, 0.3f);
            }

            groupIndex++;
        }
    }
}
