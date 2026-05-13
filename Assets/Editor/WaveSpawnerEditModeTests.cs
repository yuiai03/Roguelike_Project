using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WaveSpawnerEditModeTests
{
    private GameObject spawnerObject;
    private WaveSpawner spawner;
    private WaveConfig waveConfig;

    [SetUp]
    public void SetUp()
    {
        ResetSingletonInstance();

        spawnerObject = new GameObject("WaveSpawnerTestHost");
        spawner = spawnerObject.AddComponent<WaveSpawner>();

        waveConfig = ScriptableObject.CreateInstance<WaveConfig>();
        waveConfig.waves = new List<SimpleWaveData>
        {
            new SimpleWaveData
            {
                enemyGroups = new List<EnemyGroup>
                {
                    new EnemyGroup { enemyPoolType = PoolType.Enemy_Melee, enemyCount = 1 }
                }
            },
            new SimpleWaveData
            {
                isBossWave = true,
                bossSpawnPosition = new Vector3(0f, 0.75f, 0f),
                bossPoolTypes = WaveConfig.CreateDefaultBossPoolTypes()
            }
        };

        SetPrivateField(spawner, "waveConfig", waveConfig);
    }

    [TearDown]
    public void TearDown()
    {
        if (spawnerObject != null)
        {
            Object.DestroyImmediate(spawnerObject);
        }

        if (waveConfig != null)
        {
            Object.DestroyImmediate(waveConfig);
        }

        ResetSingletonInstance();
    }

    [Test]
    public void PickRandomBossPoolType_OnlyReturnsConfiguredBosses()
    {
        List<PoolType> candidates = new List<PoolType>
        {
            PoolType.Boss_Geo,
            PoolType.Boss_Pyro,
            PoolType.Boss_Electro
        };

        for (int i = 0; i < 50; i++)
        {
            PoolType selected = WaveSpawner.PickRandomBossPoolType(candidates);
            Assert.Contains(selected, candidates);
        }
    }

    [Test]
    public void PickRandomBossPoolType_FallsBackToDefaultTrio()
    {
        List<PoolType> candidates = new List<PoolType>();

        for (int i = 0; i < 50; i++)
        {
            PoolType selected = WaveSpawner.PickRandomBossPoolType(candidates);
            Assert.That(
                selected == PoolType.Boss_Geo
                || selected == PoolType.Boss_Pyro
                || selected == PoolType.Boss_Electro,
                Is.True);
        }
    }

    [Test]
    public void FormatWaveLabel_UsesWaveNumberOnly_WhenWaveExceedsConfiguredCount()
    {
        Assert.AreEqual("Wave: 17", WaveSpawner.FormatWaveLabel(17, 10));
        Assert.AreEqual("Wave: 5/10", WaveSpawner.FormatWaveLabel(5, 10));
    }

    [Test]
    public void GenerateEndlessWave_ClonesConfiguredEnemyCountsAndSpread()
    {
        waveConfig.waves = new List<SimpleWaveData>
        {
            new SimpleWaveData
            {
                enemyGroups = new List<EnemyGroup>
                {
                    new EnemyGroup
                    {
                        enemyPoolType = PoolType.Enemy_Melee,
                        enemyCount = 2,
                        spawnPosition = new Vector3(1f, 0f, 2f),
                        spreadRadius = 1.5f,
                        spawnDelay = 0.25f
                    },
                    new EnemyGroup
                    {
                        enemyPoolType = PoolType.Enemy_Ranged,
                        enemyCount = 7,
                        spawnPosition = new Vector3(3f, 0f, 4f),
                        spreadRadius = 4.5f,
                        spawnDelay = 1.5f
                    }
                }
            }
        };

        SimpleWaveData generatedWave = GenerateEndlessWave(6);

        Assert.IsNotNull(generatedWave);
        Assert.AreEqual(2, generatedWave.enemyGroups.Count);
        Assert.AreEqual(2, generatedWave.enemyGroups[0].enemyCount);
        Assert.AreEqual(1.5f, generatedWave.enemyGroups[0].spreadRadius, 0.0001f);
        Assert.AreEqual(0.25f, generatedWave.enemyGroups[0].spawnDelay, 0.0001f);
        Assert.AreEqual(7, generatedWave.enemyGroups[1].enemyCount);
        Assert.AreEqual(4.5f, generatedWave.enemyGroups[1].spreadRadius, 0.0001f);
        Assert.AreEqual(1.5f, generatedWave.enemyGroups[1].spawnDelay, 0.0001f);
    }

    [Test]
    public void GenerateEndlessWave_Wave44UsesBaseWave4WithoutEnemyBonus()
    {
        waveConfig.waves = new List<SimpleWaveData>();
        for (int i = 1; i <= 10; i++)
        {
            waveConfig.waves.Add(new SimpleWaveData
            {
                enemyGroups = new List<EnemyGroup>
                {
                    new EnemyGroup
                    {
                        enemyPoolType = PoolType.Enemy_Melee,
                        enemyCount = i,
                        spreadRadius = i
                    },
                    new EnemyGroup
                    {
                        enemyPoolType = PoolType.Enemy_Ranged,
                        enemyCount = i + 1,
                        spreadRadius = i + 0.5f
                    }
                }
            });
        }

        SimpleWaveData baseWave = waveConfig.waves[3];
        SimpleWaveData generatedWave = GenerateEndlessWave(44);

        Assert.IsNotNull(generatedWave);
        Assert.AreEqual(CountEnemies(baseWave), CountEnemies(generatedWave));
        Assert.LessOrEqual(CountEnemies(generatedWave), CountEnemies(baseWave));
        Assert.AreEqual(baseWave.enemyGroups[0].enemyCount, generatedWave.enemyGroups[0].enemyCount);
        Assert.AreEqual(baseWave.enemyGroups[1].enemyCount, generatedWave.enemyGroups[1].enemyCount);
        Assert.AreEqual(baseWave.enemyGroups[0].spreadRadius, generatedWave.enemyGroups[0].spreadRadius, 0.0001f);
        Assert.AreEqual(baseWave.enemyGroups[1].spreadRadius, generatedWave.enemyGroups[1].spreadRadius, 0.0001f);
    }

    [Test]
    public void JumpToWave_ReturnsFalse_ForInvalidTarget()
    {
        Assert.IsFalse(spawner.JumpToWave(0));
    }

    [Test]
    public void JumpToWave_SetsRequestedWaveImmediately()
    {
        bool result = spawner.JumpToWave(2);

        Assert.IsTrue(result);
        Assert.AreEqual(2, spawner.GetCurrentWave());
    }

    [Test]
    public void JumpToWave_InvalidatesPreviousWaveSession()
    {
        int beforeJump = spawner.GetWaveSessionId();

        spawner.JumpToWave(1);

        int afterJump = spawner.GetWaveSessionId();
        Assert.AreNotEqual(beforeJump, afterJump);
        Assert.IsFalse(spawner.IsWaveSessionCurrent(beforeJump));
        Assert.IsTrue(spawner.IsWaveSessionCurrent(afterJump));
    }

    [Test]
    public void UtilsFormatWholeNumber_RoundsUpForDisplay()
    {
        Assert.AreEqual("13", Utils.FormatWholeNumber(12.6f));
        Assert.AreEqual("100", Utils.FormatWholeNumber(99.4f));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(fieldInfo, $"Could not find field '{fieldName}'.");
        fieldInfo.SetValue(target, value);
    }

    private SimpleWaveData GenerateEndlessWave(int waveNumber)
    {
        MethodInfo methodInfo = typeof(WaveSpawner).GetMethod("GenerateEndlessWave", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(methodInfo, "Could not find GenerateEndlessWave.");
        return methodInfo.Invoke(spawner, new object[] { waveNumber }) as SimpleWaveData;
    }

    private static int CountEnemies(SimpleWaveData wave)
    {
        int count = 0;
        foreach (EnemyGroup group in wave.enemyGroups)
        {
            count += group.enemyCount;
        }

        return count;
    }

    private static void ResetSingletonInstance()
    {
        FieldInfo fieldInfo = typeof(Singleton<WaveSpawner>).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        fieldInfo?.SetValue(null, null);
    }
}
