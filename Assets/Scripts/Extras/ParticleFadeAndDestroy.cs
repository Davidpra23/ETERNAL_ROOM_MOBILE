using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleFadeAndDestroy : MonoBehaviour
{
    [Header("Duraciones")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float visibleDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Luz (opcional)")]
    [SerializeField] private Light2D targetLight;
    [SerializeField] private float lightMaxIntensity = 1f;

    private ParticleSystem[] systems;
    private Gradient[] originalGradients;

    void Start()
    {
        systems = GetComponentsInChildren<ParticleSystem>();

        // Guardar gradientes originales
        originalGradients = new Gradient[systems.Length];
        for (int i = 0; i < systems.Length; i++)
        {
            var col = systems[i].colorOverLifetime;
            col.enabled = true;

            if (col.color.mode == ParticleSystemGradientMode.Gradient)
                originalGradients[i] = col.color.gradient;
            else
            {
                var grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
                originalGradients[i] = grad;
            }
        }

        if (targetLight != null)
            targetLight.intensity = 0f;

        StartCoroutine(FadeSequence());
    }

    IEnumerator FadeSequence()
    {
        // Fade In
        yield return StartCoroutine(FadeToAlpha(0f, 1f, fadeInDuration));

        // Espera visible
        yield return new WaitForSeconds(visibleDuration);

        // Detener emisión
        foreach (var ps in systems)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Fade Out
        yield return StartCoroutine(FadeToAlpha(1f, 0f, fadeOutDuration));

        Destroy(gameObject);
    }

    IEnumerator FadeToAlpha(float from, float to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);

            // Fade partículas
            for (int i = 0; i < systems.Length; i++)
            {
                var col = systems[i].colorOverLifetime;

                Gradient grad = new Gradient();
                grad.SetKeys(
                    originalGradients[i].colorKeys,
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(alpha, 0f),
                        new GradientAlphaKey(alpha, 1f)
                    }
                );

                col.color = grad;
            }

            // Fade luz
            if (targetLight != null)
                targetLight.intensity = Mathf.Lerp(0f, lightMaxIntensity, alpha);

            yield return null;
        }

        // Asegurar valores finales exactos
        if (targetLight != null)
            targetLight.intensity = to == 0f ? 0f : lightMaxIntensity;
    }
}
