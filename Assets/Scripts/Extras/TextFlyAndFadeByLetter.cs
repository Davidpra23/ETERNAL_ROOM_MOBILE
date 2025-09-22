using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextFlyAndFadeByLetter : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float visibleDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 0.7f;

    [Header("Movement & Scale")]
    [SerializeField] private float floatDistance = 30f;
    [SerializeField] private float scaleIn = 0.9f;
    [SerializeField] private float scaleOut = 1.05f;

    [Header("Direction Settings")]
    [SerializeField] private Vector2 floatDirection = Vector2.up; // Puedes cambiar a down, left, right según la runa

    private TextMeshProUGUI textComponent;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private Vector3 initialScale;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;
        initialScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        ResetState();
        StartCoroutine(Animate());
    }

    private void ResetState()
    {
        canvasGroup.alpha = 0f;
        rectTransform.anchoredPosition = initialPosition;
        rectTransform.localScale = initialScale * scaleIn;
    }

    private IEnumerator Animate()
    {
        // Normalizamos la dirección para evitar que el movimiento dependa del tamaño del vector
        Vector2 direction = floatDirection.normalized;

        // Fade In + Scale Up + Float Start
        float time = 0f;
        while (time < fadeInDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeInDuration);
            canvasGroup.alpha = t;
            rectTransform.anchoredPosition = initialPosition + direction * (floatDistance * t * 0.3f);
            rectTransform.localScale = Vector3.Lerp(initialScale * scaleIn, initialScale * scaleOut, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.localScale = initialScale * scaleOut;

        yield return new WaitForSeconds(visibleDuration);

        // Fade Out + Continue Floating
        time = 0f;
        Vector2 currentPos = rectTransform.anchoredPosition;
        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeOutDuration);
            canvasGroup.alpha = 1f - t;
            rectTransform.anchoredPosition = currentPos + direction * (floatDistance * t * 0.7f);
            rectTransform.localScale = Vector3.Lerp(initialScale * scaleOut, initialScale, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
