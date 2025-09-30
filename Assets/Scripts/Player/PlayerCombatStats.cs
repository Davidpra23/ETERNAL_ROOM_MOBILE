using UnityEngine;

public class PlayerCombatStats : MonoBehaviour
{
    [Header("Daño")]
    public float flatDamage = 0f;              // +X daño plano
    public float damageMultiplier = 1f;        // × daño (1 = 100%)

    [Header("Velocidad de Ataque")]
    public float attackSpeedMultiplier = 1f;   // × velocidad (afecta cooldown: más alto = más rápido)

    [Header("Rango / Área")]
    public float rangeMultiplier = 1f;         // × rango

    [Header("Crítico")]
    public float critChance = 0f;              // 0..1
    public float critMultiplier = 2f;          // × daño en crítico

    [Header("Otros")]
    public int extraProjectiles = 0;           // para armas de proyectiles
    public float knockbackMultiplier = 1f;

    // Helpers convenientes
    public float ComputeDamage(float weaponBaseDamage)
    {
        float baseScaled = weaponBaseDamage * Mathf.Max(0f, damageMultiplier);
        float final = baseScaled + flatDamage;
        return Mathf.Max(0f, final);
    }

    public float ComputeCooldown(float weaponBaseCooldown)
    {
        float speed = Mathf.Max(0.01f, attackSpeedMultiplier); // evitas /0
        return weaponBaseCooldown / speed;
    }

    public float ComputeRange(float weaponBaseRange)
    {
        return weaponBaseRange * Mathf.Max(0.01f, rangeMultiplier);
    }

    public bool RollCrit(out float critMult)
    {
        critMult = 1f;
        if (critChance <= 0f) return false;
        if (Random.value <= critChance)
        {
            critMult = Mathf.Max(1f, critMultiplier);
            return true;
        }
        return false;
    }

    // Métodos para upgrades/tienda
    public void AddFlatDamage(float v) => flatDamage += v;
    public void AddDamageMultiplier(float v) => damageMultiplier += v; // v=0.2 => +20% daño
    public void AddAttackSpeedMultiplier(float v) => attackSpeedMultiplier += v; // v=0.2 => +20% vel
    public void AddRangeMultiplier(float v) => rangeMultiplier += v;
    public void AddCritChance(float v) => critChance += v;
    public void AddCritMultiplier(float v) => critMultiplier += v;
    public void AddExtraProjectiles(int v) => extraProjectiles += v;
    public void AddKnockbackMultiplier(float v) => knockbackMultiplier += v;
}
