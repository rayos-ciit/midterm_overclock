using System.Collections.Generic;
using UnityEngine;

public class GridPathManager : MonoBehaviour
{
    #region Configuration
    [Header("16x9 Grid Settings")]
    public int gridWidth = 16;
    public int gridHeight = 9;
    public float cellSize = 1f;
    public Vector2 gridOrigin = new Vector2(-8f, -4.5f);

    [Header("Waypoints (Drag Empty Transforms Here)")]
    public Transform[] waypoints;

    [Header("Visual Colors")]
    public Color gridLineColor = new Color(0.1f, 0.25f, 0.35f, 0.35f);
    public Color neonPathColor = new Color(0f, 1f, 0.9f, 1f);
    public Color baseCoreColor = new Color(0.2f, 0.9f, 1f, 1f);
    #endregion

    #region State Variables
    private HashSet<Vector2Int> pathCells = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, GameObject> towerCells = new Dictionary<Vector2Int, GameObject>();

    private static Material lineMat;
    #endregion

    #region Initialization
    private void Awake()
    {
        if (lineMat == null)
        {
            lineMat = new Material(Shader.Find("Sprites/Default"));
        }

        DrawGridLines();
        BuildNeonPathAndBlockTiles();
    }
    #endregion

    #region Grid Math & Validation
    public Vector2Int WorldToCell(Vector2 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y - gridOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    public Vector2 CellToWorldCenter(Vector2Int cell)
    {
        float x = gridOrigin.x + (cell.x + 0.5f) * cellSize;
        float y = gridOrigin.y + (cell.y + 0.5f) * cellSize;
        return new Vector2(x, y);
    }

    public bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;
    }

    public bool CanBuildAtCell(Vector2Int cell)
    {
        return IsInsideGrid(cell) && !pathCells.Contains(cell) && !towerCells.ContainsKey(cell);
    }
    #endregion

    #region Tower Registration
    public void RegisterTowerAtCell(Vector2Int cell, GameObject towerObj)
    {
        towerCells[cell] = towerObj;
    }

    public void RemoveTowerAtCell(Vector2Int cell)
    {
        if (towerCells.ContainsKey(cell))
        {
            towerCells.Remove(cell);
        }
    }

    public GameObject GetTowerAtCell(Vector2Int cell)
    {
        towerCells.TryGetValue(cell, out GameObject tower);
        return tower;
    }
    #endregion

    #region Procedural Visuals
    private void DrawGridLines()
    {
        GameObject gridVisualContainer = new GameObject("GridLines_Container");
        gridVisualContainer.transform.SetParent(transform);

        //vertical
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y, 0.1f);
            Vector3 end = new Vector3(gridOrigin.x + x * cellSize, gridOrigin.y + gridHeight * cellSize, 0.1f);
            CreateLine($"GridV_{x}", start, end, 0.02f, gridLineColor, gridVisualContainer.transform);
        }

        //horizontal
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = new Vector3(gridOrigin.x, gridOrigin.y + y * cellSize, 0.1f);
            Vector3 end = new Vector3(gridOrigin.x + gridWidth * cellSize, gridOrigin.y + y * cellSize, 0.1f);
            CreateLine($"GridH_{y}", start, end, 0.02f, gridLineColor, gridVisualContainer.transform);
        }
    }

    private void BuildNeonPathAndBlockTiles()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        DrawNeonPathLine();
        CalculatePathBlockers();
        DrawBaseCoreNode();
    }

    private void DrawNeonPathLine()
    {
        GameObject pathObj = new GameObject("NeonWaypointPath");
        pathObj.transform.SetParent(transform);
        
        LineRenderer lr = pathObj.AddComponent<LineRenderer>();
        lr.material = lineMat;
        lr.positionCount = waypoints.Length;
        lr.startWidth = 0.22f;
        lr.endWidth = 0.22f;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;

        Color hdrPath = neonPathColor * 1.6f;
        lr.startColor = hdrPath;
        lr.endColor = hdrPath;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Vector3 wpPos = waypoints[i].position;
            wpPos.z = 0.05f;
            lr.SetPosition(i, wpPos);
        }
    }

    private void CalculatePathBlockers()
    {
        pathCells.Clear();
        
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector2 a = waypoints[i].position;
            Vector2 b = waypoints[i + 1].position;
            float dist = (b - a).magnitude;
            int steps = Mathf.CeilToInt(dist / (cellSize * 0.2f));

            for (int s = 0; s <= steps; s++)
            {
                Vector2 samplePoint = Vector2.Lerp(a, b, s / (float)Mathf.Max(1, steps));
                Vector2Int cell = WorldToCell(samplePoint);
                
                if (IsInsideGrid(cell))
                {
                    pathCells.Add(cell);
                }
            }
        }
    }

    private void DrawBaseCoreNode()
    {
        GameObject baseVisual = new GameObject("BaseCore_Visual");
        baseVisual.transform.SetParent(transform);
        baseVisual.transform.position = waypoints[waypoints.Length - 1].position;
        
        VectorShapeRenderer baseShape = baseVisual.AddComponent<VectorShapeRenderer>();
        baseShape.shapeType = VectorShapeRenderer.ShapeType.RegularPolygon;
        baseShape.polygonSides = 8;
        baseShape.radius = 0.55f;
        baseShape.lineWidth = 0.07f;
        baseShape.baseColor = baseCoreColor;
        baseShape.RebuildShape();
    }

    private void CreateLine(string lineName, Vector3 start, Vector3 end, float width, Color col, Transform parent)
    {
        GameObject go = new GameObject(lineName);
        go.transform.SetParent(parent);
        
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = lineMat;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = width;
        lr.endWidth = width;
        lr.startColor = col;
        lr.endColor = col;
    }
    #endregion
}