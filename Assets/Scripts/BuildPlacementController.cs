using UnityEngine;
using UnityEngine.EventSystems;

public class BuildPlacementController : MonoBehaviour
{
    #region Dependencies (Strict Dependency Injection)
    // By using [SerializeField], we inject these via the Inspector. 
    // ZERO .Instance calls and ZERO FindObjectOfType calls!
    [Header("Injected Managers")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private GridPathManager gridManager;
    [SerializeField] private AudioShakeManager audioManager;
    
    private Camera mainCamera;
    #endregion

    #region Data Structures
    [System.Serializable]
    public class TowerBuildOption
    {
        public string towerName;
        public int level1Cost = 100;
        public float level1Range = 7f;
        public VectorShapeRenderer.ShapeType previewShape;
        public Color towerColor = Color.cyan;
    }
    #endregion

    #region Configuration
    [Header("Tower Definitions (Matches TDD)")]
    public TowerBuildOption[] towerOptions = new TowerBuildOption[3]
    {
        new TowerBuildOption { towerName = "Rail Sniper",  level1Cost = 100, level1Range = 7.0f, previewShape = VectorShapeRenderer.ShapeType.TurretSniper,  towerColor = new Color(0.2f, 0.9f, 1f) },
        new TowerBuildOption { towerName = "Flak Shotgun", level1Cost = 120, level1Range = 3.5f, previewShape = VectorShapeRenderer.ShapeType.TurretShotgun, towerColor = new Color(1f, 0.75f, 0.2f) },
        new TowerBuildOption { towerName = "Cryo Cone",    level1Cost = 130, level1Range = 4.0f, previewShape = VectorShapeRenderer.ShapeType.TurretCryo,    towerColor = new Color(0.3f, 0.5f, 1f) }
    };
    #endregion

    #region State Variables
    // Tracks if an Assassin has temporarily disabled a specific hotkey (0, 1, or 2)
    [HideInInspector] public float[] buildButtonDisableTimers = new float[3];

    // Current selected tower index (-1 means not in build mode)
    private int selectedBuildIndex = -1;

    // Ghost Preview rendering objects
    private GameObject ghostRoot;
    private VectorShapeRenderer ghostBodyShape;
    private VectorShapeRenderer ghostRangeRing;
    #endregion

    #region Properties
    public bool IsInBuildMode => selectedBuildIndex != -1;
    #endregion

    #region Initialization & Setup
    private void Awake()
    {
        // Caching Camera.main is a massive performance boost over calling it every frame
        mainCamera = Camera.main; 
        
        CreateGhostPreviewObjects();
        CancelBuildMode();
    }

    // Optional: Public method to inject via code instead of Inspector if desired
    public void InjectDependencies(GameManager gm, GridPathManager grid, AudioShakeManager audio)
    {
        gameManager = gm;
        gridManager = grid;
        audioManager = audio;
    }
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        UpdateSabotageTimers();
        HandleTowerSelectionHotkeys();

        // Guard Clause: Stop executing if we aren't actively trying to place a tower
        if (!IsInBuildMode) return;

        // Guard Clause: Exit build mode on Right-Click or Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelBuildMode();
            return;
        }

