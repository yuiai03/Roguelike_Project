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

    [Header("Circle Spawn Settings")]
    [SerializeField] private bool useCircleSpawn = false;
    [SerializeField] private float effectDuration = 1f;
    [SerializeField] private PoolType spawnEffectPoolType = PoolType.None;

    [Header("Current State")]
    [SerializeField] private int currentWave = 0;

    [Header("Events")]
    public UnityEvent<int> OnWaveStart;
    public UnityEvent<int> OnWaveComplete;
    public UnityEvent<int, int> OnEnemyCountChanged;
    public UnityEvent OnAllWavesComplete;
    public UnityEvent<int, string> OnBossWaveStart; // (waveNumber, bossName)

    private List<Enemy> activeEnemies = new List<Enemy>();
    private int totalEnemiesToSpawn;
    private int totalEnemiesSpawned;
    private bool isWaveActive;
    private List<SpawnPoint> pendingSpawns = new List<SpawnPoint>();

    private class SpawnPoint
    {
        public Vector3 position;
        public PoolType poolType;
    }

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

        // Log boss wave
        if (wave.isBossWave)
        {
            string bossName = GetBossNameForWave(currentWave);
            Debug.Log($"=== BOSS WAVE {currentWave}: {bossName} ===");
            OnBossWaveStart?.Invoke(currentWave, bossName);
        }

        StartCoroutine(RunWave(wave));
    }

    private string GetBossNameForWave(int wave) => wave switch
    {
        10 => "Stone Golem",
        20 => "Shadow Stalker",
        30 => "Void Titan",
        _  => "Boss"
    };

    private IEnumerator RunWave(SimpleWaveData wave)
    {
        Debug.Log($"=== Wave {currentWave} Incoming! ===");
        yield return new WaitForSeconds(wave.preparationTime);

        isWaveActive = true;
        totalEnemiesToSpawn = 0;
        totalEnemiesSpawned = 0;

        foreach (EnemyGroup group in wave.enemyGroups)
            totalEnemiesToSpawn += group.enemyCount;

        // Boss wave: +1 vào count
        if (wave.isBossWave && wave.bossPoolType != PoolType.None)
            totalEnemiesToSpawn += 1;

        Debug.Log($"=== Wave {currentWave} Started! ===");
        OnWaveStart?.Invoke(currentWave);

        if (useCircleSpawn)
            yield return StartCoroutine(SpawnCircle(wave));
        else
        {
            for (int i = 0; i < wave.enemyGroups.Count; i++)
            {
                EnemyGroup group = wave.enemyGroups[i];
                if (group.spawnDelay > 0f)
                    yield return new WaitForSeconds(group.spawnDelay);
                SpawnGroup(group, i + 1);
            }
        }

        // Spawn boss
        if (wave.isBossWave && wave.bossPoolType != PoolType.None)
        {
            Vector3 bossPos = transform.position + Vector3.forward * 5f;
            SpawnEnemyFromPool(wave.bossPoolType, bossPos);
            totalEnemiesSpawned++;
        }
    }

    private IEnumerator SpawnCircle(SimpleWaveData wave)
    {
        pendingSpawns.Clear();

        foreach (EnemyGroup group in wave.enemyGroups)
        {
            List<Vector3> usedPositions = new List<Vector3>();

            for (int i = 0; i < group.enemyCount; i++)
            {
                Vector3 spawnPos = CalculateRandomSpawnPosition(group.spawnPosition, group.spreadRadius, usedPositions);
                usedPositions.Add(spawnPos);

                if (spawnEffectPoolType != PoolType.None)
                {
                    GameObject effect = ObjectPool.Instance.Spawn(spawnEffectPoolType, spawnPos, Quaternion.identity);
                    ObjectPool.Instance.DespawnAfterDelay(effect, spawnEffectPoolType, effectDuration);
                }

                pendingSpawns.Add(new SpawnPoint
                {
                    position = spawnPos,
                    poolType = group.enemyPoolType
                });
            }
        }

        yield return new WaitForSeconds(effectDuration);

        foreach (SpawnPoint sp in pendingSpawns)
        {
            SpawnEnemyFromPool(sp.poolType, sp.position);
            totalEnemiesSpawned++;
        }

        OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);
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

    private Vector3 CalculateRandomSpawnPosition(Vector3 basePos, float radius, List<Vector3> usedPositions)
    {
        if (radius <= 0f)
            return basePos;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, radius);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );

            Vector3 candidatePos = basePos + offset;

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

        float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float fallbackDistance = Random.Range(0f, radius);
        return basePos + new Vector3(
            Mathf.Cos(fallbackAngle) * fallbackDistance,
            0f,
            Mathf.Sin(fallbackAngle) * fallbackDistance
        );
    }

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
            enemy.SetPoolType(poolType);

            if (waveConfig.autoScale)
            {
                EnemyData data = enemy.GetEnemyData();
                if (data != null)
                {
                    float scale = Mathf.Pow(waveConfig.scalePerWave, currentWave - 1);
                    data.maxHealth = Mathf.Round(data.maxHealth * scale);
                    data.contactDamage = Mathf.Round(data.contactDamage * scale);
                    data.projectileDamage = Mathf.Round(data.projectileDamage * scale);
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

        Invoke(nameof(StartNextWave), 5f);
    }

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

        if (useCircleSpawn)
        {
            if (Application.isPlaying && pendingSpawns.Count > 0)
            {
                foreach (SpawnPoint sp in pendingSpawns)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(sp.position, 0.5f);
                }
            }
            else
            {
                int groupIndex = 0;
                foreach (EnemyGroup group in wave.enemyGroups)
                {
                    Color groupColor = Color.HSVToRGB((groupIndex * 0.2f) % 1f, 0.8f, 1f);
                    Gizmos.color = groupColor;

                    Gizmos.DrawWireSphere(group.spawnPosition, 0.5f);

                    Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.3f);
                    Gizmos.DrawWireSphere(group.spawnPosition, group.spreadRadius);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(
                        group.spawnPosition + Vector3.up * 2f,
                        $"Circle Mode - Group {groupIndex + 1}\n{group.enemyCount} enemies\nEffect: {effectDuration}s"
                    );
#endif

                    groupIndex++;
                }
            }
        }
        else
        {
            int groupIndex = 0;
            foreach (EnemyGroup group in wave.enemyGroups)
            {
                Color groupColor = Color.HSVToRGB((groupIndex * 0.2f) % 1f, 0.8f, 1f);
                Gizmos.color = groupColor;

                Gizmos.DrawWireSphere(group.spawnPosition, 0.5f);

                Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.3f);
                Gizmos.DrawWireSphere(group.spawnPosition, group.spreadRadius);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    group.spawnPosition + Vector3.up * 2f,
                    $"Group {groupIndex + 1}\n{group.enemyCount} enemies ({group.enemyPoolType})\nDelay: {group.spawnDelay}s"
                );
#endif

                Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.2f);
                Gizmos.DrawSphere(group.spawnPosition, 0.5f);

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
}
