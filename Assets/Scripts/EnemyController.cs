using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    #region Static Tracking
    public static readonly List<EnemyController> ActiveEnemies = new List<EnemyController>();
    #endregion

    #region Injected Dependencies
    private GameManager gameManager;
    private GridPathManager gridManager;
    private BuildPlacementController buildController;
    #endregion

    #region Properties & Components
    public EnemyDataSO EnemyData { get; private set; }
    public bool IsAssassin => EnemyData != null && EnemyData.kind == EnemyKind.Assassin;
    public bool IsSaboteur => EnemyData != null && (EnemyData.kind == EnemyKind.SpeedSaboteur || EnemyData.kind == EnemyKind.TankSaboteur);
    
    private EnemyWorldBars worldBars;
    private EnemySabotageBehavior sabotageBehavior;
    private VectorShapeRenderer bodyShape;
    #endregion

    #region State Variables
    public float maxHP;
    public float currentHP;
    public float baseSpeed;
    public int goldReward;

    private float slowMultiplier = 1f;
    private float slowTimer = 0f;
    
    private int currentWaypointIndex = 0;
    private bool isLatchedToBase = false;
    #endregion

    #region Initialization & DI
    protected virtual void OnEnable() => ActiveEnemies.Add(this);
    protected virtual void OnDisable() => ActiveEnemies.Remove(this);

    public static EnemyController SpawnEnemy(EnemyDataSO data, int waveNumber, Vector3 spawnPos, GameManager gm, GridPathManager grid, BuildPlacementController buildCtrl)
    {
        GameObject go = new GameObject($"Enemy_{data.kind}_W{waveNumber}");
        go.transform.position = spawnPos;

        EnemyController enemy = go.AddComponent<EnemyController>();
        enemy.InjectDependencies(gm, grid, buildCtrl);
        enemy.InitializeEnemy(data, waveNumber);
        
        return enemy;
    }

    public void InjectDependencies(GameManager gm, GridPathManager grid, BuildPlacementController buildCtrl)
    {
        gameManager = gm;
        gridManager = grid;
        buildController = buildCtrl;
    }

    public void InitializeEnemy(EnemyDataSO data, int waveNumber)
    {
        EnemyData = data;
        
        float hpMult = 1f + 0.22f * (waveNumber - 1);
        float spdMult = 1f + 0.04f * (waveNumber - 1);
        int bonusGold = Mathf.FloorToInt(1.5f * (waveNumber - 1));

        maxHP = data.stats.baseHP * hpMult;
        currentHP = maxHP;
        baseSpeed = data.stats.baseSpeed * spdMult;
        goldReward = data.stats.baseGold + bonusGold;

        SetupVisuals(data.stats);
        
        worldBars = gameObject.AddComponent<EnemyWorldBars>();
        worldBars.Initialize(transform);

        if (IsSaboteur || IsAssassin)
        {
            sabotageBehavior = gameObject.AddComponent<EnemySabotageBehavior>();
            sabotageBehavior.Initialize(this, data, gameManager, buildController, worldBars);
        }
    }

    private void SetupVisuals(EnemyStats stats)
    {
        bodyShape = gameObject.AddComponent<VectorShapeRenderer>();
        bodyShape.shapeType = stats.shapeType;
        bodyShape.polygonSides = stats.polygonSides;
        bodyShape.radius = stats.radius;
        bodyShape.baseColor = stats.color;
        
        bodyShape.RebuildShape();
        bodyShape.ApplyColor(stats.color);
    }
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        UpdateDebuffs();
        
        worldBars.UpdateHPBar(currentHP, maxHP);

        if (isLatchedToBase) return;
        
        HandleWaypointMovement();
    }
    #endregion

    #region Movement & Navigation
    
    public float GetPathProgress()
    {
        if (isLatchedToBase) return 999999f; 
        if (gridManager == null || gridManager.waypoints == null) return 0f;
        if (currentWaypointIndex >= gridManager.waypoints.Length) return 999999f;

        Vector2 targetPos = gridManager.waypoints[currentWaypointIndex].position;
        float sqrDistToNext = (targetPos - (Vector2)transform.position).sqrMagnitude;
        
        return (currentWaypointIndex * 1000f) - sqrDistToNext;
    }

    private void HandleWaypointMovement()
    {
        if (gridManager == null || gridManager.waypoints == null) return;
        if (currentWaypointIndex >= gridManager.waypoints.Length) return;

        Vector3 targetPos = gridManager.waypoints[currentWaypointIndex].position;
        Vector2 direction = (Vector2)targetPos - (Vector2)transform.position;
        float distance = direction.magnitude;

        if (distance <= 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= gridManager.waypoints.Length)
            {
                OnReachedBase();
            }
        }
        else
        {
            MoveAndRotate(direction.normalized);
        }
    }
    
    private void MoveAndRotate(Vector2 moveDir)
    {
        float currentSpeed = baseSpeed * slowMultiplier;
        transform.position += (Vector3)(moveDir * currentSpeed * Time.deltaTime);

        //Mathf.Atan2 + Mathf.Sin
        float baseAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        float walkWobble = Mathf.Sin(Time.time * currentSpeed * 4f) * 6f;
        transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + walkWobble);
    }

    private void OnReachedBase()
    {
        if (EnemyData.kind == EnemyKind.Grunt)
        {
            if (gameManager != null) gameManager.DamageBase(EnemyData.stats.baseDamage);
            Destroy(gameObject);
            return;
        }

        isLatchedToBase = true;
        if (sabotageBehavior != null)
        {
            sabotageBehavior.StartBaseLatch();
        }
    }

    #endregion

    #region Combat & Health
    public void ApplyCryoSlow(float slowFraction, float duration)
    {
        slowMultiplier = Mathf.Min(slowMultiplier, 1f - slowFraction);
        slowTimer = Mathf.Max(slowTimer, duration);
        bodyShape.ApplyColor(new Color(0.4f, 0.85f, 1f));
    }

    private void UpdateDebuffs()
    {
        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                slowMultiplier = 1f;
                bodyShape.ApplyColor(EnemyData.stats.color);
            }
        }
    }

    public void TakeDamage(float rawDamage, bool isFromManualOverclock)
    {
        if (IsAssassin && !isFromManualOverclock) return;

        float finalDamage = rawDamage;
        if (IsSaboteur && isFromManualOverclock)
        {
            finalDamage *= 2f;
        }

        currentHP -= finalDamage;
        bodyShape.TriggerHitFlash();

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    public void PushBackwardAlongPath(float distance)
    {
        if (isLatchedToBase) return;
        if (gridManager == null || currentWaypointIndex <= 0) return;
        if (currentWaypointIndex >= gridManager.waypoints.Length) return;

        Vector2 prevWp = gridManager.waypoints[currentWaypointIndex - 1].position;
        Vector2 currWp = gridManager.waypoints[currentWaypointIndex].position;
        Vector2 pathDir = (currWp - prevWp).normalized;
        
        transform.position -= (Vector3)(pathDir * distance);
    }

    private void Die()
    {
        ProjectileController.SpawnVectorBurstVFX(transform.position, EnemyData.stats.color, 8);

        Vector3 coinTarget = gameManager != null 
            ? gameManager.GetGoldHUDWorldPos() 
            : new Vector3(0f, 5.2f, 0f);
            
        CoinPickupEffect.SpawnCoin(transform.position, goldReward, coinTarget, gameManager);
        
        if (gameManager != null) 
        {
            gameManager.PlayEnemyDeathSound();
        }

        Destroy(gameObject);
    }
    #endregion
}