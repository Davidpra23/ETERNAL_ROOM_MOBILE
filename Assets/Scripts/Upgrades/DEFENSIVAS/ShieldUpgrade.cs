using UnityEngine;


[CreateAssetMenu(menuName = "Upgrades/Shield On Wave Start")]
public class ShieldUpgrade : Upgrade
{
public override void Apply(GameObject player)
{
var shield = player.GetComponent<PlayerShield>();
if (shield != null) shield.ActivateShield();
}
}