using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

public class Light2DFade : MonoBehaviour
{
    [Header("Configuración de luz")]
    [SerializeField] private Light2D targetLight;
    [SerializeField] private float startIntensity = 1f;
    [SerializeField] private float endIntensity = 0f;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float delayBeforeStart = 0f;
    [SerializeField] private bool playOnStart = true;

    [Header("Scripts a activar al finalizar (opcional)")]
    [SerializeField] private List<MonoBehaviour> scriptsToActivate = new List<MonoBehaviour>();

    private void Start()
    {
        if (targetLight != null)
            targetLight.intensity = startIntensity;

        if (playOnStart)
            StartFade();
    }

    public void StartFade()
    {
        StartCoroutine(FadeLight());
    }

    private IEnumerator FadeLight()
    {
        yield return new WaitForSeconds(delayBeforeStart);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float current = Mathf.Lerp(startIntensity, endIntensity, t);
            if (targetLight != null)
                targetLight.intensity = current;

            yield return null;
        }

        if (targetLight != null)
            targetLight.intensity = endIntensity;

        // Activar scripts al finalizar
        foreach (var script in scriptsToActivate)
        {
            if (script != null)
                script.enabled = true;
        }
    }
}
