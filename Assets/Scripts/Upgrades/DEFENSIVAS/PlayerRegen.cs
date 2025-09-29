using UnityEngine;


public class PlayerRegen : MonoBehaviour
{
private int healAmount;
private float interval;
private PlayerHealth health;


public void StartRegen(int amount, float tickInterval)
{
healAmount = amount;
interval = tickInterval;
health = GetComponent<PlayerHealth>();
CancelInvoke();
InvokeRepeating(nameof(DoRegen), interval, interval);
}


private void DoRegen()
{
if (health != null && health.CurrentHealth < health.MaxHealth)
health.Heal(healAmount);
}
}