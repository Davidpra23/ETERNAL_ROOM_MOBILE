using UnityEngine;


[CreateAssetMenu(menuName = "Upgrades/Increase Max Health")]
public class IncreaseMaxHealthUpgrade : Upgrade
{
public int amount;
public override void Apply(GameObject player)
{
player.GetComponent<PlayerHealth>()?.IncreaseMaxHealth(amount);
}
}