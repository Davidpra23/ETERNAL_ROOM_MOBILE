using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private CanvasGroup transitionImage;     // Asignar en inspector
    [SerializeField] private float fadeDuration = 1f;         // 0->1 de alpha
    [SerializeField] private float scaleDuration = 1f;        // Escala actual -> maxScale
    [SerializeField] private float maxScale = 1.5f;           // Escala objetivo
    [SerializeField] private float delayBeforeSceneChange = 0.5f;

    private void Start()
    {
        if (transitionImage != null)
        {
            transitionImage.alpha = 0f;            // inicia invisible
            // No fijamos escala aquí para respetar la escala actual del objeto en escena
        }
    }

    public void ChangeScene(string sceneName)
    {
        if (transitionImage != null)
            StartCoroutine(TransitionEffect(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    private IEnumerator TransitionEffect(string sceneName)
    {
        transitionImage.blocksRaycasts = true;

        // Partimos desde LA ESCALA ACTUAL del objeto
        Vector3 startScale = transitionImage.transform.localScale;
        Vector3 targetScale = new Vector3(maxScale, maxScale, startScale.z);

        float fadeTime  = 0f;
        float scaleTime = 0f;

        // Avanza ambos procesos de forma independiente
        while (fadeTime < fadeDuration || scaleTime < scaleDuration)
        {
            // (Opcional) usa Time.unscaledDeltaTime si haces la transición con el juego en pausa
            float dt = Time.deltaTime;

            if (fadeTime < fadeDuration)
            {
                fadeTime += dt;
                float tFade = Mathf.Clamp01(fadeTime / fadeDuration);
                transitionImage.alpha = Mathf.Lerp(0f, 1f, tFade);
            }

            if (scaleTime < scaleDuration)
            {
                scaleTime += dt;
                float tScale = Mathf.Clamp01(scaleTime / scaleDuration);
                // Puedes cambiar por Mathf.SmoothStep(0,1,tScale) si quieres easing
                transitionImage.transform.localScale = Vector3.Lerp(startScale, targetScale, tScale);
            }

            yield return null;
        }

        // Asegura estados finales exactos
        transitionImage.alpha = 1f;
        transitionImage.transform.localScale = targetScale;

        // Espera configurable antes de cambiar de escena
        if (delayBeforeSceneChange > 0f)
            yield return new WaitForSeconds(delayBeforeSceneChange);

        SceneManager.LoadScene(sceneName);
    }
}
