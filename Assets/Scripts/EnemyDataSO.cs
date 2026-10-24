using UnityEngine;

public enum EnemyKind
{
    Grunt,
    SpeedSaboteur,
    TankSaboteur,
    Assassin
}

[System.Serializable]
public struct EnemyStats
{
    public float baseHP;
    public float baseSpeed;
    public int baseGold;
    public int baseDamage;
    public float latchDuration;
    
    [Header("Visuals")]
    public VectorShapeRenderer.ShapeType shapeType;
    public int polygonSides;
    public float radius;
    public Color color;
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Overclock/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    public EnemyKind kind;
    public EnemyStats stats;

    // Fallback factory to auto-generate exact TDD stats if Inspector assets aren't set up yet
    public static EnemyDataSO CreateDefault(EnemyKind type)
    {
        EnemyDataSO data = CreateInstance<EnemyDataSO>();
        data.kind = type;

        if (type == EnemyKind.Grunt)
        {
            data.stats = new EnemyStats { baseHP = 60f, baseSpeed = 2.0f, baseGold = 15, baseDamage = 10, latchDuration = 0f, 
                shapeType = VectorShapeRenderer.ShapeType.RegularPolygon, polygonSides = 4, radius = 0.3f, color = new Color(1f, 0.25f, 0.25f) };
        }
        else if (type == EnemyKind.SpeedSaboteur)
        {
            data.stats = new EnemyStats { baseHP = 45f, baseSpeed = 3.2f, baseGold = 20, baseDamage = 5, latchDuration = 5f, 
                shapeType = VectorShapeRenderer.ShapeType.RegularPolygon, polygonSides = 3, radius = 0.28f, color = new Color(1f, 0.9f, 0.15f) };
        }
        else if (type == EnemyKind.TankSaboteur)
        {
            data.stats = new EnemyStats { baseHP = 160f, baseSpeed = 1.1f, baseGold = 30, baseDamage = 10, latchDuration = 5f, 
                shapeType = VectorShapeRenderer.ShapeType.RegularPolygon, polygonSides = 6, radius = 0.38f, color = new Color(1f, 0.5f, 0.1f) };
        }
        else // Assassin
        {
            data.stats = new EnemyStats { baseHP = 75f, baseSpeed = 2.4f, baseGold = 35, baseDamage = 15, latchDuration = 4f, 
                shapeType = VectorShapeRenderer.ShapeType.StealthChevron, polygonSides = 4, radius = 0.32f, color = new Color(0.8f, 0.2f, 1f) };
        }
        return data;
    }
}