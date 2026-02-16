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

    [ContextMenu("Generate 30 Empty Waves")]
    public void Generate30Waves()
    {
        waves.Clear();

        for (int i = 1; i <= 30; i++)
        {
            SimpleWaveData wave = new SimpleWaveData
            {
                preparationTime = 3f
            };

            waves.Add(wave);
        }

        Debug.Log("Generated 30 empty waves! Add enemy groups in Inspector.");
    }
}
