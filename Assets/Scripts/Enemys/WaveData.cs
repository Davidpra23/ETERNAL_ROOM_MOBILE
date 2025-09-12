using UnityEngine;
using System;

[Serializable]
public class EnemySpawnWeight
{
    public GameObject enemyPrefab;
    [Range(1, 100)] public int weight = 50;
}

[CreateAssetMenu(fileName = "NewWaveData", menuName = "Wave System/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Configuration")]
    public int waveNumber;
    [Tooltip("Número total de enemigos en esta oleada")]
    public int totalEnemies = 20;
    [Tooltip("Tiempo entre spawns de enemigos en segundos")]
    public float spawnInterval = 1f;

    [Header("Enemy Spawn Settings")]
    public EnemySpawnWeight[] enemySpawnWeights;

    [Header("Boss Wave Settings (Oleadas 5, 10, 15, 20)")]
    public bool isBossWave = false;
    public GameObject bossPrefab;
}