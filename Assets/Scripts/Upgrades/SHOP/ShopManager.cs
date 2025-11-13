using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private List<ShopItem> allShopItems;
    [SerializeField] private int itemsPerVisit = 3;
    [SerializeField] private RarityTable rarityTable;

    private GameObject player;
    private readonly HashSet<int> shopWaves = new HashSet<int> { 2, 5, 10, 15, 19 };

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public bool ShouldShowShopThisWave()
    {
        if (WaveManager.Instance == null)
            return false;

        // ✅ Tienda solo tras estas oleadas completadas
        int wave = WaveManager.Instance.GetCurrentWave();
        return wave == 2 || wave == 5 || wave == 10 || wave == 15 || wave == 19;
    }






    public void ShowShop()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (shopUI == null)
        {
            Debug.LogError("[ShopManager] Falta ShopUI");
            return;
        }

        // ✅ Pausar el juego
        Time.timeScale = 0f;

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

        // ❌ No cerramos la tienda automáticamente (botón lo hace)
    }

    private void OnShopClosed()
    {
        // ✅ Reanudar juego al cerrar tienda
        Time.timeScale = 1f;
        // No llamamos PrepareNextWave aquí (tu botón lo hace)
    }
}
