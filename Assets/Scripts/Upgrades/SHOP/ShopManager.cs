using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private List<ShopItem> allShopItems;
    [SerializeField] private int itemsPerVisit = 3;
    [SerializeField] private bool closeAfterFirstPurchase = true;

    private GameObject player;

    private void Start() // <- mejor Start que Awake para referencias de escena
    {
        if (!Application.isPlaying) return;
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public void ShowShop()
    {
        if (!Application.isPlaying) return; // <- clave: no abrir tienda en editor

        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (shopUI == null) { Debug.LogError("[ShopManager] Falta ShopUI"); return; }

        var selection = allShopItems
            .Where(i => i != null)
            .OrderBy(_ => Random.value)
            .Take(itemsPerVisit)
            .ToList();

        // Difíerelo un frame: evita choques con carga/inspector/TMP
        StartCoroutine(ShowDeferred(selection));
    }

    private IEnumerator ShowDeferred(List<ShopItem> selection)
    {
        yield return null; // espera un frame para asegurar main thread/UI ok
        shopUI.Show(selection, OnBuyItem, OnShopClosed);
    }

    private void OnBuyItem(ShopItem item)
    {
        if (!Application.isPlaying) return;

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
            shopUI.Close(); // disparará OnShopClosed
        }
    }

    private void OnShopClosed()
    {
        if (!Application.isPlaying) return;
        WaveManager.Instance?.PrepareNextWave();
    }
}
