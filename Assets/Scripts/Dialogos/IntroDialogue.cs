using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class IntroDialogue : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup blackPanel;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Dialogue Settings")]
    [TextArea(2, 4)] public string[] dialogueLines = {
        "Aquí, donde todo comenzó...",
        "…un poder dormido comienza a despertar."
    };

    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private float delayBetweenLines = 2f;
    [SerializeField] private float typeSpeed = 0.05f;
    [SerializeField] private float textFadeDuration = 0.5f;

    [Header("Evento al terminar")]
    [SerializeField] private MonoBehaviour scriptToActivate;

    private void Start()
    {
        // Iniciar totalmente oscuro
        blackPanel.alpha = 1f;
        dialogueText.alpha = 0f;
        StartCoroutine(PlayIntroSequence());
    }

    IEnumerator PlayIntroSequence()
    {
        // Mostrar cada línea con efecto de texto
        foreach (string line in dialogueLines)
        {
            yield return StartCoroutine(FadeTextInAndType(line));
            yield return new WaitForSeconds(delayBetweenLines);
            dialogueText.text = "";
            dialogueText.alpha = 0f;
        }

        // Fade Out del panel completo
        yield return StartCoroutine(FadeCanvasGroup(blackPanel, 1f, 0f, fadeOutDuration));
        blackPanel.gameObject.SetActive(false);

        // Activar el script final
        if (scriptToActivate != null)
            scriptToActivate.enabled = true;
    }

    IEnumerator FadeTextInAndType(string line)
    {
        dialogueText.text = "";
        dialogueText.alpha = 0f;

        // Fade in del texto
        float elapsed = 0f;
        while (elapsed < textFadeDuration)
        {
            elapsed += Time.deltaTime;
            dialogueText.alpha = Mathf.Lerp(0f, 1f, elapsed / textFadeDuration);
            yield return null;
        }

        // Typewriter effect
        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }
}
