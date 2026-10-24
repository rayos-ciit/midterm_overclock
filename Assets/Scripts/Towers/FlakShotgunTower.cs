using UnityEngine;

public class FlakShotgunTower : TowerController
{
    #region Combat Overrides
    protected override void FireWeapon(Vector2 aimDir, bool isManual)
    {
        int pellets = Mathf.Max(1, CurrentStats.shotgunPellets);
        float baseAngleDeg = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        float spreadStep = 10f; 
        float startOffset = -((pellets - 1) * 0.5f) * spreadStep;

        for (int i = 0; i < pellets; i++)
        {
            float pelletDeg = baseAngleDeg + startOffset + (i * spreadStep);
            float pelletRad = pelletDeg * Mathf.Deg2Rad;
            Vector2 pelletDir = new Vector2(Mathf.Cos(pelletRad), Mathf.Sin(pelletRad));

            ProjectileController.SpawnPlayerProjectile(
                startPos: transform.position + (Vector3)(pelletDir * 0.4f),
                dir: pelletDir,
                speed: 12f,
                maxRange: range,
                dmg: damage,
                isManual: isManual,
                pierce: 1,
                knockbackClose: true, 
                col: new Color(1f, 0.8f, 0.2f),
                towerOrigin: transform.position
            );
        }
    }
    #endregion
}