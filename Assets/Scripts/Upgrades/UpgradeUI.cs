using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform container;
    
    [Header("Upgrade Button Prefabs por Rareza")]
    [Tooltip("Prefab para upgrades Common.")]
    [SerializeField] private GameObject commonUpgradePrefab;
    [Tooltip("Prefab para upgrades Rare.")]
    [SerializeField] private GameObject rareUpgradePrefab;
    [Tooltip("Prefab para upgrades Epic.")]
    [SerializeField] private GameObject epicUpgradePrefab;
    [Tooltip("Prefab para upgrades Legendary.")]
    [SerializeField] private GameObject legendaryUpgradePrefab;

    private Action<Upgrade> onUpgradeSelected;

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Show(List<Upgrade> upgrades, Action<Upgrade> onSelected)
    {
        onUpgradeSelected = onSelected;
        panel.SetActive(true);

        // ✅ Pausar el juego mientras se elige una mejora
        Time.timeScale = 0f;

        // Limpiar anteriores
        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (var upgrade in upgrades)
        {
            if (upgrade == null) continue;

            // Elegir prefab según rareza
            GameObject prefabToUse = GetPrefabForRarity(upgrade.rarity);
            if (prefabToUse == null)
            {
                Debug.LogError($"[UpgradeUI] No hay prefab asignado para rareza {upgrade.rarity}.");
                continue;
            }

            GameObject go = Instantiate(prefabToUse, container);

            var nameText = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = go.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var iconImage = go.transform.Find("Image")?.GetComponent<Image>();
            var buyButton = go.transform.Find("BuyButton")?.GetComponent<Button>();

            if (nameText == null || descText == null || iconImage == null || buyButton == null)
            {
                Debug.LogError("[UpgradeUI] Prefab incompleto: falta NameText, DescriptionText, Image o BuyButton.");
                continue;
            }

            nameText.text = upgrade.upgradeName;
            descText.text = upgrade.description;
            iconImage.sprite = upgrade.icon;

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() =>
            {
                // ✅ Aplica mejora
                onUpgradeSelected?.Invoke(upgrade);

                // ✅ Limpia y cierra panel
                HideAndClear();
                Time.timeScale = 1f; // reanuda el juego

                // ✅ Verificar si se debe mostrar tienda
                var shop = FindObjectOfType<ShopManager>();
                if (shop != null && shop.ShouldShowShopThisWave())
                {
                    Debug.Log($"[UpgradeUI] Mostrando tienda después de oleada {WaveManager.Instance.GetCurrentWave()}");
                    shop.ShowShop();
                }
                else
                {
                    // ✅ No hay tienda: iniciar siguiente oleada automáticamente
                    Debug.Log($"[UpgradeUI] No hay tienda en oleada {WaveManager.Instance.GetCurrentWave()}, iniciando siguiente.");
                    WaveManager.Instance?.StartNextWave();
                }
            });


        }
    }

    private GameObject GetPrefabForRarity(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => commonUpgradePrefab,
            Rarity.Rare => rareUpgradePrefab,
            Rarity.Epic => epicUpgradePrefab,
            Rarity.Legendary => legendaryUpgradePrefab,
            _ => null,
        };
    }

    public void HideAndClear()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        if (panel != null)
            panel.SetActive(false);
    }
}
