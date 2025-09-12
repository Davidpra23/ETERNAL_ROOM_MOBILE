using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform healthFillRect;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthFillImage;

    [Header("Damage Effects")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private int flashCount = 2;

    private PlayerHealth playerHealth;
    private float maxFillWidth;
    private Vector2 originalPosition;
    private Color originalFillColor;
    private Coroutine flashCoroutine;

    void Start()
    {
        // Buscar el PlayerHealth en la escena
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        if (playerHealth == null)
        {
            Debug.LogError("No se encontró PlayerHealth en la escena!");
            return;
        }

        // Guardar el ancho máximo y posición original
        if (healthFillRect != null)
        {
            maxFillWidth = healthFillRect.rect.width;
            originalPosition = healthFillRect.anchoredPosition;
        }

        // Guardar color original
        if (healthFillImage != null)
        {
            originalFillColor = healthFillImage.color;
        }

        // Suscribirse a los eventos de salud
        playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
        playerHealth.OnDamageTaken.AddListener(OnDamageTaken);
        
        // Configurar valores iniciales
        UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        // Actualizar tamaño de la barra
        if (healthFillRect != null)
        {
            float fillPercentage = (float)currentHealth / maxHealth;
            float newWidth = maxFillWidth * fillPercentage;
            
            healthFillRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, 
                newWidth
            );
            
            healthFillRect.anchoredPosition = new Vector2(
                originalPosition.x - (maxFillWidth - newWidth) / 2f,
                originalPosition.y
            );
        }
        
        // Actualizar texto
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    private void OnDamageTaken()
    {
        // Efecto de parpadeo para la barra principal
        if (healthFillImage != null && flashCoroutine == null)
        {
            flashCoroutine = StartCoroutine(FlashHealthBar());
        }
    }

    private IEnumerator FlashHealthBar()
    {
        for (int i = 0; i < flashCount; i++)
        {
            if (healthFillImage != null)
            {
                // Cambiar a color de daño
                healthFillImage.color = damageFlashColor;
                yield return new WaitForSeconds(flashDuration / 2);
                
                // Volver al color original
                healthFillImage.color = originalFillColor;
                yield return new WaitForSeconds(flashDuration / 2);
            }
        }
        
        flashCoroutine = null;
    }

    void OnDestroy()
    {
        // Desuscribirse de los eventos al destruir
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
            playerHealth.OnDamageTaken.RemoveListener(OnDamageTaken);
        }
    }
}