using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private List<ShopItem> allShopItems;
    [SerializeField] private int itemsPerVisit = 3;
    [SerializeField] private bool closeAfterFirstPurchase = true;
    [SerializeField] private RarityTable rarityTable; // ASIGNA

    private GameObject player;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public void ShowShop()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (shopUI == null) { Debug.LogError("[ShopManager] Falta ShopUI"); return; }

        int wave = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWave() : 1;
        var weights = rarityTable != null
            ? rarityTable.GetWeights(wave)
            : new Dictionary<Rarity, float> // fallback simple
            {
                { Rarity.Common, 0.7f },
                { Rarity.Rare, 0.2f },
                { Rarity.Epic, 0.09f },
                { Rarity.Legendary, 0.01f }
            };

        var pool = allShopItems?.Where(i => i != null).ToList() ?? new List<ShopItem>();
        var selection = WeightedPicker.PickManyDistinct(pool, itemsPerVisit, i => i.rarity, weights);

        shopUI.Show(selection, OnBuyItem, OnShopClosed);
    }

    private void OnBuyItem(ShopItem item)
    {
        if (item == null || player == null) return;
        if (ScoreManager.Instance == null) return;

        if (ScoreManager.Instance.CurrentScore < item.cost)
        {
            Debug.Log("[Shop] No tienes monedas suficientes.");
            return;
        }

        bool applied = item.ApplyTo(player);
        if (!applied) return;

        ScoreManager.Instance.AddScore(-item.cost);

        if (closeAfterFirstPurchase)
        {
            shopUI.Close();
        }
    }

    private void OnShopClosed()
    {
        WaveManager.Instance?.PrepareNextWave();
    }
}
