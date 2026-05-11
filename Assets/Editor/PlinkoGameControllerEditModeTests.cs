using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class PlinkoGameControllerEditModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float PhysicsStep = 1f / 60f;

    private GameObject rootObject;
    private PlinkoGameController controller;

    [SetUp]
    public void SetUp()
    {
        rootObject = new GameObject("PlinkoRoot");
        controller = rootObject.AddComponent<PlinkoGameController>();
        controller.BuildBoard();
    }

    [TearDown]
    public void TearDown()
    {
        DestroyAllRuntimeObjects();

        if (rootObject != null)
        {
            Object.DestroyImmediate(rootObject);
        }

        Physics2D.SyncTransforms();
    }

    [Test]
    public void BuildBoard_CreatesExpectedHierarchy()
    {
        Assert.IsNotNull(rootObject.transform.Find("SpawnPoint"));
        Assert.IsNotNull(rootObject.transform.Find("Pins"));
        Assert.IsNotNull(rootObject.transform.Find("Slots"));
        Assert.IsNotNull(rootObject.transform.Find("BoardBounds"));
        Assert.IsNotNull(rootObject.transform.Find("KillZone"));
        Assert.IsNotNull(rootObject.transform.Find("Funnels"));
    }

    [Test]
    public void BuildBoard_CreatesSeventyFivePins_AndThirteenSlots()
    {
        Assert.AreEqual(75, rootObject.transform.Find("Pins").childCount);
        Assert.AreEqual(13, rootObject.transform.Find("Slots").childCount);
    }

    [Test]
    public void BuildBoard_AssignsAscendingSlotValues()
    {
        Transform slots = rootObject.transform.Find("Slots");

        for (int slotIndex = 0; slotIndex < slots.childCount; slotIndex++)
        {
            PlinkoSlot slot = slots.GetChild(slotIndex).GetComponent<PlinkoSlot>();
            Assert.IsNotNull(slot);
            Assert.AreEqual(slotIndex + 1, slot.RewardValue);
        }
    }

    [Test]
    public void BuildBoard_ConfiguresKillZoneAsTrigger()
    {
        GameObject killZone = rootObject.transform.Find("KillZone").gameObject;
        BoxCollider2D collider = killZone.GetComponent<BoxCollider2D>();

        Assert.IsNotNull(killZone.GetComponent<PlinkoKillZone>());
        Assert.IsNotNull(collider);
        Assert.IsTrue(collider.isTrigger);
    }

    [Test]
    public void BuildBoard_ConfiguresBallWithBounceMaterial()
    {
        Transform ballTemplate = rootObject.transform.Find("Templates/BallTemplate");
        CircleCollider2D collider = ballTemplate.GetComponent<CircleCollider2D>();

        Assert.IsNotNull(collider);
        Assert.IsNotNull(collider.sharedMaterial);
        Assert.Greater(collider.sharedMaterial.bounciness, 0f);
        Assert.Less(collider.sharedMaterial.friction, 0.2f);
    }

    [Test]
    public void BuildBoard_ConfiguresBallToNeverSleep()
    {
        Transform ballTemplate = rootObject.transform.Find("Templates/BallTemplate");
        Rigidbody2D rigidbody2D = ballTemplate.GetComponent<Rigidbody2D>();

        Assert.IsNotNull(rigidbody2D);
        Assert.AreEqual(RigidbodySleepMode2D.NeverSleep, rigidbody2D.sleepMode);
    }

    [Test]
    public void BuildBoard_ConfiguresPinsWithSlipperyMaterial()
    {
        Transform pinTemplate = rootObject.transform.Find("Templates/PinTemplate");
        CircleCollider2D collider = pinTemplate.GetComponent<CircleCollider2D>();

        Assert.IsNotNull(collider);
        Assert.IsNotNull(collider.sharedMaterial);
        Assert.AreEqual(0f, collider.sharedMaterial.friction, 0.0001f);
    }

    [Test]
    public void BuildBoard_PreservesSpawnPointLocalX()
    {
        Transform spawnPoint = rootObject.transform.Find("SpawnPoint");
        spawnPoint.localPosition = new Vector3(0.5f, spawnPoint.localPosition.y, spawnPoint.localPosition.z);

        controller.BuildBoard();

        Assert.AreEqual(0.5f, spawnPoint.localPosition.x, 0.0001f);
    }

    [Test]
    public void SpawnBall_FallbackUsesSpawnPointWorldPosition_WhenRootIsMoved()
    {
        rootObject.transform.position = new Vector3(3f, 0f, 0f);
        Transform spawnPoint = rootObject.transform.Find("SpawnPoint");
        spawnPoint.localPosition = new Vector3(0.5f, spawnPoint.localPosition.y, spawnPoint.localPosition.z);
        Vector3 expectedSpawnPosition = spawnPoint.position;

        InvokeFallbackSpawn(controller);

        PlinkoBall spawnedBall = FindSingleBall();
        Assert.AreEqual(rootObject.transform, spawnedBall.transform.parent);
        Assert.AreEqual(expectedSpawnPosition.x, spawnedBall.transform.position.x, 0.0001f);
        Assert.AreEqual(expectedSpawnPosition.y, spawnedBall.transform.position.y, 0.0001f);
    }

    [TestCase(-1.5f)]
    [TestCase(-0.5f)]
    [TestCase(0f)]
    [TestCase(0.5f)]
    [TestCase(1.5f)]
    public void SpawnBall_FallbackLocksSameRewardForCommonSpawnPositions(float spawnLocalX)
    {
        int rewardAtOrigin = SpawnFallbackBallAndGetLockedReward(0f, spawnLocalX);
        int rewardAtOffset = SpawnFallbackBallAndGetLockedReward(3f, spawnLocalX);

        Assert.AreEqual(rewardAtOrigin, rewardAtOffset);
    }

    [Test]
    public void ResolveSlotIndexFromLocalX_ClampsOutsideBoard()
    {
        Transform slots = rootObject.transform.Find("Slots");
        float leftOutsideX = slots.GetChild(0).localPosition.x - 20f;
        float rightOutsideX = slots.GetChild(slots.childCount - 1).localPosition.x + 20f;

        Assert.AreEqual(0, controller.ResolveSlotIndexFromLocalX(leftOutsideX));
        Assert.AreEqual(slots.childCount - 1, controller.ResolveSlotIndexFromLocalX(rightOutsideX));
    }

    [Test]
    public void ResolveSlotIndexFromLocalX_PicksLeftSlot_WhenSpawnIsAtMidpoint()
    {
        Transform slots = rootObject.transform.Find("Slots");
        Transform leftSlot = slots.GetChild(5);
        Transform rightSlot = slots.GetChild(6);
        float midpoint = (leftSlot.localPosition.x + rightSlot.localPosition.x) * 0.5f;

        Assert.AreEqual(5, controller.ResolveSlotIndexFromLocalX(midpoint));
    }

    [TestCase(0)]
    [TestCase(6)]
    [TestCase(12)]
    public void SpawnBallToSlotIndex_LocksSameReward_WhenRootIsTranslated(int slotIndex)
    {
        int rewardAtOrigin = SpawnBallToSlotAndGetLockedReward(0f, slotIndex);
        int rewardAtOffset = SpawnBallToSlotAndGetLockedReward(3f, slotIndex);

        Assert.AreEqual(rewardAtOrigin, rewardAtOffset);
    }

    [Test]
    public void SpawnBallToSlotIndex_ClampsRequestedSlotIndex()
    {
        controller.SpawnBallToSlotIndex(-99);
        PlinkoBall leftBall = FindSingleBall();
        Assert.AreEqual(0, leftBall.LockedSlotIndex);
        DestroyAllRuntimeObjects();

        controller.SpawnBallToSlotIndex(999);
        PlinkoBall rightBall = FindSingleBall();
        Assert.AreEqual(controller.SlotCount - 1, rightBall.LockedSlotIndex);
    }

    [Test]
    public void SpawnBallToSlotIndex_CreatesOneFunnelAroundLockedSlot()
    {
        controller.SpawnBallToSlotIndex(6);

        PlinkoBall ball = FindSingleBall();
        PlinkoFunnel funnel = ball.OwnedFunnel;
        Rigidbody2D rigidbody2D = ball.GetComponent<Rigidbody2D>();
        CircleCollider2D collider = ball.GetComponent<CircleCollider2D>();

        Assert.IsNotNull(funnel);
        Assert.AreEqual(1, rootObject.transform.Find("Funnels").childCount);
        Assert.AreEqual(ball, funnel.OwnerBall);
        Assert.AreEqual(ball.LockedSlot, funnel.LockedSlot);
        Assert.AreEqual(ball.LockedSlot.localPosition.x, funnel.SlotCenterLocal.x, 0.0001f);
        Assert.AreEqual(ball.LockedSlot.localPosition.y, funnel.SlotCenterLocal.y, 0.0001f);
        Assert.AreEqual(4, funnel.WallColliders.Count);
        Assert.IsTrue(rigidbody2D.simulated);
        Assert.IsTrue(collider.enabled);
    }

    [Test]
    public void SpawnBallToSlotIndex_LeavesBottomPegCollidersActive()
    {
        controller.SpawnBallToSlotIndex(6);

        int checkedPinCount = 0;
        foreach (Transform pin in rootObject.transform.Find("Pins"))
        {
            if (!pin.name.StartsWith("Pin_8_") &&
                !pin.name.StartsWith("Pin_9_") &&
                !pin.name.StartsWith("Pin_10_"))
            {
                continue;
            }

            CircleCollider2D collider = pin.GetComponent<CircleCollider2D>();
            Assert.IsNotNull(collider);
            Assert.IsTrue(collider.enabled);
            checkedPinCount++;
        }

        Assert.Greater(checkedPinCount, 0);
    }

    [Test]
    public void MultipleBalls_GetSeparateFunnels_AndIgnoreEachOthersFunnels()
    {
        controller.SpawnBallToSlotIndex(2);
        controller.SpawnBallToSlotIndex(10);

        PlinkoBall[] balls = Object.FindObjectsByType<PlinkoBall>(FindObjectsSortMode.None);
        Assert.AreEqual(2, balls.Length);

        PlinkoBall leftBall = FindBallBySlotIndex(balls, 2);
        PlinkoBall rightBall = FindBallBySlotIndex(balls, 10);
        Assert.IsNotNull(leftBall);
        Assert.IsNotNull(rightBall);
        Assert.AreEqual(2, rootObject.transform.Find("Funnels").childCount);
        Assert.IsNotNull(leftBall.OwnedFunnel);
        Assert.IsNotNull(rightBall.OwnedFunnel);
        Assert.AreNotSame(leftBall.OwnedFunnel, rightBall.OwnedFunnel);

        AssertFunnelCollisionState(leftBall, leftBall.OwnedFunnel, false);
        AssertFunnelCollisionState(rightBall, rightBall.OwnedFunnel, false);
        AssertFunnelCollisionState(leftBall, rightBall.OwnedFunnel, true);
        AssertFunnelCollisionState(rightBall, leftBall.OwnedFunnel, true);
    }

    [Test]
    public void WrongSlotTrigger_DoesNotResolveLockedReward()
    {
        controller.SpawnBallToSlotIndex(6);

        PlinkoBall ball = FindSingleBall();
        Transform slots = rootObject.transform.Find("Slots");
        PlinkoSlot wrongSlot = FindSlotDifferentFrom(slots, ball.LockedSlot);

        Assert.IsNotNull(wrongSlot);
        InvokeSlotTrigger(wrongSlot, ball.CachedCollider);

        Assert.AreEqual(-1, controller.LastResolvedRewardValue);
        Assert.IsFalse(ball == null);
    }

    [Test]
    public void CorrectSlotTrigger_ResolvesLockedReward()
    {
        controller.SpawnBallToSlotIndex(6);

        PlinkoBall ball = FindSingleBall();
        int expectedReward = ball.LockedRewardValue;

        InvokeSlotTrigger(ball.LockedSlot.GetComponent<PlinkoSlot>(), ball.CachedCollider);

        Assert.AreEqual(expectedReward, controller.LastResolvedRewardValue);
        Assert.IsTrue(ball == null);
    }

    [Test]
    public void KillZoneTrigger_RemovesUnresolvedBallWithoutScoring()
    {
        controller.SpawnBallToSlotIndex(6);

        PlinkoBall ball = FindSingleBall();
        PlinkoKillZone killZone = rootObject.transform.Find("KillZone").GetComponent<PlinkoKillZone>();
        InvokeKillZoneTrigger(killZone, ball.CachedCollider);

        Assert.AreEqual(-1, controller.LastResolvedRewardValue);
        Assert.IsTrue(ball == null);
    }

    [Test]
    public void SpawnBallToSlotIndex_CanResolveSameRewardAfterRootMovesMidFall()
    {
        GameObject simulationRoot = CreateSimulationRoot(0f, out PlinkoGameController simulationController);

        try
        {
            simulationController.SpawnBallToSlotIndex(6);
            PlinkoBall ball = FindSingleBall();
            int expectedReward = ball.LockedRewardValue;

            SimulatePhysicsSteps(30);
            simulationRoot.transform.position = new Vector3(3f, 0f, 0f);
            SimulatePhysicsSteps(30);

            InvokeSlotTrigger(ball.LockedSlot.GetComponent<PlinkoSlot>(), ball.CachedCollider);

            Assert.AreEqual(expectedReward, simulationController.LastResolvedRewardValue);
        }
        finally
        {
            DestroyAllRuntimeObjects();
            Object.DestroyImmediate(simulationRoot);
            Physics2D.SyncTransforms();
        }
    }

    private int SpawnFallbackBallAndGetLockedReward(float rootX, float spawnLocalX)
    {
        GameObject simulationRoot = CreateSimulationRoot(rootX, out PlinkoGameController simulationController);

        try
        {
            Transform spawnPoint = simulationRoot.transform.Find("SpawnPoint");
            spawnPoint.localPosition = new Vector3(spawnLocalX, spawnPoint.localPosition.y, spawnPoint.localPosition.z);

            InvokeFallbackSpawn(simulationController);
            return FindSingleBall().LockedRewardValue;
        }
        finally
        {
            DestroyAllRuntimeObjects();
            Object.DestroyImmediate(simulationRoot);
            Physics2D.SyncTransforms();
        }
    }

    private int SpawnBallToSlotAndGetLockedReward(float rootX, int slotIndex)
    {
        GameObject simulationRoot = CreateSimulationRoot(rootX, out PlinkoGameController simulationController);

        try
        {
            simulationController.SpawnBallToSlotIndex(slotIndex);
            return FindSingleBall().LockedRewardValue;
        }
        finally
        {
            DestroyAllRuntimeObjects();
            Object.DestroyImmediate(simulationRoot);
            Physics2D.SyncTransforms();
        }
    }

    private GameObject CreateSimulationRoot(float rootX, out PlinkoGameController simulationController)
    {
        GameObject simulationRoot = new GameObject("PlinkoRoot_Simulation");
        simulationController = simulationRoot.AddComponent<PlinkoGameController>();
        simulationRoot.transform.position = new Vector3(rootX, 0f, 0f);
        simulationController.BuildBoard();
        return simulationRoot;
    }

    private void InvokeFallbackSpawn(PlinkoGameController targetController)
    {
        typeof(PlinkoGameController)
            .GetMethod("SpawnBall", PrivateInstance)
            .Invoke(targetController, null);
    }

    private void SimulatePhysicsSteps(int stepCount)
    {
        SimulationMode2D previousSimulationMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;

        try
        {
            for (int step = 0; step < stepCount; step++)
            {
                Physics2D.Simulate(PhysicsStep);
            }
        }
        finally
        {
            Physics2D.simulationMode = previousSimulationMode;
        }
    }

    private void InvokeSlotTrigger(PlinkoSlot slot, CircleCollider2D ballCollider)
    {
        typeof(PlinkoSlot)
            .GetMethod("OnTriggerEnter2D", PrivateInstance)
            .Invoke(slot, new object[] { ballCollider });
    }

    private void InvokeKillZoneTrigger(PlinkoKillZone killZone, CircleCollider2D ballCollider)
    {
        typeof(PlinkoKillZone)
            .GetMethod("OnTriggerEnter2D", PrivateInstance)
            .Invoke(killZone, new object[] { ballCollider });
    }

    private void AssertFunnelCollisionState(PlinkoBall ball, PlinkoFunnel funnel, bool expectedIgnored)
    {
        foreach (BoxCollider2D wall in funnel.WallColliders)
        {
            Assert.AreEqual(expectedIgnored, Physics2D.GetIgnoreCollision(ball.CachedCollider, wall));
        }
    }

    private PlinkoBall FindSingleBall()
    {
        PlinkoBall[] balls = Object.FindObjectsByType<PlinkoBall>(FindObjectsSortMode.None);
        Assert.AreEqual(1, balls.Length);
        return balls[0];
    }

    private PlinkoBall FindBallBySlotIndex(IEnumerable<PlinkoBall> balls, int slotIndex)
    {
        foreach (PlinkoBall ball in balls)
        {
            if (ball.LockedSlotIndex == slotIndex)
            {
                return ball;
            }
        }

        return null;
    }

    private PlinkoSlot FindSlotDifferentFrom(Transform slotsRoot, Transform targetSlot)
    {
        for (int slotIndex = 0; slotIndex < slotsRoot.childCount; slotIndex++)
        {
            Transform slotTransform = slotsRoot.GetChild(slotIndex);
            if (slotTransform == targetSlot)
            {
                continue;
            }

            return slotTransform.GetComponent<PlinkoSlot>();
        }

        return null;
    }

    private void DestroyAllRuntimeObjects()
    {
        foreach (PlinkoBall ball in Object.FindObjectsByType<PlinkoBall>(FindObjectsSortMode.None))
        {
            if (ball != null)
            {
                Object.DestroyImmediate(ball.gameObject);
            }
        }

        foreach (PlinkoFunnel funnel in Object.FindObjectsByType<PlinkoFunnel>(FindObjectsSortMode.None))
        {
            if (funnel != null)
            {
                Object.DestroyImmediate(funnel.gameObject);
            }
        }
    }
}

