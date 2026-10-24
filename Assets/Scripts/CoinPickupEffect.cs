using UnityEngine;

public class CoinPickupEffect : MonoBehaviour
{
    [Header("Adjustable Coin Lerp Settings")]
    public static float GlobalCoinLerpSpeed = 6.5f;
    public static float GlobalPopDuration = 0.22f;

    private int goldAmount;
    private Vector3 burstVelocity;
    private Vector3 targetWorldPos;
    private float timer = 0f;
    
    // Strict Dependency Injection reference
    private GameManager gameManager;

    public static void SpawnCoin(Vector3 spawnPos, int gold, Vector3 hudTargetWorldPos, GameManager gm)
    {
        GameObject go = new GameObject("GoldCoin_Lerp");
        go.transform.position = spawnPos;

        VectorShapeRenderer shape = go.AddComponent<VectorShapeRenderer>();
        shape.shapeType = VectorShapeRenderer.ShapeType.RegularPolygon;
        shape.polygonSides = 4;
        shape.radius = 0.18f;
        shape.lineWidth = 0.04f;
        shape.baseColor = new Color(1f, 0.85f, 0.1f, 1f);
        shape.RebuildShape();

        CoinPickupEffect fx = go.AddComponent<CoinPickupEffect>();
        fx.goldAmount = gold;
        fx.targetWorldPos = hudTargetWorldPos;
        fx.gameManager = gm; // Inject the dependency!

        float randRad = Random.Range(0f, Mathf.PI * 2f);
        fx.burstVelocity = new Vector3(Mathf.Cos(randRad), Mathf.Sin(randRad), 0f) * Random.Range(2f, 3.5f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);

        if (timer < GlobalPopDuration)
        {
            transform.position += burstVelocity * Time.deltaTime;
            burstVelocity = Vector3.Lerp(burstVelocity, Vector3.zero, Time.deltaTime * 10f);
            return;
        }

        float decay = Mathf.Clamp(Mathf.Pow(0.1f, GlobalCoinLerpSpeed * 0.2f), 0.0001f, 0.99f);
        float t = 1f - Mathf.Pow(decay, Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetWorldPos, t);

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, GlobalCoinLerpSpeed * 0.8f * Time.deltaTime);

        if (((Vector2)transform.position - (Vector2)targetWorldPos).sqrMagnitude <= 0.12f * 0.12f)
        {
            
            if (gameManager != null)
            {
                gameManager.AddGold(goldAmount);
            }
            Destroy(gameObject);
        }
    }
}