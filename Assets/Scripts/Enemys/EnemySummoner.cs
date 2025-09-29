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
    public float fadeOutDuration = 0.5f; // (no usado en esta versión)

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
            currentParticleInstance = Instantiate(
                particlePrefab,
                transform.position,
                particlePrefab.transform.rotation
            );
            particleSystems = currentParticleInstance.GetComponentsInChildren<ParticleSystem>(true);
        }
    }

    private void SummonEnemy()
    {
        Vector3 spawnPosition = currentParticleInstance != null
            ? currentParticleInstance.transform.position
            : transform.position;

        if (enemyPrefab != null)
            Instantiate(enemyPrefab, spawnPosition, enemyPrefab.transform.rotation);

        if (particleSystems != null && particleSystems.Length > 0)
        {
            // Detener emisión de todos los PS
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                // StopEmitting: deja que las partículas vivas terminen su vida
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            StartCoroutine(WaitParticlesThenDestroy());
        }
        else
        {
            // No hay PS: destruye inmediatamente
            Destroy(gameObject);
        }
    }

    private IEnumerator WaitParticlesThenDestroy()
    {
        // Espera a que todas las partículas mueran
        bool anyAlive = true;
        while (anyAlive)
        {
            anyAlive = false;
            if (particleSystems != null)
            {
                foreach (var ps in particleSystems)
                {
                    if (ps == null) continue;
                    if (ps.IsAlive(true)) { anyAlive = true; break; }
                }
            }
            yield return null;
        }

        if (currentParticleInstance != null)
            Destroy(currentParticleInstance);

        Destroy(gameObject);
    }
}
