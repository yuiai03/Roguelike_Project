using UnityEngine;

public static class Utils
{
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

    public static float GetRarityWeight(RarityType rarity)
    {
        return 5f - (int)rarity;
    }

    #endregion
}
