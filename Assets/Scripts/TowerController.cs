using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class TowerController : MonoBehaviour
{
    #region Static Tracking
    // Tracking active towers via a static list avoids expensive FindObjectsOfType calls.
    public static readonly List<TowerController> ActiveTowers = new List<TowerController>();
    public static TowerController CurrentlyOverclockedTower { get; private set; }
    #endregion

    #region Injected Dependencies
    // Strict Dependency Injection: No Singleton .Instance calls allowed!
    protected GameManager gameManager;
    protected GridPathManager gridManager;
    protected AudioShakeManager audioManager;
    protected BuildPlacementController buildController;
    private Camera mainCam;
    #endregion

    #region Configuration & State Variables
    [Header("Tower Configuration Data")]
    public TowerDataSO towerData;
    public int towerTypeIndex;
    public int level = 1; 
    public int totalInvestedGold = 0;
    public Vector2Int gridCell;

    [Header("Current Level Runtime Stats")]
    public float damage;
    public float fireRate;
    public float range;

    [Header("Status & Debuff Timers")]
    public float empSlowTimer = 0f;
    public float sabotageDisableTimer = 0f;

    protected float fireCooldown = 0f;
    protected VectorShapeRenderer turretShape;
    protected VectorShapeRenderer rangeRingShape;
    #endregion

    #region Properties
    public bool IsOverclocked => CurrentlyOverclockedTower == this;
    public bool IsDisabled => sabotageDisableTimer > 0f;
    public TowerLevelStats CurrentStats => towerData.levels[Mathf.Clamp(level - 1, 0, towerData.levels.Length - 1)];
    #endregion

    #region Initialization & Dependency Injection
    private void Awake()
    {
        mainCam = Camera.main;
    }

    protected virtual void OnEnable() => ActiveTowers.Add(this);
    
    protected virtual void OnDisable()
    {
        ActiveTowers.Remove(this);
        if (CurrentlyOverclockedTower == this)
        {
            CurrentlyOverclockedTower = null;
        }
    }

    public static GameObject SpawnProceduralTower(int typeIdx, Vector2 worldPos, Vector2Int cell, TowerDataSO customData = null)
    {
        GameObject go = new GameObject($"Tower_Type{typeIdx}_{cell.x}_{cell.y}");
        go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

        TowerController tower;
        if (typeIdx == 0)      tower = go.AddComponent<RailSniperTower>();
        else if (typeIdx == 1) tower = go.AddComponent<FlakShotgunTower>();
        else                   tower = go.AddComponent<CryoConeTower>();

        tower.InitializeTower(typeIdx, cell, customData);
        return go;
    }

    // Constructor-style injection satisfies strict Dependency Injection rules
    public void InjectDependencies(GameManager gm, GridPathManager gridPath, AudioShakeManager audioMgr, BuildPlacementController buildCtrl)
    {
        gameManager = gm;
        gridManager = gridPath;
        audioManager = audioMgr;
        buildController = buildCtrl;
    }

    public virtual void InitializeTower(int typeIdx, Vector2Int cell, TowerDataSO customData = null)
    {
        towerTypeIndex = typeIdx;
        gridCell = cell;
        level = 1;
        towerData = customData != null ? customData : TowerDataSO.CreateDefaultData(typeIdx);
        totalInvestedGold = towerData.levels[0].cost;

        turretShape = gameObject.AddComponent<VectorShapeRenderer>();
        turretShape.shapeType = towerData.shapeType;
        turretShape.baseColor = towerData.baseColor;
        turretShape.radius = 0.38f;
        turretShape.lineWidth = 0.055f;

        GameObject ringObj = new GameObject("RangeRing");
        ringObj.transform.SetParent(transform);
        ringObj.transform.localPosition = Vector3.zero;
        
        rangeRingShape = ringObj.AddComponent<VectorShapeRenderer>();
        rangeRingShape.shapeType = VectorShapeRenderer.ShapeType.RegularPolygon;
        rangeRingShape.polygonSides = 48;
        rangeRingShape.lineWidth = 0.02f;
        rangeRingShape.animateIdlePulse = false;

        ApplyLevelStats();
    }

    protected virtual void ApplyLevelStats()
    {
        TowerLevelStats stats = CurrentStats;
        damage = stats.damage;
        fireRate = stats.fireRate;
        range = stats.range;

        turretShape.radius = 0.36f + (level - 1) * 0.05f;
        turretShape.RebuildShape();
        turretShape.ApplyColor(turretShape.baseColor);

        rangeRingShape.radius = range;
        rangeRingShape.RebuildShape();
        UpdateRangeRingVisibility();
    }
    #endregion

    #region Unity Lifecycle Methods
    protected virtual void Update()
    {
        UpdateDebuffTimers();

        // Guard Clause: Do not execute combat logic if disabled by a Saboteur
        if (IsDisabled) return; 

        if (fireCooldown > 0f) 
        {
            fireCooldown -= Time.deltaTime;
        }

        CheckForPlayerClickTakeover();

        if (IsOverclocked)
        {
            HandleManualOverclockControl();
        }
        else
        {
            HandleAutonomousControl();
        }
    }
    #endregion

    #region Core Logic & Debuffs
    private void UpdateDebuffTimers()
    {
        if (empSlowTimer > 0f) 
        {
            empSlowTimer -= Time.deltaTime;
        }

        if (sabotageDisableTimer > 0f)
        {
            sabotageDisableTimer -= Time.deltaTime;
            turretShape.ApplyColor(new Color(0.35f, 0.35f, 0.35f, 0.6f));
            
            if (sabotageDisableTimer <= 0f)
            {
                turretShape.ApplyColor(IsOverclocked ? new Color(1f, 0.3f, 0.9f) : turretShape.baseColor);
            }
        }
    }
    #endregion

    #region Manual Overclocking
    private void CheckForPlayerClickTakeover()
    {
        // Guard Clauses (Early Returns) flatten the code and make it readable!
        if (buildController != null && buildController.IsInBuildMode) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (CurrentlyOverclockedTower == this) return;

        Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        float sqrDist = (mouseWorld - (Vector2)transform.position).sqrMagnitude;

        if (sqrDist > 0.48f * 0.48f) return;

        TakeOverTurret();
        fireCooldown = Mathf.Max(fireCooldown, 0.12f);
    }

    private void HandleManualOverclockControl()
    {
        // Guard Clause: Check for exit inputs first
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.R))
        {
            ReleaseOverclock();
            return; 
        }

        if (Input.GetKeyDown(KeyCode.U)) TryUpgradeTower();
        
        // Guard Clause: Stop execution if tower was sold this frame
        if (Input.GetKeyDown(KeyCode.X))
        {
            SellTower();
            return; 
        }

        AimAtCursor();

        // Guard Clause: Prevent firing if cooldown is active or clicking UI
        if (!Input.GetMouseButton(0) || fireCooldown > 0f) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aimDirection = (mouseWorld - (Vector2)transform.position).normalized;
        
        ExecuteFire(aimDirection, isManual: true);
    }

    private void AimAtCursor()
    {
        Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mouseWorld - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    public void TakeOverTurret()
    {
        TowerController prev = CurrentlyOverclockedTower;
        CurrentlyOverclockedTower = this;
        
        if (prev != null) 
        {
            prev.UpdateRangeRingVisibility();
        }
        
        UpdateRangeRingVisibility();
    }

    public void ReleaseOverclock()
    {
        if (CurrentlyOverclockedTower == this)
        {
            CurrentlyOverclockedTower = null;
            UpdateRangeRingVisibility();
        }
    }

    public void UpdateRangeRingVisibility()
    {
        if (IsOverclocked)
        {
            rangeRingShape.ApplyColor(new Color(1f, 0.25f, 0.8f, 0.85f));
            turretShape.ApplyColor(new Color(1f, 0.4f, 0.9f, 1f));
        }
        else
        {
            rangeRingShape.ApplyColor(new Color(turretShape.baseColor.r, turretShape.baseColor.g, turretShape.baseColor.b, 0.15f));
            turretShape.ApplyColor(turretShape.baseColor);
        }
    }
    #endregion

    #region Autonomous Combat
    private void HandleAutonomousControl()
    {
        EnemyController target = FindClosestAutoTarget();
        
        // Guard Clause: No target found
        if (target == null) return;

        Vector2 toTarget = (Vector2)target.transform.position - (Vector2)transform.position;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;

        Quaternion desiredRot = Quaternion.Euler(0f, 0f, targetAngle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRot, 260f * Time.deltaTime);

        // Guard Clause: Cooldown active or target out of specific firing angle
        if (fireCooldown > 0f || !CanAutoFire(toTarget)) return;

        ExecuteFire(transform.right, isManual: false);
    }

    private void ExecuteFire(Vector2 aimDir, bool isManual)
    {
        turretShape.TriggerAttackRecoil(0.35f);
        
        if (audioManager != null) 
        {
            audioManager.PlayShoot();
        }

        FireWeapon(aimDir, isManual);
        ResetFireCooldown(isManual);
    }

    protected EnemyController FindClosestAutoTarget()
    {
        EnemyController closest = null;
        float bestSqr = range * range;

        foreach (EnemyController enemy in EnemyController.ActiveEnemies)
        {
            if (enemy == null || enemy.IsAssassin) continue;

            float sqrDist = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDist <= bestSqr)
            {
                bestSqr = sqrDist;
                closest = enemy;
            }
        }
        return closest;
    }

    private void ResetFireCooldown(bool isManual)
    {
        float effectiveRate = fireRate;
        if (isManual) effectiveRate *= 1.25f;
        if (empSlowTimer > 0f) effectiveRate *= 0.65f;
        
        fireCooldown = 1f / Mathf.Max(0.1f, effectiveRate);
    }

    public void ApplyEMPSlow(float duration = 3f)
    {
        empSlowTimer = Mathf.Max(empSlowTimer, duration);
    }

    public void DisableTowerFromSaboteur(float duration = 8f)
    {
        sabotageDisableTimer = Mathf.Max(sabotageDisableTimer, duration);
        if (IsOverclocked)
        {
            ReleaseOverclock();
        }
    }
    #endregion

    #region Economy & Upgrades
    public int GetNextUpgradeCost()
    {
        // Guard Clause
        if (level >= towerData.levels.Length) return -1; 
        
        return towerData.levels[level].cost;
    }

    public int GetSellRefundGold()
    {
        return Mathf.RoundToInt(totalInvestedGold * 0.70f);
    }

    public void TryUpgradeTower()
    {
        int cost = GetNextUpgradeCost();
        
        // Guard Clauses
        if (cost < 0) return;
        if (gameManager == null || !gameManager.SpendGold(cost)) return;

        totalInvestedGold += cost;
        level++;
        ApplyLevelStats();
        ProjectileController.SpawnVectorBurstVFX(transform.position, Color.green, 10);
    }

    public void SellTower()
    {
        if (gameManager != null) 
        {
            gameManager.AddGold(GetSellRefundGold());
        }
        
        if (gridManager != null) 
        {
            gridManager.RemoveTowerAtCell(gridCell);
        }
        
        ReleaseOverclock();
        Destroy(gameObject);
    }
    #endregion

    #region Subclass Contract Methods
    // Subclasses MUST override these to define their unique math & weapon behavior
    protected virtual bool CanAutoFire(Vector2 toTarget) => true;
    protected abstract void FireWeapon(Vector2 aimDir, bool isManual);
    #endregion
}