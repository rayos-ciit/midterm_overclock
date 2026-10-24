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
    private bool isWaveSpawning = false;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        // Guard Clauses
        if (gameManager == null || !gameManager.IsGameActive || gameManager.IsPaused) return;
        if (isWaveSpawning) return;

        CheckWaveCompletion();
    }
    #endregion

    #region Spawning Logic
    private void CheckWaveCompletion()
    {
        // Advance wave when all enemies in the current wave are eliminated
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
    }

    private void StartNextWave()
    {
        WaveConfig[] waves = gameManager.config.waves;
        
        // Guard Clause
        if (CurrentWaveIndex >= waves.Length) return;
        
        StartCoroutine(SpawnWaveCoroutine(waves[CurrentWaveIndex], CurrentWaveIndex + 1));
    }

    private IEnumerator SpawnWaveCoroutine(WaveConfig cfg, int waveNumber)
    {
        isWaveSpawning = true;
        int gruntsLeft = cfg.grunts;
        int saboteursLeft = cfg.saboteurs;
        int assassinsLeft = cfg.assassins;
        int totalToSpawn = gruntsLeft + saboteursLeft + assassinsLeft;

        // Brief breather before the wave actually starts
        yield return new WaitForSeconds(2.0f);

        for (int i = 0; i < totalToSpawn; i++)
        {
            // Pause spawning if the game state changes (e.g., player dies mid-wave)
            while (gameManager != null && !gameManager.IsGameActive)
            {
                yield return null;
            }

            EnemyKind kindToSpawn = PickNextEnemyToSpawn(ref gruntsLeft, ref saboteursLeft, ref assassinsLeft);
            EnemyDataSO enemyData = EnemyDataSO.CreateDefault(kindToSpawn); // Factory fetches the stats
            
            // Pass the dependencies down safely (Strict DI)
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
        
        if (roll < g)
        {
            g--;
            return EnemyKind.Grunt;
        }
        
        if (roll < g + s)
        {
            s--;
            // TDD Rule: 50/50 mutually exclusive roll between Speed Saboteur and Tank Saboteur
            return (Random.value < 0.5f) ? EnemyKind.SpeedSaboteur : EnemyKind.TankSaboteur;
        }
        
        a--;
        return EnemyKind.Assassin;
    }
    #endregion
}