using UnityEngine;

public static class RarityUtil
{
    // Colores sugeridos (puedes cambiarlos desde aquí)
    public static Color GetColor(Rarity r) => r switch
    {
        Rarity.Common    => new Color32(77, 77, 77, 77), 
        Rarity.Rare      => new Color32(30, 144, 255, 255),  // #1E90FF
        Rarity.Epic      => new Color32(186, 85, 211, 255),  // #BA55D3
        Rarity.Legendary => new Color32(255, 215, 0, 255),   // #FFD700
        _ => Color.white
    };

    public static string GetHex(Rarity r)
    {
        var c = GetColor(r);
        return ColorUtility.ToHtmlStringRGB(c); // ej: "FFD700"
    }

    public static string WrapWithColorTag(string text, Rarity r)
        => $"<color=#{GetHex(r)}>{text}</color>";
}
