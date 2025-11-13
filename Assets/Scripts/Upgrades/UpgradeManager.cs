using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private UpgradeUI upgradeUI;
    [SerializeField] private RarityTable rarityTable; // ASIGNA en el inspector

    public List<Upgrade> allUpgrades;
    public int upgradesPerChoice = 3;

    public void ShowUpgradeChoices()
    {
        var player = GameObject.FindWithTag("Player");
        if (!player) return;

        int wave = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWave() : 1;
        var weights = rarityTable != null
            ? rarityTable.GetWeights(wave)
            : new Dictionary<Rarity, float>
            {
                { Rarity.Common, 0.7f },
                { Rarity.Rare, 0.2f },
                { Rarity.Epic, 0.09f },
                { Rarity.Legendary, 0.01f }
            };

        var pool = allUpgrades?.Where(u => u != null).ToList() ?? new List<Upgrade>();
        var chosen = WeightedPicker.PickManyDistinct(pool, upgradesPerChoice, u => u.rarity, weights);

        upgradeUI.Show(chosen, ApplyUpgrade);
    }

    public void ApplyUpgrade(Upgrade upgrade)
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null && upgrade != null)
            upgrade.Apply(player);

        // 👇 Ya no toca tienda aquí, la maneja UpgradeUI
    }
}
