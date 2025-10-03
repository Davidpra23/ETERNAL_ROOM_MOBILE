using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private UpgradeManager upgradeManager;
    public static WaveManager Instance { get; private set; }
    public int GetCurrentWave() => currentWave;

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
    [SerializeField] private string waveTextFormat = "Wave: {0}/{1}";
    [SerializeField] private string timerTextFormat = "Time: {0}s";

    [Header("Configuración de Jefes")]
    [SerializeField] private GameObject bossPrefab; // SOLO UN JEFE

    [Header("Player")]
    [Tooltip("Si está activo, cada nueva oleada comenzará con el jugador a vida máxima.")]
    [SerializeField] private bool fullHealOnNewWave = true;

    public enum WaveState { ENTRE_OLEADAS, OLEADA_EN_CURSO }
    public WaveState CurrentState { get; private set; }

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action OnAllWavesCompleted;

    private int currentWave = 0;
    private float waveTimer;
    private int totalEnemiesSpawnedThisWave = 0;
    private HashSet<GameObject> activeEnemies = new HashSet<GameObject>();
    private Vector3 spawnAreaMin;
    private Vector3 spawnAreaMax;

    // Referencia dinámica al PlayerHealth (se re-busca si es null)
    private PlayerHealth playerHealth;

    // ---------------------- Helpers UI ----------------------
    private void SetStartButtonVisible(bool visible)
    {
        if (startWaveButton == null) return;
        startWaveButton.gameObject.SetActive(visible);
    }
    // -------------------------------------------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CalculateSpawnArea();
        InitializeWaveSystem();
        EnsurePlayerHealth(); // primer intento de asignación
    }

    private void Update()
    {
        // Reasignación automática si el player/PlayerHealth cambió (respawn, cambio de escena, etc.)
        if (playerHealth == null) EnsurePlayerHealth();

        if (CurrentState == WaveState.OLEADA_EN_CURSO)
            UpdateWaveTimer();
    }

    // ==== NUEVO: búsqueda segura y barata de PlayerHealth ====
    private bool EnsurePlayerHealth()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            playerHealth = null;
            return false;
        }

        var ph = playerObj.GetComponent<PlayerHealth>();
        if (ph != null) playerHealth = ph;
        return playerHealth != null;
    }
    // =========================================================

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
            startWaveButton.interactable = true; // opcional; lo que manda es SetStartButtonVisible
            startWaveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Wave 1";
            SetStartButtonVisible(true); // visible al inicio
        }

        if (timerText != null) timerText.text = "Start wave";
    }

    public void StartNextWave()
    {
        if (CurrentState != WaveState.ENTRE_OLEADAS) return;

        // Curar a full al empezar la oleada (opcional)
        if (fullHealOnNewWave && EnsurePlayerHealth())
        {
            playerHealth.RestoreToMax(); // actualiza UI vía eventos
        }

        currentWave++;
        CurrentState = WaveState.OLEADA_EN_CURSO;
        totalEnemiesSpawnedThisWave = 0;

        // Ocultar botón completamente durante la oleada
        SetStartButtonVisible(false);

        waveTimer = waveDuration;
        UpdateWaveUI();

        StartCoroutine(StartWaveRoutine());
    }

    private IEnumerator StartWaveRoutine()
    {
        OnWaveStarted?.Invoke(currentWave);

        // Spawnear jefe SOLO en oleada 20
        if (currentWave == 20 && bossPrefab != null)
            SpawnEnemyGroup(bossPrefab, 1);

        // Corutina principal de spawn
        yield return StartCoroutine(EnemySpawnLoop());

        // Esperar a que se acabe el tiempo
        yield return new WaitUntil(() => waveTimer <= 0);

        CompleteWave();
    }

    private IEnumerator EnemySpawnLoop()
    {
        while (CurrentState == WaveState.OLEADA_EN_CURSO && waveTimer > 0)
        {
            if (activeEnemies.Count < maxActiveEnemies && totalEnemiesSpawnedThisWave < maxTotalEnemiesPerWave)
            {
                if (enemyConfiguration != null)
                {
                    foreach (var config in enemyConfiguration.enemyConfigurations)
                    {
                        if (!ShouldSpawnInCurrentWave(config)) continue;

                        int enemiesPerSpawn = config.enemiesPerSpawn + (config.enemiesPerRoundIncrease * (currentWave - config.startRound));
                        float spawnInterval = Mathf.Max(config.minInterval, config.spawnInterval - (config.intervalReductionPerRound * (currentWave - config.startRound)));

                        SpawnEnemyGroup(config.enemyPrefab, enemiesPerSpawn);

                        yield return new WaitForSeconds(spawnInterval);
                    }
                }
                else
                {
                    // fallback
                    SpawnEnemyGroup(GetRandomEnemyPrefab(), 3);
                    yield return new WaitForSeconds(2f);
                }
            }
            yield return null;
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

            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            activeEnemies.Add(enemy);
            totalEnemiesSpawnedThisWave++;

            EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null) eh.OnDeath += () => RegisterEnemyDeath(enemy);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(
            UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y),
            0
        );
    }

    private GameObject GetRandomEnemyPrefab()
    {
        GameObject[] enemyPrefabs = Resources.LoadAll<GameObject>("Enemies");
        return enemyPrefabs.Length > 0 ? enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)] : null;
    }

    private bool ShouldSpawnInCurrentWave(EnemyWaveConfig config)
    {
        if (config.enemyPrefab == null) return false;
        if (currentWave < config.startRound) return false;
        if (config.endRound >= 0 && currentWave > config.endRound) return false;
        return true;
    }

    public void RegisterEnemyDeath(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
            activeEnemies.Remove(enemy);
    }

    private void UpdateWaveTimer()
    {
        if (timerText == null) return;

        if (waveTimer > 0)
        {
            waveTimer -= Time.deltaTime;
            timerText.text = string.Format(timerTextFormat, Mathf.CeilToInt(waveTimer));
        }
    }

    private void CompleteWave()
    {
        // Eliminar todos los enemigos y objetos marcados
        foreach (var enemy in activeEnemies)
            if (enemy != null) Destroy(enemy);
        activeEnemies.Clear();

        DestroyAllWithTag("Spawner");
        DestroyAllWithTag("Coin");
        DestroyAllWithTag("Enemy");

        OnWaveCompleted?.Invoke(currentWave);

        if (currentWave >= totalWaves)
        {
            CurrentState = WaveState.ENTRE_OLEADAS;
            OnAllWavesCompleted?.Invoke();
            if (waveText != null) waveText.text = "¡Todas las oleadas completadas!";
            if (timerText != null) timerText.text = "";
            SetStartButtonVisible(false); // oculto definitivo
        }
        else
        {
            CurrentState = WaveState.ENTRE_OLEADAS;

            // Mostrar mejoras antes de preparar la siguiente oleada
            if (upgradeManager != null)
            {
                upgradeManager.ShowUpgradeChoices();
            }
            else
            {
                PrepareNextWave(); // Fallback si no hay UpgradeManager
            }
        }
    }

    private void DestroyAllWithTag(string tag)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        foreach (var obj in objs)
            Destroy(obj);
    }

    public void PrepareNextWave()
    {
        if (startWaveButton != null)
        {
            startWaveButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Iniciar Oleada {currentWave + 1}";
            startWaveButton.interactable = true; // opcional
            SetStartButtonVisible(true);         // mostrar de nuevo
        }

        if (timerText != null)
            timerText.text = "Oleada completada. Prepárate para la siguiente.";
    }

    private void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = string.Format(waveTextFormat, currentWave, totalWaves);
    }

    private void OnDestroy()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.RemoveListener(StartNextWave);
    }
}
