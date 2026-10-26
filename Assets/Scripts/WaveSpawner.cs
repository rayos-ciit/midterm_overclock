using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    #region Dependencies
    [Header("Injected Managers")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GridPathManager gridManager;
    [SerializeField] private BuildPlacementController buildController;
    #endregion

    #region State Variables
    public int CurrentWaveIndex { get; private set; } = 0;
    public bool IsPrepPhase { get; private set; } = false;
    public float CurrentPrepTimer { get; private set; } = 0f;
    private bool isWaveSpawning = false;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        if (gameManager == null || !gameManager.IsGameActive || gameManager.IsPaused) return;
        if (isWaveSpawning) return;

        CheckWaveCompletion();
    }
    #endregion

    #region Spawning Logic
    private void CheckWaveCompletion()
    {
        if (EnemyController.ActiveEnemies.Count > 0) return;

        CurrentWaveIndex++;
        WaveConfig[] waves = gameManager.config.waves;

        if (CurrentWaveIndex >= waves.Length)
        {
            gameManager.TriggerWinGame();
        }
        else
        {
            StartNextWave();
        }
    }

    public void BeginWaves()
    {
        StopWaves();
        CurrentWaveIndex = 0;
        StartNextWave();
    }

    public void StopWaves()
    {
        StopAllCoroutines();
        isWaveSpawning = false;
        IsPrepPhase = false;
    }

    private void StartNextWave()
    {
        WaveConfig[] waves = gameManager.config.waves;
        if (CurrentWaveIndex >= waves.Length) return;
        
        StartCoroutine(SpawnWaveCoroutine(waves[CurrentWaveIndex], CurrentWaveIndex + 1));
    }

    private IEnumerator SpawnWaveCoroutine(WaveConfig cfg, int waveNumber)
    {
        isWaveSpawning = true;
        IsPrepPhase = true;
        CurrentPrepTimer = gameManager.config.wavePrepTime;

        while (CurrentPrepTimer > 0f)
        {
            if (gameManager != null && gameManager.IsGameActive && !gameManager.IsPaused)
            {
                CurrentPrepTimer -= Time.deltaTime;
            }
            yield return null;
        }
        IsPrepPhase = false;

        int gruntsLeft = cfg.grunts;
        int saboteursLeft = cfg.saboteurs;
        int assassinsLeft = cfg.assassins;
        int totalToSpawn = gruntsLeft + saboteursLeft + assassinsLeft;

        for (int i = 0; i < totalToSpawn; i++)
        {
            while (gameManager != null && !gameManager.IsGameActive) yield return null;

            EnemyKind kindToSpawn = PickNextEnemyToSpawn(ref gruntsLeft, ref saboteursLeft, ref assassinsLeft);
            EnemyDataSO enemyData = EnemyDataSO.CreateDefault(kindToSpawn);
            
            Vector3 spawnPos = gridManager.waypoints[0].position;
            EnemyController.SpawnEnemy(enemyData, waveNumber, spawnPos, gameManager, gridManager, buildController);

            yield return new WaitForSeconds(cfg.spawnInterval);
        }

        isWaveSpawning = false;
    }

    private EnemyKind PickNextEnemyToSpawn(ref int g, ref int s, ref int a)
    {
        int total = g + s + a;
        int roll = Random.Range(0, total);
        if (roll < g) { g--; return EnemyKind.Grunt; }
        if (roll < g + s) { s--; return (Random.value < 0.5f) ? EnemyKind.SpeedSaboteur : EnemyKind.TankSaboteur; }
        a--; return EnemyKind.Assassin;
    }
    #endregion
}