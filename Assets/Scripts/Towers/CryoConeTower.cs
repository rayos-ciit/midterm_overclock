using System.Collections.Generic;
using UnityEngine;

public class CryoConeTower : TowerController
{
    #region Variables
    private VectorShapeRenderer cryoConeVisual;
    #endregion

    #region Initialization Overrides
    public override void InitializeTower(int typeIdx, Vector2Int cell, TowerDataSO customData = null)
    {
        GameObject coneObj = new GameObject("CryoFOVCone");
        coneObj.transform.SetParent(transform);
        coneObj.transform.localPosition = Vector3.zero;
        coneObj.transform.localRotation = Quaternion.identity;

        cryoConeVisual = coneObj.AddComponent<VectorShapeRenderer>();
        cryoConeVisual.shapeType = VectorShapeRenderer.ShapeType.FOVCone;
        cryoConeVisual.lineWidth = 0.025f;
        cryoConeVisual.animateIdlePulse = false;

        base.InitializeTower(typeIdx, cell, customData);
    }

    protected override void ApplyLevelStats()
    {
        base.ApplyLevelStats();

        // Guard Clause
        if (cryoConeVisual == null) return;
        
        cryoConeVisual.radius = range;
        cryoConeVisual.fovConeAngle = CurrentStats.cryoConeAngle;
        cryoConeVisual.RebuildShape();
        cryoConeVisual.ApplyColor(new Color(0.3f, 0.7f, 1f, 0.25f));
    }
    #endregion

    #region Combat Overrides
    protected override void FireWeapon(Vector2 aimDir, bool isManual)
    {
        float tAngle = transform.eulerAngles.z;
        float coneAngle = CurrentStats.cryoConeAngle;
        float slowFraction = CurrentStats.cryoSlowAmount;

        ProjectileController.SpawnVectorBurstVFX(transform.position + (Vector3)(aimDir * 0.8f), new Color(0.4f, 0.85f, 1f), 6);

        List<EnemyController> snapshot = new List<EnemyController>(EnemyController.ActiveEnemies);
        foreach (EnemyController enemy in snapshot)
        {
            if (enemy == null) continue;
            
            if (IsInCone(transform, enemy.transform, range, coneAngle, tAngle))
            {
                enemy.ApplyCryoSlow(slowFraction, 2.5f);
                enemy.TakeDamage(damage, isManual);
            }
        }
    }
    #endregion

    #region Math Helpers
    private bool IsInCone(Transform turret, Transform enemy, float maxRange, float coneAngle, float tAngle)
    {
        Vector2 dir = (Vector2)enemy.position - (Vector2)turret.position;
        
        // Guard Clause
        if (dir.sqrMagnitude > maxRange * maxRange) return false;

        float eAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float delta = Mathf.Abs(Mathf.DeltaAngle(tAngle, eAngle));
        
        return delta <= coneAngle * 0.5f;
    }
    #endregion
}