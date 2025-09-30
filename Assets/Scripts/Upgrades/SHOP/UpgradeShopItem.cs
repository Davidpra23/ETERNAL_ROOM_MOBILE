using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Upgrade Item")]
public class UpgradeShopItem : ShopItem
{
    public Upgrade upgrade; // una Upgrade ofensiva/arma que ya tengas como ScriptableObject

    public override bool ApplyTo(GameObject player)
    {
        if (upgrade == null) return false;
        upgrade.Apply(player);
        return true;
    }
}