public class PlinkoBallEditModeTests
{
    private GameObject firstBallObject;
    private GameObject secondBallObject;
    private GameObject controllerObject;

    [TearDown]
    public void TearDown()
    {
        if (firstBallObject != null)
        {
            Object.DestroyImmediate(firstBallObject);
        }

        if (secondBallObject != null)
        {
            Object.DestroyImmediate(secondBallObject);
        }

        if (controllerObject != null)
        {
            Object.DestroyImmediate(controllerObject);
        }
    }

    [Test]
    public void Initialize_UsesSameImpulseForSameSpawnPosition()
    {
        PlinkoBall firstBall = CreateBall("FirstBall", out Rigidbody2D firstRigidbody, out _);
        PlinkoBall secondBall = CreateBall("SecondBall", out Rigidbody2D secondRigidbody, out _);

        firstBall.Initialize();
        secondBall.Initialize();

        Assert.AreEqual(firstRigidbody.linearVelocity.x, secondRigidbody.linearVelocity.x, 0.0001f);
        Assert.AreEqual(firstRigidbody.linearVelocity.y, secondRigidbody.linearVelocity.y, 0.0001f);
    }

    [Test]
    public void Initialize_IgnoresCollisionBetweenBalls()
    {
        PlinkoBall firstBall = CreateBall("FirstBall", out _, out CircleCollider2D firstCollider);
        PlinkoBall secondBall = CreateBall("SecondBall", out _, out CircleCollider2D secondCollider);

        firstBall.Initialize();
        secondBall.Initialize();

        Assert.IsTrue(Physics2D.GetIgnoreCollision(firstCollider, secondCollider));
    }

