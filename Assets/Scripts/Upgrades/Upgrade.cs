using UnityEngine;

public abstract class Upgrade : ScriptableObject
{
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Rarity")]
    public Rarity rarity = Rarity.Common;

    public abstract void Apply(GameObject player);
}
