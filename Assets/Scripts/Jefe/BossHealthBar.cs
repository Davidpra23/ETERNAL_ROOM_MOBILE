using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossHealthBar : MonoBehaviour
{
    public Image healthFill;
    public Image background;
    public CanvasGroup canvasGroup;
    
    private void Start()
    {
        // Inicialmente oculta
        canvasGroup.alpha = 0f;
    }
    
    public void ShowHealthBar()
    {
        StartCoroutine(FadeInHealthBar());
    }
    
    public void HideHealthBar()
    {
        StartCoroutine(FadeOutHealthBar());
    }
    
    public void UpdateHealth(float healthPercentage)
    {
        healthFill.fillAmount = healthPercentage;
        
        // Cambiar color según la vida
        healthFill.color = Color.Lerp(Color.red, Color.green, healthPercentage);
        // Guardar color actual para restaurarlo después de efectos
        lastFillColor = healthFill.color;
        
        // Efecto de shake cuando recibe mucho daño
        if (healthPercentage < 0.3f)
        {
            StartCoroutine(HealthBarShake());
        }
    }
    
    IEnumerator FadeInHealthBar()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    IEnumerator FadeOutHealthBar()
    {
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    IEnumerator HealthBarShake()
    {
        Vector3 originalPos = transform.localPosition;
        float shakeDuration = 0.5f;
        float shakeMagnitude = 10f;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            
            transform.localPosition = originalPos + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = originalPos;
    }

    // ------------------ DAMAGE PULSE / BLINK ------------------
    private Coroutine damagePulseRoutine;
    private Color lastFillColor = Color.green;

    public void DamagePulse(float duration = 0.18f)
    {
        if (damagePulseRoutine != null)
            StopCoroutine(damagePulseRoutine);

        damagePulseRoutine = StartCoroutine(DamagePulseRoutine(duration));
    }

    private IEnumerator DamagePulseRoutine(float duration)
    {
        // Guarda color original
        Color originalColor = lastFillColor;

        float half = duration * 0.5f;
        float elapsed = 0f;

        // Subida: hacia blanco
        while (elapsed < half)
        {
            float t = elapsed / half;
            healthFill.color = Color.Lerp(originalColor, Color.white, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Asegurar color máximo
        healthFill.color = Color.white;

        // Bajada: volver a original
        elapsed = 0f;
        while (elapsed < half)
        {
            float t = elapsed / half;
            healthFill.color = Color.Lerp(Color.white, originalColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restaurar
        healthFill.color = originalColor;
        damagePulseRoutine = null;
    }
}