using UnityEngine;

public static class Utils
{
    public static int RoundToDisplayInt(float value)
    {
        return Mathf.CeilToInt(value);
    }

    public static string FormatWholeNumber(float value)
    {
        return RoundToDisplayInt(value).ToString();
    }

    public static Vector3 GetGroundPosition(
        Vector3 worldPosition,
        LayerMask groundMask,
        float rayStartHeight = 5f,
        float raycastDistance = 50f)
    {
        Vector3 rayOrigin = worldPosition + Vector3.up * rayStartHeight;

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            raycastDistance,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return worldPosition;
    }

    #region Rarity Helpers

    public static Color GetRarityColor(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => new Color(0.8f, 0.8f, 0.8f),      
            RarityType.Rare => new Color(0.2f, 0.5f, 1f),          
            RarityType.Epic => new Color(0.6f, 0.2f, 1f),          
            RarityType.Legendary => new Color(1f, 0.6f, 0f),       
            _ => Color.white
        };
    }

    public static string GetRarityName(RarityType rarity)
    {
        return rarity.ToString();
    }

    public static float GetRarityWeight(RarityType rarity, float luckBonus = 0f)
    {
        float baseWeight = rarity switch
        {
            RarityType.Common => 100f,
            RarityType.Rare => 30f,
            RarityType.Epic => 10f,
            RarityType.Legendary => 2f,
            _ => 100f
        };

        if (rarity != RarityType.Common)
        {
            float bonusMultiplier = 1f + luckBonus;
            baseWeight *= bonusMultiplier;
        }

        return baseWeight;
    }

    #endregion
}
