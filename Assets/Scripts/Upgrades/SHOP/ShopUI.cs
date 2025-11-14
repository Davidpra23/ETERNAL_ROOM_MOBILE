using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject shopCardPrefab; // tarjeta por defecto (Icon, NameText, DescriptionText, PriceText, BuyButton)
    [Header("Card Prefabs por Rareza")]
    [Tooltip("Prefab para items Common. Si está vacío se usará el shopCardPrefab por defecto.")]
    [SerializeField] private GameObject commonCardPrefab;
    [Tooltip("Prefab para items Rare. Si está vacío se usará el shopCardPrefab por defecto.")]
    [SerializeField] private GameObject rareCardPrefab;
    [Tooltip("Prefab para items Epic. Si está vacío se usará el shopCardPrefab por defecto.")]
    [SerializeField] private GameObject epicCardPrefab;
    [Tooltip("Prefab para items Legendary. Si está vacío se usará el shopCardPrefab por defecto.")]
    [SerializeField] private GameObject legendaryCardPrefab;
    [SerializeField] private Button closeButton; // por si permites cerrar sin comprar

    private Action onShopClosed;
    private Action<ShopItem> onBuy;

    public void Show(List<ShopItem> items, Action<ShopItem> onBuyCallback, Action onClosed)
    {
        onShopClosed = onClosed;
        onBuy = onBuyCallback;

        panel.SetActive(true);

        foreach (Transform child in container)
            GameObject.Destroy(child.gameObject);

        foreach (var item in items)
        {
            if (item == null) continue;

            // Elegir prefab según rareza (si se asignó uno específico)
            GameObject prefabToUse = GetPrefabForRarity(item.rarity) ?? shopCardPrefab;
            if (prefabToUse == null)
            {
                Debug.LogError("[ShopUI] shopCardPrefab o el prefab por rareza no están asignados.");
                continue;
            }

            var go = GameObject.Instantiate(prefabToUse, container);

            var nameText = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = go.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var iconImage = go.transform.Find("Icon")?.GetComponent<Image>();
            var buyButton = go.transform.Find("BuyButton")?.GetComponent<Button>();
            // 🔹 Nuevo: PriceText ahora es hijo del BuyButton
            var priceText = go.transform.Find("BuyButton/PriceText")?.GetComponent<TextMeshProUGUI>();

            if (!nameText || !descText || !priceText || !iconImage || !buyButton)
            {
                Debug.LogError("[ShopUI] Prefab incompleto (NameText, DescriptionText, PriceText, Icon, BuyButton)");
                GameObject.Destroy(go);
                continue;
            }

            nameText.text = item.itemName;
            descText.text = item.description;
            priceText.text = item.cost.ToString();
            iconImage.sprite = item.icon;

            // Actualiza estado del botón según monedas
            buyButton.interactable = ScoreManager.Instance != null && ScoreManager.Instance.CurrentScore >= item.cost;

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                onBuy?.Invoke(item);
                // tras comprar, podemos destruir la card o actualizar interacción
                GameObject.Destroy(go);
                // opcional: cerrar tienda al primer compra
                // Close();
            });
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    private GameObject GetPrefabForRarity(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => commonCardPrefab != null ? commonCardPrefab : shopCardPrefab,
            Rarity.Rare => rareCardPrefab != null ? rareCardPrefab : shopCardPrefab,
            Rarity.Epic => epicCardPrefab != null ? epicCardPrefab : shopCardPrefab,
            Rarity.Legendary => legendaryCardPrefab != null ? legendaryCardPrefab : shopCardPrefab,
            _ => shopCardPrefab,
        };
    }

    public void Close()
    {
        // limpiar
        foreach (Transform child in container)
            GameObject.Destroy(child.gameObject);

        panel.SetActive(false);
        onShopClosed?.Invoke();
    }
}
