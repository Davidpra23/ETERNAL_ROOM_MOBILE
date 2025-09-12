using UnityEngine;
using System.Collections;

public class EnemySummoner : MonoBehaviour
{
    [Header("Prefab de Partículas")]
    public GameObject particlePrefab;

    [Header("Prefab de Enemigo")]
    public GameObject enemyPrefab;

    [Header("Opciones de Invocación")]
    public float enemySpawnDelay = 1f;   // Tiempo antes de invocar al enemigo
    public float fadeOutDuration = 0.5f; // Duración del fade-out antes de destruir

    private GameObject currentParticleInstance;
    private ParticleSystem[] particleSystems;

    private void Start()
    {
        Summon();
        Invoke(nameof(SummonEnemy), enemySpawnDelay);
    }

    private void Summon()
    {
        if (particlePrefab != null)
        {
            currentParticleInstance = Instantiate(particlePrefab, transform.position, particlePrefab.transform.rotation);
            particleSystems = currentParticleInstance.GetComponentsInChildren<ParticleSystem>();
        }
    }

    private void SummonEnemy()
    {
        Vector3 spawnPosition = currentParticleInstance != null ? currentParticleInstance.transform.position : transform.position;

        if (enemyPrefab != null)
            Instantiate(enemyPrefab, spawnPosition, enemyPrefab.transform.rotation);

        if (particleSystems != null)
        {
            // Detener emisión y fade-out de todos los sistemas de partículas
            foreach (var ps in particleSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            StartCoroutine(FadeOutAndDestroy());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float t = 0f;

        // Guardamos los gradientes originales para no perderlos
        Gradient[] originalGradients = new Gradient[particleSystems.Length];
        for (int i = 0; i < particleSystems.Length; i++)
        {
            var col = particleSystems[i].colorOverLifetime;
            col.enabled = true;
            originalGradients[i] = col.color.gradient;
        }

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeOutDuration);

            for (int i = 0; i < particleSystems.Length; i++)
            {
                var col = particleSystems[i].colorOverLifetime;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    originalGradients[i].colorKeys,
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(alpha, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                col.color = grad;
            }

            yield return null;
        }

        Destroy(currentParticleInstance);
        Destroy(gameObject);
    }
}
