using UnityEngine;

public static class Utils
{
    #region Rarity Helpers

    /// <summary>
    /// Lấy màu dựa trên rarity type
    /// </summary>
    public static Color GetRarityColor(RarityType rarity)
    {
        return rarity switch
        {
            RarityType.Common => new Color(0.8f, 0.8f, 0.8f),      // Xám
            RarityType.Rare => new Color(0.2f, 0.5f, 1f),          // Xanh dương
            RarityType.Epic => new Color(0.6f, 0.2f, 1f),          // Tím
            RarityType.Legendary => new Color(1f, 0.6f, 0f),       // Vàng cam
            _ => Color.white
        };
    }

    /// <summary>
    /// Lấy tên rarity
    /// </summary>
    public static string GetRarityName(RarityType rarity)
    {
        return rarity.ToString();
    }

    /// <summary>
    /// Lấy weight cho random selection (rarity cao = weight thấp = hiếm hơn)
    /// </summary>
    public static float GetRarityWeight(RarityType rarity)
    {
        return 5f - (int)rarity;
    }

    #endregion
}
