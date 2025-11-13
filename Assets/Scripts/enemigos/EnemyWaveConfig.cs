using UnityEngine;
using System;

[Serializable]
public class EnemyWaveConfig
{
    [Header("Identificación del Enemigo")]
    public GameObject enemyPrefab;

    [Header("Rondas de Aparición")]
    [Tooltip("Ronda inicial donde aparece este enemigo (1 = primera oleada)")]
    public int startRound = 1;
    [Tooltip("Ronda final donde aparece este enemigo (-1 = hasta el final)")]
    public int endRound = -1;

    [Header("Configuración de Spawn")]
    [Tooltip("Cantidad de enemigos de este tipo por spawn")]
    public int enemiesPerSpawn = 1;
    [Tooltip("Intervalo entre spawns en segundos")]
    public float spawnInterval = 2f;

    [Header("Escalado por Ronda")]
    [Tooltip("Cantidad adicional de enemigos por ronda")]
    public int enemiesPerRoundIncrease = 0;
    [Tooltip("Reducción del intervalo por ronda (segundos)")]
    public float intervalReductionPerRound = 0f;
    [Tooltip("Máxima reducción de intervalo permitida")]
    public float minInterval = 0.5f;
}

[CreateAssetMenu(
    fileName = "EnemyConfiguration",
    menuName = "Wave System/Enemy Configuration",
    order = 1)]
public class EnemyConfiguration : ScriptableObject
{
    [Tooltip("Lista de configuraciones de enemigos por ronda.")]
    public EnemyWaveConfig[] enemyConfigurations;

    private static EnemyConfiguration _instance;

    /// <summary>
    /// Devuelve una instancia cargada automáticamente desde Resources.
    /// </summary>
    public static EnemyConfiguration Instance
    {
        get
        {
            if (_instance == null)
            {
                // Intento principal: cargar desde Resources/Configs/EnemyConfiguration
                _instance = Resources.Load<EnemyConfiguration>("Configs/EnemyConfiguration");
                if (_instance == null)
                {
                    Debug.LogWarning("[EnemyConfiguration] No encontrado en Resources/Configs. Intentando localizar en editor...");

#if UNITY_EDITOR
                    // En el editor, si el asset no está en Resources, intentar localizarlo mediante AssetDatabase
                    try
                    {
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EnemyConfiguration");
                        if (guids != null && guids.Length > 0)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            var found = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyConfiguration>(path);
                            if (found != null)
                            {
                                _instance = found;
                                Debug.Log($"[EnemyConfiguration] Encontrado via AssetDatabase en: {path}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[EnemyConfiguration] Error al buscar con AssetDatabase: " + ex.Message);
                    }
#endif

                    if (_instance == null)
                        Debug.LogError("[EnemyConfiguration] ❌ No se encontró 'Configs/EnemyConfiguration.asset' en Resources/ ni ningún Asset del tipo en el proyecto.");
                }
            }
            return _instance;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validación rápida para evitar errores comunes
        foreach (var cfg in enemyConfigurations)
        {
            if (cfg == null) continue;
            if (cfg.enemyPrefab == null)
                Debug.LogWarning("[EnemyConfiguration] Un enemigo no tiene prefab asignado.");
            if (cfg.startRound < 1)
                cfg.startRound = 1;
            if (cfg.minInterval <= 0)
                cfg.minInterval = 0.1f;
        }
    }
#endif
}
