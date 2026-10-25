using System.Collections.Generic;
using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    private Vector2 moveDir;
    private float speed;
    private float maxRange;
    private float damage;
    private bool isManualShot;
    private int remainingPierce;
    private bool hasCloseKnockback;
    private Vector2 originTowerPos;
    private HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();

    //for sabo emp projectile 
    private bool isEMPShot = false;
    private TowerController targetTower;

    public static void SpawnPlayerProjectile(
        Vector3 startPos, Vector2 dir, float speed, float maxRange, float dmg,
        bool isManual, int pierce, bool knockbackClose, Color col, Vector2 towerOrigin)
    {
        GameObject go = new GameObject("Projectile_Slug");
        go.transform.position = startPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        VectorShapeRenderer shape = go.AddComponent<VectorShapeRenderer>();
        shape.shapeType = VectorShapeRenderer.ShapeType.RegularPolygon;
        shape.polygonSides = 4;
        shape.radius = 0.09f;
        shape.lineWidth = 0.035f;
        shape.baseColor = col;
        shape.animateIdlePulse = false;
        shape.RebuildShape();

        ProjectileController proj = go.AddComponent<ProjectileController>();
        proj.moveDir = dir.normalized;
        proj.speed = speed;
        proj.maxRange = maxRange;
        proj.damage = dmg;
        proj.isManualShot = isManual;
        proj.remainingPierce = pierce;
        proj.hasCloseKnockback = knockbackClose;
        proj.originTowerPos = towerOrigin;
    }

    public static void SpawnEMPProjectile(Vector3 startPos, TowerController target)
    {
        GameObject go = new GameObject("Saboteur_EMP_Shot");
        go.transform.position = startPos;

        VectorShapeRenderer shape = go.AddComponent<VectorShapeRenderer>();
        shape.shapeType = VectorShapeRenderer.ShapeType.RegularPolygon;
        shape.polygonSides = 3;
        shape.radius = 0.11f;
        shape.lineWidth = 0.035f;
        shape.baseColor = new Color(1f, 0.95f, 0.2f);
        shape.RebuildShape();

        ProjectileController proj = go.AddComponent<ProjectileController>();
        proj.isEMPShot = true;
        proj.targetTower = target;
        proj.speed = 7.5f;
    }

    public static void SpawnVectorBurstVFX(Vector3 pos, Color col, int sparkCount = 6)
    {
        GameObject vfx = new GameObject("VectorBurstVFX");
        vfx.transform.position = pos;
        VectorShapeRenderer s = vfx.AddComponent<VectorShapeRenderer>();
        s.shapeType = VectorShapeRenderer.ShapeType.RegularPolygon;
        s.polygonSides = sparkCount;
        s.radius = 0.35f;
        s.lineWidth = 0.04f;
        s.baseColor = col;
        s.RebuildShape();
        Destroy(vfx, 0.14f);
    }

    void Update()
    {
        if (isEMPShot)
        {
            UpdateEMPShot();
            return;
        }

        UpdatePlayerProjectile();
    }

    private void UpdateEMPShot()
    {
        if (targetTower == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, targetTower.transform.position, speed * Time.deltaTime);
        if (((Vector2)transform.position - (Vector2)targetTower.transform.position).sqrMagnitude <= 0.2f * 0.2f)
        {
            targetTower.ApplyEMPSlow(3f);
            SpawnVectorBurstVFX(transform.position, Color.yellow, 5);
            Destroy(gameObject);
        }
    }

    private void UpdatePlayerProjectile()
    {
        transform.position += (Vector3)(moveDir * speed * Time.deltaTime);

        //retains proper turret range
        if (((Vector2)transform.position - originTowerPos).sqrMagnitude > maxRange * maxRange)
        {
            Destroy(gameObject);
            return;
        }

        List<EnemyController> snapshot = new List<EnemyController>(EnemyController.ActiveEnemies);
        foreach (EnemyController e in snapshot)
        {
            if (e == null || hitEnemies.Contains(e)) continue;
            if (e.IsAssassin && !isManualShot) continue;

            float sqrDist = ((Vector2)transform.position - (Vector2)e.transform.position).sqrMagnitude;
            if (sqrDist <= 0.34f * 0.34f)
            {
                hitEnemies.Add(e);
                e.TakeDamage(damage, isManualShot);
                SpawnVectorBurstVFX(transform.position, Color.white, 4);

                if (hasCloseKnockback && ((Vector2)e.transform.position - originTowerPos).sqrMagnitude <= 1.5f * 1.5f)
                {
                    e.PushBackwardAlongPath(0.4f);
                }

                remainingPierce--;
                if (remainingPierce <= 0)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}