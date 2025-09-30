using UnityEngine;

public abstract class ShopItem : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public int cost;

    /// <summary>
    /// Aplica el efecto de la compra al player.
    /// Retorna true si se aplicó con éxito.
    /// </summary>
    public abstract bool ApplyTo(GameObject player);
}
