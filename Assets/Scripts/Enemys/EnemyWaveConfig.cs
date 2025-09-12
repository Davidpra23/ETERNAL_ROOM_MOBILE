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

[CreateAssetMenu(fileName = "NewEnemyConfiguration", menuName = "Wave System/Enemy Configuration")]
public class EnemyConfiguration : ScriptableObject
{
    public EnemyWaveConfig[] enemyConfigurations;
}