using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextRevealDespierta : MonoBehaviour
{
    [Header("Duraciones")]
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float visibleDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("Escala")]
    [SerializeField] private float startScale = 0.9f;
    [SerializeField] private float peakScale = 1.1f;
    [SerializeField] private float endScale = 0.95f;

    [Header("Brillo pulsante (opcional)")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.05f;

    private TextMeshProUGUI textComponent;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        StartCoroutine(PlayEffect());
    }

    private IEnumerator PlayEffect()
    {
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * startScale;

        // Fade In + Escala hacia arriba
        float time = 0f;
        while (time < fadeInDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeInDuration;
            canvasGroup.alpha = t;
            rectTransform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one * peakScale, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.localScale = Vector3.one * peakScale;

        // Comenzar pulsación si está activa
        if (enablePulse)
            pulseCoroutine = StartCoroutine(PulseEffect());

        yield return new WaitForSeconds(visibleDuration);

        // Detener pulsación si estaba activa
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        // Fade Out + Escala hacia abajo
        time = 0f;
        Vector3 currentScale = rectTransform.localScale;
        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeOutDuration;
            canvasGroup.alpha = 1f - t;
            rectTransform.localScale = Vector3.Lerp(currentScale, Vector3.one * endScale, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private IEnumerator PulseEffect()
    {
        float baseAlpha = 1f;
        float time = 0f;

        while (true)
        {
            time += Time.deltaTime * pulseSpeed;
            float alphaOffset = Mathf.Sin(time) * pulseAmount;
            canvasGroup.alpha = baseAlpha + alphaOffset;
            yield return null;
        }
    }
}
