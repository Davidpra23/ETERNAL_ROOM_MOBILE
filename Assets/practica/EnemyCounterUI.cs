using UnityEngine;
using TMPro;

public class EnemyCounterTMP : MonoBehaviour
{
    [Header("Referencia al texto TMP")]
    [SerializeField] private TextMeshProUGUI counterText; // Asigna el texto TMP desde el inspector

    private int totalEnemies;
    private int remainingEnemies;

    void Start()
    {
        // Contar enemigos al iniciar
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        totalEnemies = enemies.Length;
        remainingEnemies = totalEnemies;

        UpdateUI();
    }

    void Update()
    {
        // Recalcular enemigos vivos
        int currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (currentEnemies != remainingEnemies)
        {
            remainingEnemies = currentEnemies;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (counterText != null)
        {
            // \n genera el salto de línea
            counterText.text = $"Elimina enemigos\n{remainingEnemies}/{totalEnemies}";
        }
    }
}