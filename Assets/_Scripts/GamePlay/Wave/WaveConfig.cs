using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyGroup
{
    [Header("Enemies")]
    public PoolType enemyPoolType = PoolType.MeleeEnemy;
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
    [Tooltip("True nếu đây là boss wave (wave 10, 20, 30)")]
    public bool isBossWave = false;
    [Tooltip("PoolType của boss boss được spawn ở wave này")]
    public PoolType bossPoolType = PoolType.None;
}

[CreateAssetMenu(fileName = "SimpleWaveConfig", menuName = "Roguelike/Simple Wave Config")]
public class WaveConfig : ScriptableObject
{
    [Header("Waves")]
    public List<SimpleWaveData> waves = new List<SimpleWaveData>();

    [Header("Scaling")]
    public bool autoScale = true;
    public float scalePerWave = 1.1f;

    public SimpleWaveData GetWave(int waveNumber)
    {
        if (waveNumber <= 0 || waveNumber > waves.Count)
            return null;

        return waves[waveNumber - 1];
    }

    [ContextMenu("Generate 30 Waves")]
    public void Generate30Waves()
    {
        waves.Clear();

        for (int i = 1; i <= 30; i++)
        {
            bool isBoss = (i % 10 == 0); 

            SimpleWaveData wave = new SimpleWaveData
            {
                preparationTime = isBoss ? 5f : 3f, 
                isBossWave      = isBoss,
                bossPoolType    = PoolType.None,     
            };

            if (!isBoss)
            {
                wave.enemyGroups.Add(new EnemyGroup
                {
                    enemyPoolType = PoolType.MeleeEnemy,
                    enemyCount    = 3 + i / 3, 
                    spreadRadius  = 3f,
                });
            }

            waves.Add(wave);
        }

        Debug.Log("Generated 30 waves! Wave 10/20/30 đánh dấu là Boss Wave. Gán bossPoolType trong Inspector!");
    }
}
