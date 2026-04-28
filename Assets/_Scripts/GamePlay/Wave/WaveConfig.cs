using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyGroup
{
    [Header("Enemies")]
    public PoolType enemyPoolType = PoolType.Enemy_Melee;
    public int enemyCount = 3;

    [Header("Spawn Position")]
    public Vector3 spawnPosition = Vector3.zero;
    public float spreadRadius = 2f;

    [Header("Timing")]
    public float spawnDelay = 0f;
}

[Serializable]
public class SimpleWaveData
{
    [Header("Enemy Groups")]
    public List<EnemyGroup> enemyGroups = new List<EnemyGroup>();

    [Header("Wave Settings")]
    public float preparationTime = 3f;

    [Header("Boss Wave")]
    [Tooltip("True neu day la boss wave (wave 10, 20, 30)")]
    public bool isBossWave = false;
    [Tooltip("Vi tri spawn boss o wave nay")]
    public Vector3 bossSpawnPosition = Vector3.zero;
    [Tooltip("Danh sach boss co the random trong wave nay")]
    public List<PoolType> bossPoolTypes = new List<PoolType>();
}

[CreateAssetMenu(fileName = "SimpleWaveConfig", menuName = "Roguelike/Simple Wave Config")]
public class WaveConfig : ScriptableObject
{
    public static readonly PoolType[] DefaultBossPoolTypes =
    {
        PoolType.Boss_Geo,
        PoolType.Boss_Pyro,
        PoolType.Boss_Electro
    };

    [Header("Waves")]
    public List<SimpleWaveData> waves = new List<SimpleWaveData>();

    [Header("Scaling")]
    public bool autoScale = true;
    public float scalePerWave = 1.1f;

    public SimpleWaveData GetWave(int waveNumber)
    {
        if (waveNumber <= 0 || waveNumber > waves.Count)
        {
            return null;
        }

        return waves[waveNumber - 1];
    }

    [ContextMenu("Generate 30 Waves")]
    public void Generate30Waves()
    {
        waves.Clear();

        for (int i = 1; i <= 30; i++)
        {
            bool isBoss = i % 10 == 0;

            SimpleWaveData wave = new SimpleWaveData
            {
                preparationTime = isBoss ? 5f : 3f,
                isBossWave = isBoss,
                bossSpawnPosition = Vector3.zero,
                bossPoolTypes = isBoss ? CreateDefaultBossPoolTypes() : new List<PoolType>()
            };

            if (!isBoss)
            {
                wave.enemyGroups.Add(new EnemyGroup
                {
                    enemyPoolType = PoolType.Enemy_Melee,
                    enemyCount = 3 + i / 3,
                    spreadRadius = 3f,
                });
            }

            waves.Add(wave);
        }

        Debug.Log("Generated 30 waves! Wave 10/20/30 da danh dau la Boss Wave va da gan san trio boss LawaChurl.");
    }

    public static List<PoolType> CreateDefaultBossPoolTypes()
    {
        return new List<PoolType>(DefaultBossPoolTypes);
    }
}
