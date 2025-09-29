using UnityEngine;


[CreateAssetMenu(menuName = "Upgrades/Movement Speed Boost")]
public class SpeedBoostUpgrade : Upgrade
{
public float speedBonus;
public override void Apply(GameObject player)
{
player.GetComponent<PlayerMovement>()?.AddPermanentSpeedBonus(speedBonus);
}
}