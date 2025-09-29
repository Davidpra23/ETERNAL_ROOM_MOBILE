using UnityEngine;


[CreateAssetMenu(menuName = "Upgrades/Passive Regeneration")]
public class PassiveRegenUpgrade : Upgrade
{
public int healPerTick = 1;
public float interval = 3f;


public override void Apply(GameObject player)
{
var regen = player.GetComponent<PlayerRegen>();
if (regen == null) regen = player.AddComponent<PlayerRegen>();
regen.StartRegen(healPerTick, interval);
}
}