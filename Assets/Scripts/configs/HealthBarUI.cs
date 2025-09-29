using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform healthFillRect; // <- Ya no lo usaremos para escalar
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image healthFillImage;

    [Header("Damage Effects")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private int flashCount = 2;

    // Opcional: suavizado del fill
    [Header("Smoothing")]
    [SerializeField] private bool smoothFill = true;
    [SerializeField] private float fillLerpSpeed = 10f;

    private PlayerHealth playerHealth;
    private Color originalFillColor;
    private Coroutine flashCoroutine;
    private float retryTimer = 0f;
    private float targetFill = 1f;

    void Start()
    {
        if (healthFillImage != null)
        {
            // Asegurar configuración correcta del Image (recorte horizontal)
            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFillImage.fillAmount = 1f;

            originalFillColor = healthFillImage.color;
        }

        TryFindPlayerHealth();
    }

    void Update()
    {
        if (playerHealth == null)
        {
            retryTimer -= Time.deltaTime;
            if (retryTimer <= 0f)
            {
                TryFindPlayerHealth();
                retryTimer = 1f;
            }
        }

        if (smoothFill && healthFillImage != null)
        {
            healthFillImage.fillAmount = Mathf.Lerp(
                healthFillImage.fillAmount,
                targetFill,
                Time.deltaTime * fillLerpSpeed
            );
        }
    }

    private void TryFindPlayerHealth()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
            playerHealth.OnDamageTaken.AddListener(OnDamageTaken);
            UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            Debug.Log("HealthBarUI: PlayerHealth encontrado y vinculado.");
        }
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        float fillPercentage = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        // Recorte por fillAmount (no escalado)
        if (healthFillImage != null)
        {
            if (smoothFill)
                targetFill = fillPercentage;
            else
                healthFillImage.fillAmount = fillPercentage;
        }

        if (healthText != null)
            healthText.text = $"{currentHealth}/{maxHealth}";
    }

    private void OnDamageTaken()
    {
        if (healthFillImage != null && flashCoroutine == null)
            flashCoroutine = StartCoroutine(FlashHealthBar());
    }

    private IEnumerator FlashHealthBar()
    {
        for (int i = 0; i < flashCount; i++)
        {
            if (healthFillImage != null)
            {
                healthFillImage.color = damageFlashColor;
                yield return new WaitForSeconds(flashDuration / 2);
                healthFillImage.color = originalFillColor;
                yield return new WaitForSeconds(flashDuration / 2);
            }
        }
        flashCoroutine = null;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
            playerHealth.OnDamageTaken.RemoveListener(OnDamageTaken);
        }
    }

#if UNITY_EDITOR
    // Útil al editar: si cambias la imagen en el inspector, se reconfigura sola.
    void OnValidate()
    {
        if (healthFillImage != null)
        {
            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
#endif
}
