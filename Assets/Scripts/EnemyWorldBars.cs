using UnityEngine;

public class EnemyWorldBars : MonoBehaviour
{
    #region Variables
    private LineRenderer hpBarBg;
    private LineRenderer hpBarFill;
    private LineRenderer latchBarFill;
    private LineRenderer warningLaserLine;
    
    private static Material sharedBarMat;
    private Transform targetTransform;
    #endregion

    #region Initialization
    public void Initialize(Transform targetToFollow)
    {
        targetTransform = targetToFollow;
        
        if (sharedBarMat == null) 
        {
            sharedBarMat = new Material(Shader.Find("Sprites/Default"));
        }

        hpBarBg = CreateWorldBar("HP_BG", 0.05f, new Color(0.15f, 0.15f, 0.15f, 0.85f));
        hpBarFill = CreateWorldBar("HP_Fill", 0.05f, new Color(0.2f, 1f, 0.3f, 1f) * 1.4f);
        
        latchBarFill = CreateWorldBar("Latch_Fill", 0.06f, new Color(1f, 0.85f, 0.1f, 1f) * 1.6f);
        latchBarFill.enabled = false;

        GameObject laserGo = new GameObject("SabotageWarningLaser");
        laserGo.transform.SetParent(targetTransform);
        warningLaserLine = laserGo.AddComponent<LineRenderer>();
        warningLaserLine.material = sharedBarMat;
        warningLaserLine.positionCount = 2;
        warningLaserLine.startWidth = 0.04f;
        warningLaserLine.endWidth = 0.04f;
        warningLaserLine.enabled = false;
    }

    private LineRenderer CreateWorldBar(string barName, float width, Color col)
    {
        GameObject go = new GameObject(barName);
        go.transform.SetParent(targetTransform);
        
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = sharedBarMat;
        lr.positionCount = 2;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 0;
        lr.startColor = col;
        lr.endColor = col;
        
        return lr;
    }
    #endregion

    #region Public Update Methods
    public void UpdateHPBar(float currentHP, float maxHP)
    {
        float barWidth = 0.55f;
        Vector3 basePos = targetTransform.position + new Vector3(0f, 0.48f, -0.1f);
        Vector3 leftEdge = basePos + new Vector3(-barWidth * 0.5f, 0f, 0f);
        Vector3 rightEdge = basePos + new Vector3(barWidth * 0.5f, 0f, 0f);

        hpBarBg.SetPosition(0, leftEdge);
        hpBarBg.SetPosition(1, rightEdge);

        float hpFill = Mathf.InverseLerp(0f, maxHP, currentHP);
        hpBarFill.SetPosition(0, leftEdge);
        hpBarFill.SetPosition(1, Vector3.Lerp(leftEdge, rightEdge, hpFill));
    }

    public void EnableLatchVisuals(bool enable)
    {
        latchBarFill.enabled = enable;
        warningLaserLine.enabled = enable;
    }

    public void UpdateLatchBarAndLaser(float latchProgress, Vector3 laserTargetPos)
    {
        float barWidth = 0.55f;
        Vector3 basePos = targetTransform.position + new Vector3(0f, 0.48f, -0.1f);
        Vector3 leftEdge = basePos + new Vector3(-barWidth * 0.5f, 0f, 0f);
        Vector3 rightEdge = basePos + new Vector3(barWidth * 0.5f, 0f, 0f);
        
        Vector3 latchLeft = leftEdge + new Vector3(0f, 0.12f, 0f);
        Vector3 latchRight = rightEdge + new Vector3(0f, 0.12f, 0f);
        
        latchBarFill.SetPosition(0, latchLeft);
        latchBarFill.SetPosition(1, Vector3.Lerp(latchLeft, latchRight, latchProgress));

        float pulse = Mathf.PingPong(Time.time * 5f, 1f);
        Color laserCol = Color.Lerp(new Color(1f, 0.9f, 0.1f, 0.35f), new Color(1f, 0.2f, 0.1f, 1f), pulse) * 1.6f;
        
        warningLaserLine.startColor = laserCol;
        warningLaserLine.endColor = laserCol;
        warningLaserLine.SetPosition(0, targetTransform.position);
        warningLaserLine.SetPosition(1, laserTargetPos);
    }
    #endregion
}