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
    [SerializeField] private int totalWaves = 15; // Limite reducido: boss en wave 15, última tienda wave 14
    [SerializeField] private float waveDuration = 60f;

    [Header("Área de Spawn")]
    [SerializeField] private Transform spawnAreaCorner1;
    [SerializeField] private Transform spawnAreaCorner2;
    [SerializeField] private float groupSpawnRadius = 3f;

    [Header("Límites")]
    [SerializeField] private int maxActiveEnemies = 25;
    [SerializeField] private int maxTotalEnemiesPerWave = 80;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button startWaveButton;
    [SerializeField] private string waveTextFormat = "Wave: {0}/{1}";
    [SerializeField] private string timerTextFormat = "Time: {0}s";

    [Header("Auto-start Countdown")]
    [Tooltip("Delay after scene load before showing the countdown (seconds)")]
    [SerializeField] private float autoStartDelay = 1.5f;
    [Tooltip("Seconds to count down from before starting the wave")]
    [SerializeField] private int autoStartCountdownFrom = 5;
    [Tooltip("Scale multiplier for the countdown pulse effect")]
    [SerializeField] private float countdownPulseScale = 1.3f;
    [Tooltip("Duration of the pulse effect (seconds)")]
    [SerializeField] private float countdownPulseDuration = 0.45f;

    [Header("Efecto visual de inicio de oleada")]
    [SerializeField] private float waveTextPulseScale = 1.5f;
    [SerializeField] private float waveTextPulseDuration = 0.6f;

    [Header("Configuración de Jefes")]
    [SerializeField] private GameObject bossPrefab;

    [Header("Player")]
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
    private PlayerHealth playerHealth;
    // Guardar color original del timerText para restaurarlo después del countdown
    private Color originalTimerColor = Color.white;
    // Para detectar cambios de segundo y aplicar efectos sólo una vez
    private int lastTimerSeconds = -1;

    private void Awake()
    {
        // Mejor manejo del singleton para evitar que una instancia "residual"
        // de otra escena impida que el WaveManager de la escena recién cargada
        // inicialice y spawnee enemigos.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // Si existe una instancia previa y pertenece a una escena diferente,
            // preferimos la instancia de la escena que se acaba de cargar (esta).
            // Esto evita que un WaveManager creado en la escena de inicio (por ejemplo)
            // impida que el WaveManager de la escena de oleadas funcione.
            try
            {
                var existingScene = Instance.gameObject.scene;
                var thisScene = this.gameObject.scene;

                if (existingScene != thisScene)
                {
                    Debug.Log("[WaveManager] Reemplazando instancia previa por la de la nueva escena.");
                    // Destruir la instancia vieja y usar la nueva
                    Destroy(Instance.gameObject);
                    Instance = this;
                }
                else
                {
                    // Misma escena: destruir el duplicado (comportamiento original)
                    Destroy(gameObject);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WaveManager] Error comprobando escenas del singleton: " + ex.Message + ". Manteniendo la instancia actual.");
                // Fallback: no destruir nada
            }
        }

        if (enemyConfiguration == null)
        {
            enemyConfiguration = EnemyConfiguration.Instance;
            if (enemyConfiguration == null)
                Debug.LogError("[WaveManager] ❌ No se pudo cargar EnemyConfiguration.");
        }
    }

    private void Start()
    {
        // Log scene and config state to help debug cases where the scene is
        // cargada vía teletransporte desde otra escena y la configuración no se asigna.
        Debug.Log($"[WaveManager] Start() en escena '{gameObject.scene.name}'. enemyConfiguration field assigned: { (enemyConfiguration != null) }");

        // Si el campo serializado no está asignado, intentar usar el singleton Instance
        if (enemyConfiguration == null)
        {
            if (EnemyConfiguration.Instance != null)
            {
                enemyConfiguration = EnemyConfiguration.Instance;
                Debug.Log("[WaveManager] Asignada EnemyConfiguration desde EnemyConfiguration.Instance en Start().");
            }
            else
            {
                Debug.LogWarning("[WaveManager] EnemyConfiguration no asignada y EnemyConfiguration.Instance es null. Iniciando reintentos.");
                StartCoroutine(RetryLoadEnemyConfiguration());
            }
        }

        CalculateSpawnArea();
        InitializeWaveSystem();
        EnsurePlayerHealth();

        // Guardar color original del timerText para poder restaurarlo
        if (timerText != null)
            originalTimerColor = timerText.color;

        // Iniciar la secuencia automática de inicio de oleada si corresponde
        // Solo si estamos entre oleadas y no se ha comenzado ninguna todavía
        if (CurrentState == WaveState.ENTRE_OLEADAS && currentWave == 0)
        {
            StartCoroutine(AutoStartSequenceIfNeeded());
        }
    }

    private IEnumerator AutoStartSequenceIfNeeded()
    {
        // Iniciar la secuencia inmediatamente (sin delay)

        if (timerText == null)
            yield break;

        // Mostrar cuenta regresiva usando timerText con efecto pulso.
        // El color solo cambiará en el número (usando rich text). El texto general mantiene su color original.
        timerText.richText = true;
        for (int i = autoStartCountdownFrom; i >= 1; i--)
        {
            // Color que va de blanco a rojo conforme se acerca el inicio (solo para el número)
            float t = (float)(autoStartCountdownFrom - i) / Mathf.Max(1, autoStartCountdownFrom - 1);
            Color numberColor = Color.Lerp(Color.white, Color.red, t);
            string hex = ColorUtility.ToHtmlStringRGB(numberColor);

            timerText.color = originalTimerColor; // mantener color general
            timerText.text = $"La oleada empezara en <color=#{hex}>{i}</color>";

            // Pulso visual
            StartCoroutine(PulseTimerText(countdownPulseScale, countdownPulseDuration));

            yield return new WaitForSecondsRealtime(1f);
        }

    // Mensaje final en amarillo (como antes)
    timerText.text = "¡Sobrevive!!!";
    timerText.color = Color.yellow;
    StartCoroutine(PulseTimerText(countdownPulseScale * 1.2f, countdownPulseDuration * 1.1f));

        // Pequeña pausa antes de iniciar la oleada
        yield return new WaitForSecondsRealtime(1f);

        // Iniciar la oleada manteniendo el color amarillo hasta que empiece a contar
        StartNextWave();
    }

    private IEnumerator PulseTimerText(float targetScaleMul, float duration)
    {
        if (timerText == null) yield break;

        Transform t = timerText.transform;
        Vector3 originalScale = t.localScale;
        Vector3 targetScale = originalScale * targetScaleMul;

        float half = duration * 0.5f;
        float timer = 0f;

        // Escalar up
        while (timer < half)
        {
            timer += Time.unscaledDeltaTime;
            float f = timer / half;
            t.localScale = Vector3.Lerp(originalScale, targetScale, Mathf.SmoothStep(0f, 1f, f));
            yield return null;
        }

        // Escalar down
        timer = 0f;
        while (timer < half)
        {
            timer += Time.unscaledDeltaTime;
            float f = timer / half;
            t.localScale = Vector3.Lerp(targetScale, originalScale, Mathf.SmoothStep(0f, 1f, f));
            yield return null;
        }

        t.localScale = originalScale;
    }

    private IEnumerator RetryLoadEnemyConfiguration()
    {
        // Intentar varias veces con un pequeño delay; ayuda si el orden de carga provoca un timing raro
        const int maxAttempts = 10;
        int attempt = 0;
        while (attempt < maxAttempts && enemyConfiguration == null)
        {
            attempt++;
            if (EnemyConfiguration.Instance != null)
            {
                enemyConfiguration = EnemyConfiguration.Instance;
                Debug.Log($"[WaveManager] EnemyConfiguration cargada en intento {attempt}.");
                yield break;
            }
            Debug.Log($"[WaveManager] Reintento {attempt}/{maxAttempts} para cargar EnemyConfiguration...");
            yield return new WaitForSeconds(0.2f);
        }

        if (enemyConfiguration == null)
            Debug.LogError("[WaveManager] ❌ No se pudo cargar EnemyConfiguration tras reintentos. Asegúrate de que existe 'Resources/Configs/EnemyConfiguration.asset' o que el campo está asignado en el inspector.");
    }

    private void Update()
    {
        if (playerHealth == null) EnsurePlayerHealth();

        if (CurrentState == WaveState.OLEADA_EN_CURSO)
            UpdateWaveTimer();
    }

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
            // ❌ No mostrar el botón al inicio, la primera oleada es automática
            startWaveButton.gameObject.SetActive(false);
        }
        // No escribir texto en timerText aquí; lo controlará la secuencia automática
        if (timerText != null) timerText.text = "";
    }

    public void StartNextWave()
    {
        if (CurrentState != WaveState.ENTRE_OLEADAS) return;
        StopAllCoroutines();
        StartCoroutine(StartNextWaveRoutine());
    }

    private IEnumerator StartNextWaveRoutine()
    {
        if (fullHealOnNewWave && EnsurePlayerHealth())
            playerHealth.RestoreToMax();

        currentWave++;
        CurrentState = WaveState.OLEADA_EN_CURSO;
        totalEnemiesSpawnedThisWave = 0;
        startWaveButton.gameObject.SetActive(false);

        // ✨ Animación visual del texto de oleada
        yield return StartCoroutine(AnimateWaveText());

        waveTimer = waveDuration;
        lastTimerSeconds = -1; // Reset para nuevo ciclo de efectos
        UpdateWaveUI();
        StartCoroutine(StartWaveRoutine());
    }



    private IEnumerator AnimateWaveText()
    {
        if (waveText == null) yield break;

        float elapsed = 0f;
        Vector3 originalScale = waveText.transform.localScale;
        Vector3 targetScale = originalScale * waveTextPulseScale;

        while (elapsed < waveTextPulseDuration)
        {
            float t = elapsed / waveTextPulseDuration;
            waveText.transform.localScale = Vector3.Lerp(originalScale, targetScale, Mathf.Sin(t * Mathf.PI));
            elapsed += Time.unscaledDeltaTime; // se anima incluso si el tiempo está pausado
            yield return null;
        }

        waveText.transform.localScale = originalScale;
    }

    private IEnumerator StartWaveRoutine()
    {
        OnWaveStarted?.Invoke(currentWave);

        // Oleada del jefe (última oleada): sin límite de tiempo, esperar muerte del jefe
        if (currentWave == totalWaves && bossPrefab != null)
        {
            // Ocultar temporizador en la oleada del jefe
            if (timerText != null)
            {
                timerText.text = "";
            }

            // Spawnear jefe en el centro del área
            Vector3 centerPosition = new Vector3(
                (spawnAreaMin.x + spawnAreaMax.x) * 0.5f,
                (spawnAreaMin.y + spawnAreaMax.y) * 0.5f,
                0
            );
            
            GameObject boss = Instantiate(bossPrefab, centerPosition, Quaternion.identity);
            activeEnemies.Add(boss);
            totalEnemiesSpawnedThisWave++;

            // Suscribirse a la muerte del jefe
            BossHealth bh = boss.GetComponent<BossHealth>();
            if (bh != null) bh.OnDeath += () => RegisterEnemyDeath(boss);
            
            // Esperar hasta que todos los enemigos activos (incluyendo el jefe) estén muertos
            yield return new WaitUntil(() => activeEnemies.Count == 0);
            CompleteWave();
            yield break;
        }

        // Oleadas normales: spawn loop y límite de tiempo
        yield return StartCoroutine(EnemySpawnLoop());
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

            // Suscribirse a la muerte del enemigo normal
            EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null) eh.OnDeath += () => RegisterEnemyDeath(enemy);
            
            // También suscribirse si es un jefe (BossHealth)
            BossHealth bh = enemy.GetComponent<BossHealth>();
            if (bh != null) bh.OnDeath += () => RegisterEnemyDeath(enemy);
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

        // No actualizar timer en la oleada del jefe (ya está oculto)
        if (currentWave == totalWaves) return;

        if (waveTimer > 0)
        {
            waveTimer -= Time.deltaTime;
            int remaining = Mathf.CeilToInt(waveTimer);

            // Mantener formato base
            timerText.richText = true;

            // Efecto y color en últimos 5 segundos
            if (remaining <= 5 && remaining > 0)
            {
                // Color gradiente de amarillo a rojo
                float t = (float)(5 - remaining) / 4f;
                Color numberColor = Color.Lerp(Color.yellow, Color.red, t);
                string hex = ColorUtility.ToHtmlStringRGB(numberColor);
                timerText.color = originalTimerColor; // color general del texto
                timerText.text = $"<color=#{hex}>{remaining}s</color>";

                // Pulso sólo cuando cambia el segundo
                if (lastTimerSeconds != remaining)
                {
                    StartCoroutine(PulseTimerText(countdownPulseScale, countdownPulseDuration));
                    lastTimerSeconds = remaining;
                }
            }
            else
            {
                // Mostrar normal (resetea color si veníamos de la fase amarilla de 'Sobrevive!!!')
                timerText.color = originalTimerColor;
                timerText.text = string.Format(timerTextFormat, remaining);
                lastTimerSeconds = remaining; // actualizar para evitar efecto accidental si vuelve a >5 (poco probable)
            }
        }
    }

    private void CompleteWave()
    {
        foreach (var enemy in activeEnemies)
            if (enemy != null) Destroy(enemy);
        activeEnemies.Clear();

        DestroyAllWithTag("Spawner");
        DestroyAllWithTag("Coin");
        DestroyAllWithTag("Enemy");

        if (currentWave >= totalWaves)
        {
            CurrentState = WaveState.ENTRE_OLEADAS;
            if (waveText != null) waveText.text = "¡Todas las oleadas completadas!";
            if (timerText != null) timerText.text = "";
            startWaveButton.gameObject.SetActive(false);
            return;
        }

        CurrentState = WaveState.ENTRE_OLEADAS;

        // ✅ SIEMPRE mostrar panel de mejora
        if (upgradeManager != null)
            upgradeManager.ShowUpgradeChoices();
        else
            PrepareNextWave();

    }


    private void DestroyAllWithTag(string tag)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        foreach (var obj in objs)
            Destroy(obj);
    }

    public void PrepareNextWave()
    {
        // ✅ Mostrar botón para que el usuario inicie manualmente la siguiente oleada (solo después de tienda)
        if (startWaveButton != null)
        {
            int nextWave = currentWave + 1;
            var buttonText = startWaveButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = $"Empezar oleada {nextWave}";
            startWaveButton.interactable = true;
            startWaveButton.gameObject.SetActive(true);
        }
    }

    private void UpdateWaveUI()
    {
        if (waveText != null)
            waveText.text = string.Format(waveTextFormat, currentWave, totalWaves);
    }
}
