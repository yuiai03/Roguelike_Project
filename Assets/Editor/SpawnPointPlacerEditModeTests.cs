using System.Collections.Generic;
using NUnit.Framework;

public class SpawnPointPlacerEditModeTests
{
    [Test]
    public void BuildRandomAllowedTypes_OnlyRandomBoss_ReturnsBossTrio()
    {
        List<PoolType> allowed = SpawnPointPlacer.BuildRandomAllowedTypes(false, false, false, true);

        CollectionAssert.AreEqual(
            new[]
            {
                PoolType.Boss_Geo,
                PoolType.Boss_Pyro,
                PoolType.Boss_Electro
            },
            allowed);
    }

    [Test]
    public void BuildRandomAllowedTypes_MixedNormalAndBoss_ReturnsEnabledTypesOnly()
    {
        List<PoolType> allowed = SpawnPointPlacer.BuildRandomAllowedTypes(false, true, false, true);

        CollectionAssert.AreEqual(
            new[]
            {
                PoolType.Enemy_Ranged,
                PoolType.Boss_Geo,
                PoolType.Boss_Pyro,
                PoolType.Boss_Electro
            },
            allowed);
    }

    [Test]
    public void BuildRandomAllowedTypes_AllOptionsDisabled_ReturnsEmptyCandidateList()
    {
        List<PoolType> allowed = SpawnPointPlacer.BuildRandomAllowedTypes(false, false, false, false);

        Assert.AreEqual(0, allowed.Count);
    }

    [Test]
    public void PickRandomAllowedType_AllOptionsDisabled_FallsBackToMelee()
    {
        PoolType selected = SpawnPointPlacer.PickRandomAllowedType(false, false, false, false);

        Assert.AreEqual(PoolType.Enemy_Melee, selected);
    }
}
