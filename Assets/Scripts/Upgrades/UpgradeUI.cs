using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject upgradeButtonPrefab;

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

            GameObject go = Instantiate(upgradeButtonPrefab, container);

            var nameText = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var descText = go.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var iconImage = go.transform.Find("Image")?.GetComponent<Image>();
            var buyButton = go.transform.Find("BuyButton")?.GetComponent<Button>();
            var rarityFrame = go.transform.Find("RarityFrame")?.GetComponent<Image>();

            if (rarityFrame != null)
            {
                var color = RarityUtil.GetColor(upgrade.rarity);
                color.a = 1f;
                rarityFrame.color = color;
            }

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
                    Debug.Log($"[UpgradeUI] No hay tienda en oleada {WaveManager.Instance.GetCurrentWave()}, iniciando siguiente.");
                    WaveManager.Instance?.StartNextWave();
                }
            });


        }
    }

    public void HideAndClear()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);

        if (panel != null)
            panel.SetActive(false);
    }
}
