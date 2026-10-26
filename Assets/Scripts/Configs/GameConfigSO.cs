using UnityEngine;

[System.Serializable]
public struct WaveConfig
{
    public int grunts;
    public int saboteurs;
    public int assassins;
    public float spawnInterval;
}

[CreateAssetMenu(fileName = "GameConfig", menuName = "Overclock/Game Config")]
public class GameConfigSO : ScriptableObject
{
    [Header("Base Settings")]
    public float maxBaseHP = 1000f;
    public int startingGold = 300;

    [Header("UI Lerp Speeds")]
    public float ghostBarLerpSpeed = 5f;
    public float goldTextLerpSpeed = 10f;
    public float coinFlyLerpSpeed = 6.5f;

    [Header("Wave Settings")]
    public float wavePrepTime = 10f;

    public WaveConfig[] waves = new WaveConfig[10]
    {
        new WaveConfig { grunts = 10, saboteurs = 0, assassins = 0, spawnInterval = 1.5f },
        new WaveConfig { grunts = 15, saboteurs = 2, assassins = 0, spawnInterval = 1.2f },
        new WaveConfig { grunts = 20, saboteurs = 4, assassins = 0, spawnInterval = 1.0f },
        new WaveConfig { grunts = 25, saboteurs = 6, assassins = 1, spawnInterval = 0.9f },
        new WaveConfig { grunts = 30, saboteurs = 8, assassins = 2, spawnInterval = 0.8f },
        new WaveConfig { grunts = 35, saboteurs = 10, assassins = 4, spawnInterval = 0.7f },
        new WaveConfig { grunts = 40, saboteurs = 15, assassins = 6, spawnInterval = 0.6f },
        new WaveConfig { grunts = 50, saboteurs = 20, assassins = 8, spawnInterval = 0.5f },
        new WaveConfig { grunts = 60, saboteurs = 25, assassins = 10, spawnInterval = 0.4f },
        new WaveConfig { grunts = 80, saboteurs = 35, assassins = 15, spawnInterval = 0.3f }
    };
}