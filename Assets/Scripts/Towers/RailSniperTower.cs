using UnityEngine;

public class RailSniperTower : TowerController
{
    #region Combat Overrides
    protected override bool CanAutoFire(Vector2 toTarget)
    {
        Vector2 barrel = transform.right;
        float dot = Vector2.Dot(barrel.normalized, toTarget.normalized);
        return dot >= 0.98f;
    }

    protected override void FireWeapon(Vector2 aimDir, bool isManual)
    {
        int pierceCount = (level >= 3) ? 2 : 1;

        ProjectileController.SpawnPlayerProjectile(
            startPos: transform.position + (Vector3)(aimDir * 0.45f),
            dir: aimDir,
            speed: 18f,
            maxRange: range,
            dmg: damage,
            isManual: isManual,
            pierce: pierceCount,
            knockbackClose: false,
            col: new Color(0.2f, 1f, 1f),
            towerOrigin: transform.position
        );
    }
    #endregion
}