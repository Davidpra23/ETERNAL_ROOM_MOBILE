using UnityEngine;

public abstract class ShopItem : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public int cost;

    [Header("Rarity")]
    public Rarity rarity = Rarity.Common;

    public abstract bool ApplyTo(GameObject player);
}
