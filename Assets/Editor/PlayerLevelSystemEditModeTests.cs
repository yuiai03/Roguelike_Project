using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

public class PlayerLevelSystemEditModeTests
{
    private GameObject levelSystemObject;
    private PlayerLevelSystem levelSystem;
    private int levelUpEventCount;

    [SetUp]
    public void SetUp()
    {
        ResetSingletonInstance();

        levelSystemObject = new GameObject("PlayerLevelSystemTestHost");
        levelSystem = levelSystemObject.AddComponent<PlayerLevelSystem>();

        if (levelSystem.OnLevelUp == null)
        {
            levelSystem.OnLevelUp = new UnityEvent<int>();
        }
        levelSystem.OnLevelUp.AddListener(HandleLevelUp);
    }

    [TearDown]
    public void TearDown()
    {
        if (levelSystem != null && levelSystem.OnLevelUp != null)
        {
            levelSystem.OnLevelUp.RemoveListener(HandleLevelUp);
        }

        if (levelSystemObject != null)
        {
            Object.DestroyImmediate(levelSystemObject);
        }

        ResetSingletonInstance();
        levelUpEventCount = 0;
    }

    [Test]
    public void InitialTotalExpRequiredForNextLevel_UsesStartingRequirement()
    {
        Assert.AreEqual(100f, levelSystem.GetTotalExpRequiredForNextLevel(), 0.0001f);
    }

    [Test]
    public void GrantLevels_LevelsOnce_UpdatesStateAndInvokesEvent()
    {
        levelSystem.GrantLevels(1);

        Assert.AreEqual(1, levelSystem.GetCurrentLevel());
        Assert.AreEqual(0f, levelSystem.GetCurrentExp(), 0.0001f);
        Assert.AreEqual(110f, levelSystem.GetExpToNextLevel(), 0.0001f);
        Assert.AreEqual(100f, levelSystem.GetTotalExpGained(), 0.0001f);
        Assert.AreEqual(210f, levelSystem.GetTotalExpRequiredForNextLevel(), 0.0001f);
        Assert.AreEqual(1, levelUpEventCount);
    }

    [Test]
    public void GrantLevels_LevelsFiveTimes_UpdatesScalingAndInvokesEventPerLevel()
    {
        levelSystem.GrantLevels(5);

        Assert.AreEqual(5, levelSystem.GetCurrentLevel());
        Assert.AreEqual(0f, levelSystem.GetCurrentExp(), 0.0001f);
        Assert.AreEqual(160f, levelSystem.GetExpToNextLevel(), 0.0001f);
        Assert.AreEqual(610f, levelSystem.GetTotalExpGained(), 0.0001f);
        Assert.AreEqual(770f, levelSystem.GetTotalExpRequiredForNextLevel(), 0.0001f);
        Assert.AreEqual(5, levelUpEventCount);
    }

    private void HandleLevelUp(int _)
    {
        levelUpEventCount++;
    }

    private static void ResetSingletonInstance()
    {
        FieldInfo fieldInfo = typeof(Singleton<PlayerLevelSystem>).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        fieldInfo?.SetValue(null, null);
    }
}
