using UnityEngine;

public class EnemySabotageBehavior : MonoBehaviour
{
    #region Dependencies & References
    private EnemyController coreEnemy;
    private EnemyDataSO enemyData;
    private GameManager gameManager;
    private BuildPlacementController buildController;
    private EnemyWorldBars worldBars;
    
    private TowerController targetedTurret;
    private int targetedBuildButtonIndex = 0;
    #endregion

    #region Timers & State
    private float empShootTimer = 1.5f;
    private float latchTimer = 0f;
    private bool isLatched = false;
    #endregion

    #region Initialization
    public void Initialize(EnemyController core, EnemyDataSO data, GameManager gm, BuildPlacementController buildCtrl, EnemyWorldBars bars)
    {
        coreEnemy = core;
        enemyData = data;
        gameManager = gm;
        buildController = buildCtrl;
        worldBars = bars;
    }
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        // Guard Clause
        if (coreEnemy == null) return;

        if (isLatched)
        {
            HandleBaseLatchProgression();
        }
        else if (coreEnemy.IsSaboteur)
        {
            HandleEMPShooting();
        }
    }
    #endregion

    #region EMP Logic
    private void HandleEMPShooting()
    {
        empShootTimer -= Time.deltaTime;
        
        // Guard Clause
        if (empShootTimer > 0f) return;

        TowerController nearestTower = FindNearestTower(3.5f);
        
        // Guard Clause
        if (nearestTower == null) return;

        empShootTimer = 2.5f;
        ProjectileController.SpawnEMPProjectile(transform.position, nearestTower);
    }

    private TowerController FindNearestTower(float maxRange)
    {
        TowerController nearest = null;
        float bestSqrDist = maxRange * maxRange;

        foreach (TowerController t in TowerController.ActiveTowers)
        {
            float sqrD = ((Vector2)t.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrD <= bestSqrDist)
            {
                bestSqrDist = sqrD;
                nearest = t;
            }
        }
        return nearest;
    }
    #endregion

    #region Latch Logic
    public void StartBaseLatch()
    {
        isLatched = true;
        latchTimer = 0f;
        
        worldBars.EnableLatchVisuals(true);

        // Fan out around base core
        float offsetAngle = Random.Range(0f, Mathf.PI * 2f);
        transform.position += new Vector3(Mathf.Cos(offsetAngle) * 0.45f, Mathf.Sin(offsetAngle) * 0.45f, 0f);

        if (coreEnemy.IsSaboteur)
        {
            PickRandomTurretToSabotage();
        }
        else if (coreEnemy.IsAssassin)
        {
            targetedBuildButtonIndex = Random.Range(0, 3);
        }
    }

    private void PickRandomTurretToSabotage()
    {
        if (TowerController.ActiveTowers.Count > 0)
        {
            int idx = Random.Range(0, TowerController.ActiveTowers.Count);
            targetedTurret = TowerController.ActiveTowers[idx];
        }
    }

    private void HandleBaseLatchProgression()
    {
        latchTimer += Time.deltaTime;
        
        // Pick new target if previous one was sold/destroyed
        if (coreEnemy.IsSaboteur && targetedTurret == null && TowerController.ActiveTowers.Count > 0)
        {
            PickRandomTurretToSabotage();
        }

        UpdateVisuals();

        // Check if progress bar is full
        if (latchTimer >= enemyData.stats.latchDuration)
        {
            ExecuteSabotageAndExplode();
        }
    }

    private void UpdateVisuals()
    {
        float progress = Mathf.InverseLerp(0f, enemyData.stats.latchDuration, latchTimer);
        Vector3 targetPos = transform.position;

        if (coreEnemy.IsSaboteur && targetedTurret != null)
        {
            targetPos = targetedTurret.transform.position;
        }
        else if (coreEnemy.IsAssassin && gameManager != null)
        {
            targetPos = gameManager.GetBuildButtonWorldPos(targetedBuildButtonIndex);
        }

        worldBars.UpdateLatchBarAndLaser(progress, targetPos);
    }

    private void ExecuteSabotageAndExplode()
    {
        if (coreEnemy.IsSaboteur && targetedTurret != null)
        {
            targetedTurret.DisableTowerFromSaboteur(8f);
        }
        else if (coreEnemy.IsAssassin && buildController != null)
        {
            buildController.DisableBuildSlotFromAssassin(targetedBuildButtonIndex, 12f);
        }

        if (gameManager != null)
        {
            gameManager.DamageBase(enemyData.stats.baseDamage);
        }

        ProjectileController.SpawnVectorBurstVFX(transform.position, Color.yellow, 10);
        Destroy(gameObject);
    }
    #endregion
}