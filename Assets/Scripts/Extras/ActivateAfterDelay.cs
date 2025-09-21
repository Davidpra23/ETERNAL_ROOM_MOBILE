using UnityEngine;
using System.Collections;

public class ActivateAfterDelay : MonoBehaviour
{
    [Header("Objeto a activar")]
    [SerializeField] private GameObject targetObject;

    [Header("Tiempo de espera (segundos)")]
    [SerializeField] private float delay = 1f;

    [Header("Fade In (opcional)")]
    [SerializeField] private bool useFadeIn = false;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Auto ejecución")]
    [SerializeField] private bool activateOnStart = true;

    private void Start()
    {
        if (activateOnStart)
            StartActivation();
    }

    public void StartActivation()
    {
        StartCoroutine(ActivateAfterTime());
    }

    private IEnumerator ActivateAfterTime()
    {
        yield return new WaitForSeconds(delay);

        if (targetObject == null)
            yield break;

        if (!useFadeIn)
        {
            targetObject.SetActive(true);
        }
        else
        {
            // Activar objeto, pero iniciar invisible si tiene SpriteRenderer(s)
            targetObject.SetActive(true);

            SpriteRenderer[] sprites = targetObject.GetComponentsInChildren<SpriteRenderer>();

            foreach (var sprite in sprites)
            {
                Color c = sprite.color;
                c.a = 0f;
                sprite.color = c;
            }

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);

                foreach (var sprite in sprites)
                {
                    Color c = sprite.color;
                    c.a = alpha;
                    sprite.color = c;
                }

                yield return null;
            }

            // Asegurar alpha final en 1
            foreach (var sprite in sprites)
            {
                Color c = sprite.color;
                c.a = 1f;
                sprite.color = c;
            }
        }
    }
}
