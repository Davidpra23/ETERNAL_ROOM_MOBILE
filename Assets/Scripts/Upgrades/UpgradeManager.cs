using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private UpgradeUI upgradeUI;

    public List<Upgrade> allUpgrades;
    public int upgradesPerChoice = 3;

    public void ShowUpgradeChoices()
    {
        var player = GameObject.FindWithTag("Player");
        if (!player) return;

        var chosen = allUpgrades
            .Where(u => u != null)
            .OrderBy(_ => Random.value)
            .Take(upgradesPerChoice)
            .ToList();

        upgradeUI.Show(chosen, ApplyUpgrade);
    }

    public void ApplyUpgrade(Upgrade upgrade)
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null && upgrade != null)
        {
            upgrade.Apply(player);
        }
    }
}