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
    public void FormatWaveLabel_UsesEndlessSuffix_WhenWaveExceedsConfiguredCount()
    {
        Assert.AreEqual("Wave: 17 (Endless)", WaveSpawner.FormatWaveLabel(17, 10));
        Assert.AreEqual("Wave: 5/10", WaveSpawner.FormatWaveLabel(5, 10));
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
    public void UtilsFormatWholeNumber_RemovesDecimalPlaces()
    {
        Assert.AreEqual("13", Utils.FormatWholeNumber(12.6f));
        Assert.AreEqual("99", Utils.FormatWholeNumber(99.4f));
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(fieldInfo, $"Could not find field '{fieldName}'.");
        fieldInfo.SetValue(target, value);
    }

    private static void ResetSingletonInstance()
    {
        FieldInfo fieldInfo = typeof(Singleton<WaveSpawner>).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        fieldInfo?.SetValue(null, null);
    }
}
