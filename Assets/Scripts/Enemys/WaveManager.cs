using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Configuración General")]
    [SerializeField] private EnemyConfiguration enemyConfiguration;
    [SerializeField] private int totalWaves = 20;
    [SerializeField] private float waveDuration = 60f;

    [Header("Área de Spawn (Rectángulo)")]
    [SerializeField] private Transform spawnAreaCorner1;
    [SerializeField] private Transform spawnAreaCorner2;
    [SerializeField] private float groupSpawnRadius = 3f;

    [Header("Límites de Rendimiento")]
    [SerializeField] private int maxActiveEnemies = 25;
    [SerializeField] private int maxTotalEnemiesPerWave = 80;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button startWaveButton;
    [SerializeField] private string waveTextFormat = "Oleada: {0}/20";
    [SerializeField] private string timerTextFormat = "Tiempo: {0}s";

    [Header("Configuración de Jefes")]
    [SerializeField] private GameObject[] bossPrefabs;

    public enum WaveState { ENTRE_OLEADAS, OLEADA_EN_CURSO }
    public WaveState CurrentState { get; private set; }

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action OnAllWavesCompleted;

    private int currentWave = 0;
    private float waveTimer;
    private int enemiesRemaining;
    private int totalEnemiesSpawnedThisWave = 0;
    private GameObject currentBoss;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private List<Coroutine> spawnCoroutines = new List<Coroutine>();
    private Vector3 spawnAreaMin;
    private Vector3 spawnAreaMax;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CalculateSpawnArea();
        InitializeWaveSystem();
    }

    private void CalculateSpawnArea()
    {
        if (spawnAreaCorner1 != null && spawnAreaCorner2 != null)
        {
            spawnAreaMin = new Vector3(
                Mathf.Min(spawnAreaCorner1.position.x, spawnAreaCorner2.position.x),
                Mathf.Min(spawnAreaCorner1.position.y, spawnAreaCorner2.position.y),
                0
            );
            
            spawnAreaMax = new Vector3(
                Mathf.Max(spawnAreaCorner1.position.x, spawnAreaCorner2.position.x),
                Mathf.Max(spawnAreaCorner1.position.y, spawnAreaCorner2.position.y),
                0
            );
        }
        else
        {
            // Fallback: área por defecto alrededor del manager
            spawnAreaMin = transform.position - new Vector3(10f, 10f, 0);
            spawnAreaMax = transform.position + new Vector3(10f, 10f, 0);
        }
    }

    private void InitializeWaveSystem()
    {
        CurrentState = WaveState.ENTRE_OLEADAS;
        currentWave = 0;
        UpdateWaveUI();
        
        if (startWaveButton != null)
        {
            startWaveButton.onClick.AddListener(StartNextWave);
            startWaveButton.interactable = true;
            startWaveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Iniciar Oleada 1";
        }
        
        timerText.text = "Presiona el botón para comenzar";
    }

    private void Update()
    {
        if (CurrentState == WaveState.OLEADA_EN_CURSO)
        {
            UpdateWaveTimer();
        }
    }

    public void StartNextWave()
    {
        if (CurrentState != WaveState.ENTRE_OLEADAS)
            return;

        currentWave++;
        CurrentState = WaveState.OLEADA_EN_CURSO;
        totalEnemiesSpawnedThisWave = 0;
        
        if (startWaveButton != null)
            startWaveButton.interactable = false;

        waveTimer = waveDuration;
        UpdateWaveUI();

        StartCoroutine(StartWaveRoutine());
    }

    private IEnumerator StartWaveRoutine()
    {
        OnWaveStarted?.Invoke(currentWave);

        if (IsBossWave() && bossPrefabs != null && bossPrefabs.Length > 0)
        {
            SpawnBoss();
            enemiesRemaining++;
        }

        StartEnemySpawning();

        // SOLO POR TIEMPO: Eliminada la condición de enemigos restantes
        yield return new WaitUntil(() => waveTimer <= 0);

        CompleteWave();
    }

    private void StartEnemySpawning()
    {
        StopAllSpawning();

        if (enemyConfiguration == null || enemyConfiguration.enemyConfigurations == null)
        {
            Debug.LogWarning("No hay configuración de enemigos asignada. Usando enemigos por defecto.");
            StartCoroutine(DefaultEnemySpawning());
            return;
        }

        foreach (var config in enemyConfiguration.enemyConfigurations)
        {
            if (ShouldSpawnInCurrentWave(config))
            {
                int actualEnemiesPerSpawn = config.enemiesPerSpawn + (config.enemiesPerRoundIncrease * (currentWave - config.startRound));
                float actualSpawnInterval = Mathf.Max(config.minInterval, config.spawnInterval - (config.intervalReductionPerRound * (currentWave - config.startRound)));

                Coroutine spawnCoroutine = StartCoroutine(SpawnEnemyTypeRoutine(config, actualEnemiesPerSpawn, actualSpawnInterval));
                spawnCoroutines.Add(spawnCoroutine);
            }
        }
    }

    private IEnumerator DefaultEnemySpawning()
    {
        while (CurrentState == WaveState.OLEADA_EN_CURSO && waveTimer > 0)
        {
            if (activeEnemies.Count < maxActiveEnemies && totalEnemiesSpawnedThisWave < maxTotalEnemiesPerWave)
            {
                SpawnEnemyGroup(GetRandomEnemyPrefab(), 3); // Grupo de 3 enemigos por defecto
                yield return new WaitForSeconds(2f);
            }
            yield return null;
        }
    }

    private GameObject GetRandomEnemyPrefab()
    {
        GameObject[] enemyPrefabs = Resources.LoadAll<GameObject>("Enemies");
        return enemyPrefabs.Length > 0 ? enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)] : null;
    }

    private IEnumerator SpawnEnemyTypeRoutine(EnemyWaveConfig config, int enemiesPerSpawn, float spawnInterval)
    {
        while (CurrentState == WaveState.OLEADA_EN_CURSO && waveTimer > 0)
        {
            if (activeEnemies.Count < maxActiveEnemies && totalEnemiesSpawnedThisWave < maxTotalEnemiesPerWave)
            {
                SpawnEnemyGroup(config.enemyPrefab, enemiesPerSpawn);
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemyGroup(GameObject enemyPrefab, int count)
    {
        if (enemyPrefab == null) return;

        Vector3 groupCenter = GetRandomSpawnPosition();
        
        for (int i = 0; i < count; i++)
        {
            if (activeEnemies.Count >= maxActiveEnemies || totalEnemiesSpawnedThisWave >= maxTotalEnemiesPerWave)
                break;

            Vector3 spawnPosition = groupCenter + UnityEngine.Random.insideUnitSphere * groupSpawnRadius;
            spawnPosition.z = 0;

            SpawnSingleEnemy(enemyPrefab, spawnPosition);
        }
    }

    private void SpawnSingleEnemy(GameObject enemyPrefab, Vector3 position)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        activeEnemies.Add(enemy);
        enemiesRemaining++;
        totalEnemiesSpawnedThisWave++;

        SetupEnemy(enemy);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(
            UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y),
            0
        );
    }

    private bool ShouldSpawnInCurrentWave(EnemyWaveConfig config)
    {
        if (config.enemyPrefab == null) return false;
        if (currentWave < config.startRound) return false;
        if (config.endRound >= 0 && currentWave > config.endRound) return false;
        return true;
    }

    private bool IsBossWave()
    {
        return currentWave % 5 == 0;
    }

    private void SpawnBoss()
    {
        int bossIndex = (currentWave / 5) - 1;
        if (bossIndex < bossPrefabs.Length && bossPrefabs[bossIndex] != null)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            currentBoss = Instantiate(bossPrefabs[bossIndex], spawnPosition, Quaternion.identity);
            activeEnemies.Add(currentBoss);
            enemiesRemaining++;
            totalEnemiesSpawnedThisWave++;

            SetupEnemy(currentBoss);
        }
    }

    private void SetupEnemy(GameObject enemy)
    {
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath += OnEnemyDeath;
        }

        if (enemy.GetComponent<EnemySeparation>() == null)
        {
            enemy.AddComponent<EnemySeparation>();
        }
    }

    private void OnEnemyDeath()
    {
        enemiesRemaining--;
    }

    public void RegisterEnemyDeath(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            OnEnemyDeath();
        }
    }

    private void UpdateWaveTimer()
    {
        if (waveTimer > 0)
        {
            waveTimer -= Time.deltaTime;
            timerText.text = string.Format(timerTextFormat, Mathf.CeilToInt(waveTimer));
        }
    }

    private void StopAllSpawning()
    {
        foreach (var coroutine in spawnCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        spawnCoroutines.Clear();
    }

    private void CompleteWave()
    {
        StopAllSpawning();
        OnWaveCompleted?.Invoke(currentWave);

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        activeEnemies.Clear();

        if (currentWave >= totalWaves)
        {
            CurrentState = WaveState.ENTRE_OLEADAS;
            OnAllWavesCompleted?.Invoke();
            waveText.text = "¡Todas las oleadas completadas!";
            timerText.text = "";
            if (startWaveButton != null) startWaveButton.gameObject.SetActive(false);
        }
        else
        {
            CurrentState = WaveState.ENTRE_OLEADAS;
            PrepareNextWave();
        }
    }

    private void PrepareNextWave()
    {
        if (startWaveButton != null)
        {
            startWaveButton.interactable = true;
            startWaveButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Iniciar Oleada {currentWave + 1}";
        }
        
        timerText.text = "Oleada completada. Prepárate para la siguiente.";
    }

    private void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = string.Format(waveTextFormat, currentWave, totalWaves);
        }
    }

    private void OnDestroy()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.RemoveListener(StartNextWave);

        StopAllSpawning();
        
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                    enemyHealth.OnDeath -= OnEnemyDeath;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Dibujar área de spawn
        if (spawnAreaCorner1 != null && spawnAreaCorner2 != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Vector3 center = (spawnAreaCorner1.position + spawnAreaCorner2.position) / 2;
            Vector3 size = new Vector3(
                Mathf.Abs(spawnAreaCorner2.position.x - spawnAreaCorner1.position.x),
                Mathf.Abs(spawnAreaCorner2.position.y - spawnAreaCorner1.position.y),
                0.1f
            );
            Gizmos.DrawCube(center, size);
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif
}