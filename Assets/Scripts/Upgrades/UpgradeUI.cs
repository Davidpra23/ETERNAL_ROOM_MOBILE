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

    public void Show(List<Upgrade> upgrades, Action<Upgrade> onSelected)
    {
        onUpgradeSelected = onSelected;
        panel.SetActive(true);

        // (Opcional) si pausas el juego al abrir upgrades, hazlo aquí:
        // Time.timeScale = 0f;

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
                // color por rareza
                var color = RarityUtil.GetColor(upgrade.rarity);
                // mantener la transparencia del overlay (por si quieres un alpha suave)
                color.a = 1f; // o 0.8f si quieres un poco más suave
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
                // 1) Aplica la mejora
                onUpgradeSelected?.Invoke(upgrade);

                // 2) Oculta el panel y limpia
                HideAndClear();

                // Mostrar tienda
                var shop = FindObjectOfType<ShopManager>();
                if (shop != null)
                {
                    shop.ShowShop(); // La tienda, al cerrarse, llamará a WaveManager.Instance.PrepareNextWave()
                }
                else
                {
                    // Fallback si no hay ShopManager en escena:
                    WaveManager.Instance?.PrepareNextWave();
                }
            });
        }
    }

    public void HideAndClear()
    {
        // Limpia todas las tarjetas
        foreach (Transform child in container)
            Destroy(child.gameObject);

        // Oculta el panel
        if (panel != null) panel.SetActive(false);
    }
}
