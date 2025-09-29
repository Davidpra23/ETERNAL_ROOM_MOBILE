using UnityEngine;


[CreateAssetMenu(menuName = "Upgrades/Heal On Wave Start")]
public class HealOnWaveStartUpgrade : Upgrade
{
public int healAmount;
public override void Apply(GameObject player)
{
player.GetComponent<PlayerHealth>()?.Heal(healAmount);
}
}