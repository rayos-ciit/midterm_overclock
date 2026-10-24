using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VectorShapeRenderer : MonoBehaviour
{
    public enum ShapeType
    {
        RegularPolygon, // 3 = Speed Saboteur (Triangle), 4 = Grunt (Diamond), 6 = Tank Saboteur (Hexagon), 40 = Circle/Ring
        StealthChevron, // Assassin dart shape
        TurretSniper,   // Circle base + long rail barrel
        TurretShotgun,  // Octagon base + wide double barrel
        TurretCryo,     // Emitter base + wide frost nozzle
        FOVCone         // Draws the Cryo Cone area of effect
    }

    [Header("Shape Settings")]
    public ShapeType shapeType = ShapeType.RegularPolygon;
    [Range(3, 64)] public int polygonSides = 4;
    public float radius = 0.4f;
    public float lineWidth = 0.05f;
    public Color baseColor = Color.cyan;
    [Range(1f, 3f)] public float glowMultiplier = 1.6f; // Drives URP Bloom glow
    public float fovConeAngle = 45f; // Used when shapeType == FOVCone

    [Header("Math Animation Settings (Idle, Move, Attack)")]
    public bool animateIdlePulse = true;
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.06f;

    [HideInInspector] public bool isMoving = false;
    public float moveWobbleSpeed = 10f;
    public float moveWobbleAngle = 8f;

    private LineRenderer lr;
    private float recoilOffset = 0f;
    private float hitFlashTimer = 0f;
    private static Material sharedLineMat;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;

        if (sharedLineMat == null)
        {
            sharedLineMat = new Material(Shader.Find("Sprites/Default"));
        }
        lr.material = sharedLineMat;

        RebuildShape();
        ApplyColor(baseColor);
    }

    void Update()
    {
        // 1. Frame-rate independent recoil recovery (from Frame-Rate Independent Lerp slide)
        float t = 1f - Mathf.Pow(0.001f, Time.deltaTime);
        recoilOffset = Mathf.Lerp(recoilOffset, 0f, t);

        //Idle & Move Scale/Wobble Animations using Mathf.Sin
        float currentScale = 1f;
        if (animateIdlePulse)
        {
            currentScale += Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        }

        // Apply slight recoil pushback along local -X (barrel axis)
        transform.localScale = new Vector3(currentScale - recoilOffset * 0.3f, currentScale + recoilOffset * 0.15f, 1f);

        // 3. Hit Flash feedback recovery
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            if (hitFlashTimer <= 0f)
            {
                ApplyColor(baseColor);
            }
        }
    }

    public void TriggerAttackRecoil(float strength = 0.35f)
    {
        recoilOffset = strength;
    }

    public void TriggerHitFlash(float duration = 0.08f)
    {
        hitFlashTimer = duration;
        ApplyColor(Color.white * 2f);
    }

    public void ApplyColor(Color c)
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        Color hdrColor = new Color(c.r * glowMultiplier, c.g * glowMultiplier, c.b * glowMultiplier, c.a);
        lr.startColor = hdrColor;
        lr.endColor = hdrColor;
    }

    public void RebuildShape()
    {
        if (lr == null) lr = GetComponent<LineRenderer>();
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        switch (shapeType)
        {
            case ShapeType.RegularPolygon:
                DrawPolygon(polygonSides, radius);
                break;
            case ShapeType.StealthChevron:
                DrawChevron(radius);
                break;
            case ShapeType.TurretSniper:
                DrawTurretBarrel(radius, barrelLength: radius * 2.2f, barrelWidth: radius * 0.22f);
                break;
            case ShapeType.TurretShotgun:
                DrawTurretBarrel(radius, barrelLength: radius * 1.4f, barrelWidth: radius * 0.55f);
                break;
            case ShapeType.TurretCryo:
                DrawCryoTurret(radius);
                break;
            case ShapeType.FOVCone:
                DrawFOVCone(radius, fovConeAngle, 16);
                break;
        }
    }

    // Uses x = r * Cos(rad), y = r * Sin(rad) from your Week 1/Midterm slides
    private void DrawPolygon(int sides, float r)
    {
        lr.loop = true;
        lr.positionCount = sides;
        for (int i = 0; i < sides; i++)
        {
            float rad = (i / (float)sides) * Mathf.PI * 2f;
            float x = Mathf.Cos(rad) * r;
            float y = Mathf.Sin(rad) * r;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private void DrawChevron(float r)
    {
        lr.loop = true;
        lr.positionCount = 4;
        lr.SetPosition(0, new Vector3(r * 1.2f, 0f, 0f));       // Nose (+X forward)
        lr.SetPosition(1, new Vector3(-r * 0.9f, r * 0.8f, 0f)); // Top wing
        lr.SetPosition(2, new Vector3(-r * 0.4f, 0f, 0f));       // Inner notch
        lr.SetPosition(3, new Vector3(-r * 0.9f, -r * 0.8f, 0f));// Bottom wing
    }

    private void DrawTurretBarrel(float r, float barrelLength, float barrelWidth)
    {
        lr.loop = true;
        int bodyPoints = 12;
        lr.positionCount = bodyPoints + 4;

        // Draw circular turret base
        for (int i = 0; i < bodyPoints; i++)
        {
            float rad = (i / (float)bodyPoints) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f));
        }
        // Extend barrel pointing right (+X)
        lr.SetPosition(bodyPoints,     new Vector3(r * 0.5f, barrelWidth, 0f));
        lr.SetPosition(bodyPoints + 1, new Vector3(barrelLength, barrelWidth, 0f));
        lr.SetPosition(bodyPoints + 2, new Vector3(barrelLength, -barrelWidth, 0f));
        lr.SetPosition(bodyPoints + 3, new Vector3(r * 0.5f, -barrelWidth, 0f));
    }

    private void DrawCryoTurret(float r)
    {
        lr.loop = true;
        lr.positionCount = 8;
        // Hexagonal core with flared nozzle
        lr.SetPosition(0, new Vector3(-r, r * 0.5f, 0f));
        lr.SetPosition(1, new Vector3(0f, r * 0.8f, 0f));
        lr.SetPosition(2, new Vector3(r * 1.2f, r * 0.9f, 0f));
        lr.SetPosition(3, new Vector3(r * 0.8f, 0f, 0f));
        lr.SetPosition(4, new Vector3(r * 1.2f, -r * 0.9f, 0f));
        lr.SetPosition(5, new Vector3(0f, -r * 0.8f, 0f));
        lr.SetPosition(6, new Vector3(-r, -r * 0.5f, 0f));
        lr.SetPosition(7, new Vector3(-r * 1.2f, 0f, 0f));
    }

    public void DrawFOVCone(float range, float angleDeg, int arcSegments = 16)
    {
        lr.loop = true;
        lr.positionCount = arcSegments + 2;
        lr.SetPosition(0, Vector3.zero); // Origin at turret center

        float halfAngle = angleDeg * 0.5f;
        for (int i = 0; i <= arcSegments; i++)
        {
            float currentDeg = Mathf.Lerp(-halfAngle, halfAngle, i / (float)arcSegments);
            float rad = currentDeg * Mathf.Deg2Rad;
            lr.SetPosition(i + 1, new Vector3(Mathf.Cos(rad) * range, Mathf.Sin(rad) * range, 0f));
        }
    }
}