    [Test]
    public void InitializeLocked_StoresLockedMetadataAndKeepsPhysicsEnabled()
    {
        controllerObject = new GameObject("Controller");
        GameObject slotObject = new GameObject("TargetSlot");
        slotObject.transform.SetParent(controllerObject.transform, false);
        PlinkoGameController owner = controllerObject.AddComponent<PlinkoGameController>();
        PlinkoBall ball = CreateBall("LockedBall", out Rigidbody2D rigidbody2D, out CircleCollider2D collider);

        ball.InitializeLocked(owner, Vector3.zero, 5, slotObject.transform, 7);

        Assert.AreEqual(7, ball.LockedRewardValue);
        Assert.AreEqual(5, ball.LockedSlotIndex);
        Assert.AreEqual(slotObject.transform, ball.LockedSlot);
        Assert.IsTrue(rigidbody2D.simulated);
        Assert.IsTrue(collider.enabled);
    }

    private PlinkoBall CreateBall(string objectName, out Rigidbody2D rigidbody2D, out CircleCollider2D collider)
    {
        GameObject ballObject = new GameObject(objectName);
        if (firstBallObject == null)
        {
            firstBallObject = ballObject;
        }
        else
        {
            secondBallObject = ballObject;
        }

        rigidbody2D = ballObject.AddComponent<Rigidbody2D>();
        collider = ballObject.AddComponent<CircleCollider2D>();
        return ballObject.AddComponent<PlinkoBall>();
    }
}
