using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Weapon Item")]
public class WeaponShopItem : ShopItem
{
    [Header("Si usas prefabs de armas")]
    public GameObject weaponPrefab;

    [Header("Si modificas PlayerAttack directamente")]
    public float damageBonus = 0f;
    public float attackSpeedBonus = 0f;
    public int projectilesBonus = 0;

    public override bool ApplyTo(GameObject player)
    {
        if (player == null) return false;

        Transform socket = player.transform.Find("UniRoot/Weapon");
        if (socket == null) socket = player.transform; // fallback

        // Destruir arma anterior
        for (int i = socket.childCount - 1; i >= 0; i--)
            GameObject.Destroy(socket.GetChild(i).gameObject);

        // Instanciar arma nueva
        if (weaponPrefab != null)
        {
            var weapon = GameObject.Instantiate(weaponPrefab, socket);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }

        // Bonos directos al “player” si quieres (además del prefab)
        var stats = player.GetComponent<PlayerCombatStats>();
        if (stats != null)
        {
            if (damageBonus != 0f) stats.AddFlatDamage(damageBonus);
            if (attackSpeedBonus != 0f) stats.AddAttackSpeedMultiplier(attackSpeedBonus);
            if (projectilesBonus != 0) stats.AddExtraProjectiles(projectilesBonus);
        }

        return true;
    }
}
