using UnityEngine;

[System.Serializable]
public struct TowerLevelStats
{
    public int cost;
    public float damage;
    public float fireRate;
    public float range;
    public int shotgunPellets;   // for shotgun
    public float cryoConeAngle;  // for cryocone angle
    public float cryoSlowAmount; // for cryocone slow
}

[CreateAssetMenu(fileName = "TowerData", menuName = "Overclock/Tower Data")]
public class TowerDataSO : ScriptableObject
{
    public string towerName = "Tower";
    public VectorShapeRenderer.ShapeType shapeType = VectorShapeRenderer.ShapeType.TurretSniper;
    public Color baseColor = Color.cyan;
    public TowerLevelStats[] levels = new TowerLevelStats[3];

    //fallback
    public static TowerDataSO CreateDefaultData(int typeIndex)
    {
        TowerDataSO data = CreateInstance<TowerDataSO>();

        if (typeIndex == 0) //rail sniper name,type,color and levels with their stat changes
        {
            data.towerName = "Rail Sniper";
            data.shapeType = VectorShapeRenderer.ShapeType.TurretSniper;
            data.baseColor = new Color(0.2f, 0.95f, 1f);
            data.levels = new TowerLevelStats[3]
            {
                new TowerLevelStats { cost = 100, damage = 45f,  fireRate = 0.8f,  range = 7.0f },
                new TowerLevelStats { cost = 80,  damage = 75f,  fireRate = 1.0f,  range = 8.5f },
                new TowerLevelStats { cost = 120, damage = 120f, fireRate = 1.25f, range = 10.0f }
            };
        }
        else if (typeIndex == 1) //flak shotgun name,type,color and levels with their stat changes
        {
            data.towerName = "Flak Shotgun";
            data.shapeType = VectorShapeRenderer.ShapeType.TurretShotgun;
            data.baseColor = new Color(1f, 0.75f, 0.2f);
            data.levels = new TowerLevelStats[3]
            {
                new TowerLevelStats { cost = 120, damage = 12f, fireRate = 1.0f, range = 3.5f, shotgunPellets = 3 },
                new TowerLevelStats { cost = 90,  damage = 16f, fireRate = 1.2f, range = 4.0f, shotgunPellets = 4 },
                new TowerLevelStats { cost = 140, damage = 22f, fireRate = 1.5f, range = 4.5f, shotgunPellets = 5 }
            };
        }
        else //cyrocone name,type,color and levels with their stat changes
        {
            data.towerName = "Cryo Cone";
            data.shapeType = VectorShapeRenderer.ShapeType.TurretCryo;
            data.baseColor = new Color(0.35f, 0.55f, 1f);
            data.levels = new TowerLevelStats[3]
            {
                new TowerLevelStats { cost = 130, damage = 15f, fireRate = 1.0f, range = 4.0f, cryoConeAngle = 45f, cryoSlowAmount = 0.30f },
                new TowerLevelStats { cost = 100, damage = 25f, fireRate = 1.2f, range = 4.5f, cryoConeAngle = 55f, cryoSlowAmount = 0.40f },
                new TowerLevelStats { cost = 150, damage = 40f, fireRate = 1.5f, range = 5.0f, cryoConeAngle = 65f, cryoSlowAmount = 0.50f }
            };
        }

        return data;
    }
}