using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class FloatingHealthBar : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;
    
    [Header("UI References")]
    [SerializeField] private RectTransform healthFillRect;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image healthFillImage;

    [Header("Position Settings")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private bool faceCamera = true;

    [Header("Visibility Settings")]
    [SerializeField] private bool hideWhenFullHealth = true;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Damage Effects")]
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private int flashCount = 2;

    private PlayerHealth playerHealth;
    private float maxFillWidth;
    private Vector2 originalPosition;
    private Camera mainCamera;
    private Color originalFillColor;
    private bool isVisible = true;
    private Coroutine flashCoroutine;

    void Start()
    {
        mainCamera = Camera.main;

        // CanvasGroup de respaldo
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Guardar el ancho máximo y posición original
        if (healthFillRect != null)
        {
            maxFillWidth = healthFillRect.rect.width;
            originalPosition = healthFillRect.anchoredPosition;
        }

        if (healthFillImage != null)
        {
            originalFillColor = healthFillImage.color;
        }

        // Iniciar búsqueda del player
        StartCoroutine(LatePlayerAssignRoutine());
    }

    private IEnumerator LatePlayerAssignRoutine()
    {
        // Seguir buscando hasta encontrar al Player
        while (playerTransform == null || playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerHealth = player.GetComponent<PlayerHealth>();

                if (playerHealth != null)
                {
                    // Suscribirse a eventos
                    playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
                    playerHealth.OnDamageTaken.AddListener(OnDamageTaken);

                    // Configuración inicial
                    UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);

                    if (hideWhenFullHealth)
                    {
                        canvasGroup.alpha = 
                            (playerHealth.CurrentHealth == playerHealth.MaxHealth) ? 0f : 1f;
                        isVisible = canvasGroup.alpha > 0f;
                    }

                    yield break; // salir cuando ya está todo listo
                }
            }

            yield return new WaitForSeconds(0.5f); // evita sobrecargar el Update
        }
    }

    void Update()
    {
        if (playerTransform == null) return;
        
        // Seguir al player
        transform.position = playerTransform.position + worldOffset;
        
        // Mirar hacia la cámara
        if (faceCamera && mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthFillRect != null)
        {
            float fillPercentage = (float)currentHealth / maxHealth;
            float newWidth = maxFillWidth * fillPercentage;

            healthFillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);

            healthFillRect.anchoredPosition = new Vector2(
                originalPosition.x - (maxFillWidth - newWidth) / 2f,
                originalPosition.y
            );
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }

        if (hideWhenFullHealth)
        {
            if (currentHealth == maxHealth && currentHealth > 0)
                HideBar();
            else if (!isVisible)
                ShowBar();
        }
    }

    private void OnDamageTaken()
    {
        if (healthFillImage != null && flashCoroutine == null)
            flashCoroutine = StartCoroutine(FlashHealthBar());

        if (!isVisible && hideWhenFullHealth)
            ShowBar();
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

    private void ShowBar()
    {
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(1f));
        }
        isVisible = true;
    }

    private void HideBar()
    {
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(0f));
        }
        isVisible = false;
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
            playerHealth.OnDamageTaken.RemoveListener(OnDamageTaken);
        }
    }
}
