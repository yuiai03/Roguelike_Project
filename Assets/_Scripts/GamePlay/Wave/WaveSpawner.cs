using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaveSpawner : Singleton<WaveSpawner>
{
    private const float SpawnCircleVisualLift = 0.05f;

    [Header("Configuration")]
    [SerializeField] private WaveConfig waveConfig;

    [Header("Settings")]
    [SerializeField] private float spawnRandomRadius = 2f;
    [SerializeField] private int maxSpawnAttempts = 10;

    [Header("Circle Spawn Settings")]
    [SerializeField] private bool useCircleSpawn = false;
    [SerializeField] private float effectDuration = 1f;
    [SerializeField] private PoolType spawnEffectPoolType = PoolType.None;
    [SerializeField] private LayerMask spawnEffectGroundMask;

    [Header("Current State")]
    [SerializeField] private int currentWave = 0;

    [Header("Events")]
    public UnityEvent<int> OnWaveStart;
    public UnityEvent<int> OnWaveComplete;
    public UnityEvent<int, int> OnEnemyCountChanged;
    public UnityEvent OnAllWavesComplete;
    public UnityEvent<int, string> OnBossWaveStart;

    private readonly List<Enemy> activeEnemies = new List<Enemy>();
    private readonly List<SpawnPoint> pendingSpawns = new List<SpawnPoint>();

    private int totalEnemiesToSpawn;
    private int totalEnemiesSpawned;
    private int waveSessionId;
    private bool isWaveActive;
    private Coroutine pendingWaveTransitionRoutine;

    private class SpawnPoint
    {
        public Vector3 position;
        public PoolType poolType;
    }

    protected override void Awake()
    {
        base.Awake();

        if (spawnEffectGroundMask.value == 0)
        {
            spawnEffectGroundMask = LayerMask.GetMask("Ground");
        }
    }

    private void Update()
    {
        CleanupDeadEnemies();

        if (isWaveActive && totalEnemiesSpawned >= totalEnemiesToSpawn && activeEnemies.Count == 0)
        {
            CompleteWave();
        }
    }

    public void StartNextWave()
    {
        if (waveConfig == null)
        {
            return;
        }

        CancelInvoke(nameof(StartNextWave));
        currentWave++;

        SimpleWaveData wave = currentWave <= waveConfig.waves.Count
            ? waveConfig.GetWave(currentWave)
            : GenerateEndlessWave(currentWave);

        if (wave == null)
        {
            Debug.LogError($"Wave {currentWave} not found or generated.");
            return;
        }

        int sessionId = BeginNewWaveSession();
        PoolType bossPoolType = wave.isBossWave ? PickRandomBossPoolType(wave.bossPoolTypes) : PoolType.None;

        if (wave.isBossWave)
        {
            string bossName = GetBossName(bossPoolType);
            Debug.Log($"=== BOSS WAVE {currentWave}: {bossName} ===");
            OnBossWaveStart?.Invoke(currentWave, bossName);
        }

        StartCoroutine(RunWave(wave, bossPoolType, sessionId));
    }

    public bool JumpToWave(int targetWave)
    {
        if (waveConfig == null || targetWave < 1)
        {
            return false;
        }

        CancelInvoke(nameof(StartNextWave));

        if (pendingWaveTransitionRoutine != null)
        {
            StopCoroutine(pendingWaveTransitionRoutine);
            pendingWaveTransitionRoutine = null;
        }

        StopAllCoroutines();
        InvalidateCurrentWaveSession();
        pendingSpawns.Clear();

        isWaveActive = false;
        totalEnemiesToSpawn = 0;
        totalEnemiesSpawned = 0;

        KillAllEnemies();
        MapThemeManager.Instance?.ApplyThemeForWaveImmediate(targetWave);

        currentWave = targetWave - 1;
        StartNextWave();
        return currentWave == targetWave;
    }

    public void ForceNextWave()
    {
        JumpToWave(Mathf.Max(1, currentWave + 1));
    }

    public int GetCurrentWave() => currentWave;
    public int GetTotalWaves() => waveConfig != null ? waveConfig.waves.Count : 0;
    public int GetActiveEnemyCount() => activeEnemies.Count;
    public int GetTotalEnemies() => totalEnemiesToSpawn;
    public bool IsWaveActive() => isWaveActive;
    public int GetWaveSessionId() => waveSessionId;
    public bool IsWaveSessionCurrent(int sessionId) => sessionId == waveSessionId;

    public static string FormatWaveLabel(int currentWave, int totalWaves)
    {
        if (currentWave <= 0)
        {
            return totalWaves > 0 ? $"Wave: 0/{totalWaves}" : "Wave: 0";
        }

        if (totalWaves > 0 && currentWave > totalWaves)
        {
            return $"Wave: {currentWave} (Endless)";
        }

        return totalWaves > 0 ? $"Wave: {currentWave}/{totalWaves}" : $"Wave: {currentWave}";
    }

    public static PoolType PickRandomBossPoolType(IReadOnlyList<PoolType> bossPoolTypes)
    {
        List<PoolType> candidates = new List<PoolType>();

        if (bossPoolTypes != null)
        {
            for (int i = 0; i < bossPoolTypes.Count; i++)
            {
                PoolType candidate = bossPoolTypes[i];
                if (IsSupportedBossPool(candidate))
                {
                    candidates.Add(candidate);
                }
            }
        }

        if (candidates.Count == 0)
        {
            candidates.AddRange(WaveConfig.DefaultBossPoolTypes);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private static bool IsSupportedBossPool(PoolType poolType)
    {
        return poolType == PoolType.Boss_Geo
            || poolType == PoolType.Boss_Pyro
            || poolType == PoolType.Boss_Electro;
    }

    private int BeginNewWaveSession()
    {
        waveSessionId++;
        pendingSpawns.Clear();
        return waveSessionId;
    }

    private void InvalidateCurrentWaveSession()
    {
        waveSessionId++;
    }

    private SimpleWaveData GenerateEndlessWave(int waveNumber)
    {
        if (waveConfig == null || waveConfig.waves.Count == 0)
        {
            return null;
        }

        int baseIndex = (waveNumber - 1) % waveConfig.waves.Count;
        int loopCount = (waveNumber - 1) / waveConfig.waves.Count;
        SimpleWaveData baseWave = waveConfig.waves[baseIndex];

        SimpleWaveData endlessWave = new SimpleWaveData
        {
            preparationTime = baseWave.preparationTime,
            isBossWave = baseWave.isBossWave,
            bossSpawnPosition = baseWave.bossSpawnPosition,
            bossPoolTypes = new List<PoolType>(baseWave.bossPoolTypes),
            enemyGroups = new List<EnemyGroup>()
        };

        int extraEnemies = loopCount;
        float extraRadius = loopCount;

        foreach (EnemyGroup baseGroup in baseWave.enemyGroups)
        {
            endlessWave.enemyGroups.Add(new EnemyGroup
            {
                enemyPoolType = baseGroup.enemyPoolType,
                enemyCount = baseGroup.enemyCount + extraEnemies,
                spawnPosition = baseGroup.spawnPosition,
                spreadRadius = baseGroup.spreadRadius + extraRadius,
                spawnDelay = baseGroup.spawnDelay
            });
        }

        return endlessWave;
    }

    private IEnumerator RunWave(SimpleWaveData wave, PoolType bossPoolType, int sessionId)
    {
        Debug.Log($"=== Wave {currentWave} Incoming! ===");
        yield return new WaitForSeconds(wave.preparationTime);

        if (!IsWaveSessionCurrent(sessionId))
        {
            yield break;
        }

        isWaveActive = true;
        totalEnemiesSpawned = 0;
        totalEnemiesToSpawn = wave.isBossWave ? 1 : CountEnemiesToSpawn(wave);
        OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);

        Debug.Log($"=== Wave {currentWave} Started! ===");
        OnWaveStart?.Invoke(currentWave);

        if (wave.isBossWave)
        {
            if (useCircleSpawn && spawnEffectPoolType != PoolType.None && effectDuration > 0f)
            {
                StartCoroutine(SpawnBossAfterEffectRoutine(wave, bossPoolType, sessionId));
            }
            else
            {
                SpawnBossNow(wave, bossPoolType, sessionId);
            }

            yield break;
        }

        if (useCircleSpawn)
        {
            yield return StartCoroutine(SpawnCircle(wave, sessionId));
            yield break;
        }

        for (int i = 0; i < wave.enemyGroups.Count; i++)
        {
            StartCoroutine(SpawnGroupRoutine(wave.enemyGroups[i], i + 1, sessionId));
        }
    }

    private IEnumerator SpawnGroupRoutine(EnemyGroup group, int groupIndex, int sessionId)
    {
        if (group.spawnDelay > 0f)
        {
            yield return new WaitForSeconds(group.spawnDelay);
        }

        if (!IsWaveSessionCurrent(sessionId))
        {
            yield break;
        }

        SpawnGroup(group, groupIndex, sessionId);
    }

    private IEnumerator SpawnCircle(SimpleWaveData wave, int sessionId)
    {
        pendingSpawns.Clear();

        foreach (EnemyGroup group in wave.enemyGroups)
        {
            StartCoroutine(SpawnCircleGroupRoutine(group, sessionId));
        }

        yield return null;
    }

    private IEnumerator SpawnCircleGroupRoutine(EnemyGroup group, int sessionId)
    {
        if (group.spawnDelay > 0f)
        {
            yield return new WaitForSeconds(group.spawnDelay);
        }

        if (!IsWaveSessionCurrent(sessionId))
        {
            yield break;
        }

        List<Vector3> usedPositions = new List<Vector3>();
        List<SpawnPoint> groupPendingSpawns = new List<SpawnPoint>();

        for (int i = 0; i < group.enemyCount; i++)
        {
            Vector3 spawnPos = CalculateRandomSpawnPosition(group.spawnPosition, group.spreadRadius, usedPositions);
            usedPositions.Add(spawnPos);

            SpawnPoint pendingSpawn = new SpawnPoint
            {
                position = spawnPos,
                poolType = group.enemyPoolType
            };

            pendingSpawns.Add(pendingSpawn);
            groupPendingSpawns.Add(pendingSpawn);
            SpawnSpawnEffect(spawnPos);
        }

        if (spawnEffectPoolType != PoolType.None || effectDuration > 0f)
        {
            yield return new WaitForSeconds(effectDuration);
        }

        if (!IsWaveSessionCurrent(sessionId))
        {
            RemovePendingSpawns(groupPendingSpawns);
            yield break;
        }

        foreach (SpawnPoint pendingSpawn in groupPendingSpawns)
        {
            pendingSpawns.Remove(pendingSpawn);
            SpawnEnemyFromPool(pendingSpawn.poolType, pendingSpawn.position);
            totalEnemiesSpawned++;
        }

        OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);
    }

    private IEnumerator SpawnBossAfterEffectRoutine(SimpleWaveData wave, PoolType bossPoolType, int sessionId)
    {
        Vector3 bossSpawnPosition = GetBossSpawnPosition(wave.bossSpawnPosition);
        SpawnPoint pendingSpawn = new SpawnPoint
        {
            position = bossSpawnPosition,
            poolType = bossPoolType
        };

        pendingSpawns.Add(pendingSpawn);
        SpawnSpawnEffect(bossSpawnPosition);

        if (effectDuration > 0f)
        {
            yield return new WaitForSeconds(effectDuration);
        }

        if (!IsWaveSessionCurrent(sessionId))
        {
            pendingSpawns.Remove(pendingSpawn);
            yield break;
        }

        pendingSpawns.Remove(pendingSpawn);
        SpawnEnemyFromPool(bossPoolType, bossSpawnPosition);
        totalEnemiesSpawned = totalEnemiesToSpawn;
        OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);
    }

    private void SpawnBossNow(SimpleWaveData wave, PoolType bossPoolType, int sessionId)
    {
        if (!IsWaveSessionCurrent(sessionId))
        {
            return;
        }

        Vector3 bossSpawnPosition = GetBossSpawnPosition(wave.bossSpawnPosition);
        SpawnEnemyFromPool(bossPoolType, bossSpawnPosition);
        totalEnemiesSpawned = totalEnemiesToSpawn;
        OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);
    }

    private void SpawnSpawnEffect(Vector3 spawnPos)
    {
        if (spawnEffectPoolType == PoolType.None || ObjectPool.Instance == null)
        {
            return;
        }

        Vector3 effectSpawnPos = GetSpawnEffectPosition(spawnPos);
        GameObject effect = ObjectPool.Instance.Spawn(spawnEffectPoolType, effectSpawnPos, Quaternion.identity);
        if (effect == null)
        {
            return;
        }

        ObjectPool.Instance.DespawnAfterDelay(effect, spawnEffectPoolType, effectDuration);

        Transform circleTransform = null;
        for (int i = 0; i < effect.transform.childCount; i++)
        {
            Transform child = effect.transform.GetChild(i);
            if (child.name.Equals("circle", System.StringComparison.OrdinalIgnoreCase))
            {
                circleTransform = child;
                break;
            }
        }

        if (circleTransform == null)
        {
            circleTransform = effect.transform.Find("circle") ?? effect.transform.Find("Circle");
        }

        if (circleTransform != null)
        {
            StartCoroutine(ScaleCircleRoutine(circleTransform, effectDuration));
        }
    }

    private IEnumerator ScaleCircleRoutine(Transform circleTransform, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = new Vector3(1.2f, 1.2f, 1.2f);

        if (circleTransform != null)
        {
            circleTransform.localScale = startScale;
        }

        while (elapsed < duration)
        {
            if (circleTransform == null || !circleTransform.gameObject.activeInHierarchy)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            circleTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        if (circleTransform != null)
        {
            circleTransform.localScale = endScale;
        }
    }

    private Vector3 GetSpawnEffectPosition(Vector3 spawnPos)
    {
        Vector3 groundPos = Utils.GetGroundPosition(spawnPos, spawnEffectGroundMask);
        return groundPos + Vector3.up * SpawnCircleVisualLift;
    }

    private Vector3 GetBossSpawnPosition(Vector3 requestedPosition)
    {
        return Utils.GetGroundPosition(requestedPosition, spawnEffectGroundMask);
    }

    private void SpawnGroup(EnemyGroup group, int groupIndex, int sessionId)
    {
        if (!IsWaveSessionCurrent(sessionId))
        {
            return;
        }

        if (ObjectPool.Instance == null)
        {
            Debug.LogError("ObjectPool instance not found.");
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
        {
            return basePos;
        }

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(0f, radius);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance);

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
            {
                return candidatePos;
            }
        }

        float fallbackAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float fallbackDistance = Random.Range(0f, radius);
        return basePos + new Vector3(
            Mathf.Cos(fallbackAngle) * fallbackDistance,
            0f,
            Mathf.Sin(fallbackAngle) * fallbackDistance);
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
        if (enemy == null)
        {
            return;
        }

        enemy.SetPoolType(poolType);

        if (waveConfig != null && waveConfig.autoScale)
        {
            EnemyData data = enemy.GetEnemyData();
            if (data != null)
            {
                float scale = Mathf.Pow(waveConfig.scalePerWave, Mathf.Max(0, currentWave - 1));
                data.maxHealth = Utils.RoundToDisplayInt(data.maxHealth * scale);
                data.contactDamage = Utils.RoundToDisplayInt(data.contactDamage * scale);
                data.projectileDamage = Utils.RoundToDisplayInt(data.projectileDamage * scale);
                enemy.RefreshHealthState();
            }
        }

        enemy.OnDeath.AddListener(() => OnEnemyDied(enemy));
        activeEnemies.Add(enemy);
    }

    private void CleanupDeadEnemies()
    {
        activeEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead());
    }

    private void OnEnemyDied(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            return;
        }

        activeEnemies.Remove(enemy);
        OnEnemyCountChanged?.Invoke(activeEnemies.Count, totalEnemiesToSpawn);
        Debug.Log($"Enemy killed! Remaining: {activeEnemies.Count}/{totalEnemiesToSpawn}");
    }

    private void CompleteWave()
    {
        if (!isWaveActive)
        {
            return;
        }

        isWaveActive = false;
        Debug.Log($"=== Wave {currentWave} Complete! ===");
        OnWaveComplete?.Invoke(currentWave);

        if (ShouldTransitionMapForNextWave())
        {
            if (pendingWaveTransitionRoutine != null)
            {
                StopCoroutine(pendingWaveTransitionRoutine);
            }

            pendingWaveTransitionRoutine = StartCoroutine(HandleMapTransitionAndStartNextWave());
            return;
        }

        Invoke(nameof(StartNextWave), 5f);
    }

    public void KillAllEnemies()
    {
        foreach (Enemy enemy in activeEnemies.ToArray())
        {
            if (enemy == null || enemy.IsDead())
            {
                continue;
            }

            PoolType poolType = enemy.GetPoolType();
            if (poolType != PoolType.None && ObjectPool.Instance != null)
            {
                enemy.gameObject.SetActive(false);
                ObjectPool.Instance.Despawn(enemy.gameObject, poolType);
            }
            else
            {
                Destroy(enemy.gameObject);
            }
        }

        activeEnemies.Clear();
        OnEnemyCountChanged?.Invoke(0, totalEnemiesToSpawn);
    }

    private bool ShouldTransitionMapForNextWave()
    {
        if (currentWave <= 0 || currentWave % 10 != 0)
        {
            return false;
        }

        MapThemeManager mapThemeManager = MapThemeManager.Instance;
        return mapThemeManager != null && mapThemeManager.WillThemeChangeForWave(currentWave + 1);
    }

    private IEnumerator HandleMapTransitionAndStartNextWave()
    {
        int upcomingWave = currentWave + 1;
        bool transitionCompleted = false;

        LockGameplayForMapTransition();
        MapThemeManager.Instance?.TransitionToWaveTheme(upcomingWave, () => transitionCompleted = true);

        while (!transitionCompleted)
        {
            yield return null;
        }

        RestoreGameplayAfterMapTransition();
        pendingWaveTransitionRoutine = null;
        StartNextWave();
    }

    private void LockGameplayForMapTransition()
    {
        Time.timeScale = 0f;
        GameUI.Instance?.InteractPanel?.Hide();
        PlayerController.Instance?.SetInputActive(false);
    }

    private void RestoreGameplayAfterMapTransition()
    {
        Time.timeScale = 1f;

        if (PlayerController.Instance != null && (PlayerHealth.Instance == null || !PlayerHealth.Instance.IsDead()))
        {
            PlayerController.Instance.SetInputActive(true);
        }
    }

    private static int CountEnemiesToSpawn(SimpleWaveData wave)
    {
        int count = 0;
        foreach (EnemyGroup group in wave.enemyGroups)
        {
            count += group.enemyCount;
        }

        return count;
    }

    private static string GetBossName(PoolType bossPoolType)
    {
        return bossPoolType switch
        {
            PoolType.Boss_Geo => "LawaChurl Geo",
            PoolType.Boss_Pyro => "LawaChurl Pyro",
            PoolType.Boss_Electro => "LawaChurl Electro",
            _ => "Boss"
        };
    }

    private void RemovePendingSpawns(List<SpawnPoint> groupPendingSpawns)
    {
        foreach (SpawnPoint pendingSpawn in groupPendingSpawns)
        {
            pendingSpawns.Remove(pendingSpawn);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (waveConfig == null || currentWave <= 0 || currentWave > waveConfig.waves.Count)
        {
            return;
        }

        SimpleWaveData wave = waveConfig.GetWave(currentWave);
        if (wave == null)
        {
            return;
        }

        if (useCircleSpawn)
        {
            if (Application.isPlaying && pendingSpawns.Count > 0)
            {
                foreach (SpawnPoint spawnPoint in pendingSpawns)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
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

                Gizmos.color = new Color(groupColor.r, groupColor.g, groupColor.b, 0.2f);
                Gizmos.DrawSphere(group.spawnPosition, 0.5f);

                Gizmos.color = groupColor;
                int previewCount = Mathf.Min(group.enemyCount, 8);
                for (int i = 0; i < previewCount; i++)
                {
                    float angle = (360f / previewCount) * i * Mathf.Deg2Rad;
                    float distance = group.spreadRadius * 0.6f;
                    Vector3 previewPosition = group.spawnPosition + new Vector3(
                        Mathf.Cos(angle) * distance,
                        0f,
                        Mathf.Sin(angle) * distance);
                    Gizmos.DrawWireSphere(previewPosition, 0.3f);
                }

                groupIndex++;
            }
        }

        if (!wave.isBossWave)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetBossSpawnPosition(wave.bossSpawnPosition), 1f);
    }
}