        UpdateGhostPreviewAndPlacement();
    }
    #endregion

    #region Input & Sabotage Logic
    private void UpdateSabotageTimers()
    {
        for (int i = 0; i < buildButtonDisableTimers.Length; i++)
        {
            if (buildButtonDisableTimers[i] > 0f)
            {
                buildButtonDisableTimers[i] -= Time.deltaTime;
            }
        }
    }

    private void HandleTowerSelectionHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectTowerToBuild(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectTowerToBuild(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectTowerToBuild(2);
    }

    public void DisableBuildSlotFromAssassin(int slotIndex, float duration = 12f)
    {
        // Guard Clause: Prevent out-of-bounds array access
        if (slotIndex < 0 || slotIndex >= buildButtonDisableTimers.Length) return;

        buildButtonDisableTimers[slotIndex] = duration;

        if (selectedBuildIndex == slotIndex)
        {
            CancelBuildMode();
        }
    }
    #endregion

    #region Build Mode Management
    public void SelectTowerToBuild(int index)
    {
        // Guard Clause: Invalid index bounds
        if (index < 0 || index >= towerOptions.Length) return;

        // Guard Clause: Stop selection if Assassin sabotaged this button
        if (buildButtonDisableTimers[index] > 0f)
        {
            Debug.LogWarning($"{towerOptions[index].towerName} build button is SABOTAGED for {buildButtonDisableTimers[index]:F1}s!");
            return;
        }

        selectedBuildIndex = index;
        TowerBuildOption option = towerOptions[index];

        ghostRoot.SetActive(true);
        
        ghostBodyShape.shapeType = option.previewShape;
        ghostBodyShape.radius = 0.38f;
        ghostBodyShape.RebuildShape();

        ghostRangeRing.shapeType = VectorShapeRenderer.ShapeType.RegularPolygon;
        ghostRangeRing.polygonSides = 48;
        ghostRangeRing.radius = option.level1Range;
        ghostRangeRing.RebuildShape();
    }

    public void CancelBuildMode()
    {
        selectedBuildIndex = -1;
        
        if (ghostRoot != null) 
        {
            ghostRoot.SetActive(false);
        }
    }
    #endregion

    #region Preview Rendering & Placement Execution
    private void UpdateGhostPreviewAndPlacement()
    {
        // Guard Clause: Safety check to ensure Inspector dependencies are assigned
        if (gridManager == null || gameManager == null) return;

        Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int cell = gridManager.WorldToCell(mouseWorld);
        Vector2 snappedCenter = gridManager.CellToWorldCenter(cell);

        ghostRoot.transform.position = new Vector3(snappedCenter.x, snappedCenter.y, -0.1f);

        TowerBuildOption option = towerOptions[selectedBuildIndex];
        
        // Check grid blocking and economy
        bool isTileFree = gridManager.CanBuildAtCell(cell);
        bool canAfford = gameManager.CurrentGold >= option.level1Cost;
        bool isValidPlacement = isTileFree && canAfford;

        Color previewColor = isValidPlacement 
            ? new Color(0.2f, 1f, 0.4f, 0.75f) 
            : new Color(1f, 0.2f, 0.2f, 0.75f);
            
        ghostBodyShape.ApplyColor(previewColor);
        ghostRangeRing.ApplyColor(previewColor);

        // Attempt placement on Left Click
        if (Input.GetMouseButtonDown(0))
        {
            TryExecutePlacement(isValidPlacement, option, snappedCenter, cell);
        }
    }

    private void TryExecutePlacement(bool isValidPlacement, TowerBuildOption option, Vector2 snappedCenter, Vector2Int cell)
    {
        // Guard Clause: Prevent building if the player clicked a UI button
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        
        // Guard Clause: Prevent building if placement is blocked or too expensive
        if (!isValidPlacement) return;

        if (gameManager.SpendGold(option.level1Cost))
        {
            GameObject newTower = TowerController.SpawnProceduralTower(selectedBuildIndex, snappedCenter, cell);
            
            // Pass the injected dependencies directly down into the new tower!
            TowerController towerComponent = newTower.GetComponent<TowerController>();
            if (towerComponent != null)
            {
                towerComponent.InjectDependencies(gameManager, gridManager, audioManager, this);
            }

            gridManager.RegisterTowerAtCell(cell, newTower);
            CancelBuildMode(); // Exit build mode immediately so the next click can Overclock!
        }
    }

    private void CreateGhostPreviewObjects()
    {
        ghostRoot = new GameObject("GhostPlacementPreview");
        
        ghostBodyShape = ghostRoot.AddComponent<VectorShapeRenderer>();
        ghostBodyShape.lineWidth = 0.04f;
        ghostBodyShape.animateIdlePulse = false;

        GameObject ringObj = new GameObject("GhostRangeRing");
        ringObj.transform.SetParent(ghostRoot.transform);
        ringObj.transform.localPosition = Vector3.zero;
        
        ghostRangeRing = ringObj.AddComponent<VectorShapeRenderer>();
        ghostRangeRing.lineWidth = 0.025f;
        ghostRangeRing.animateIdlePulse = false;
    }
    #endregion
}