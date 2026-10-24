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
    [Header("Base & Economy")]
    public float maxBaseHP = 100f;
    public int startingGold = 250;
    public float baseXPToLevel = 100f;
    public float xpPerGoldEarned = 1.2f;

    [Header("UI & Coin Lerp Speeds")]
    public float ghostBarLerpSpeed = 3.5f;
    public float expBarLerpSpeed = 5.0f;
    public float goldTextLerpSpeed = 10.0f;
    public float coinFlyLerpSpeed = 6.5f;

    [Header("10-Wave Spawn Table (Matches TDD)")]
    public WaveConfig[] waves = new WaveConfig[10]
    {
        new WaveConfig { grunts = 6,  saboteurs = 0,  assassins = 0,  spawnInterval = 2.0f }, // Wave 1
        new WaveConfig { grunts = 8,  saboteurs = 2,  assassins = 0,  spawnInterval = 1.8f }, // Wave 2
        new WaveConfig { grunts = 10, saboteurs = 3,  assassins = 1,  spawnInterval = 1.7f }, // Wave 3
        new WaveConfig { grunts = 12, saboteurs = 4,  assassins = 2,  spawnInterval = 1.5f }, // Wave 4
        new WaveConfig { grunts = 14, saboteurs = 6,  assassins = 3,  spawnInterval = 1.4f }, // Wave 5
        new WaveConfig { grunts = 16, saboteurs = 7,  assassins = 4,  spawnInterval = 1.3f }, // Wave 6
        new WaveConfig { grunts = 18, saboteurs = 8,  assassins = 5,  spawnInterval = 1.2f }, // Wave 7
        new WaveConfig { grunts = 20, saboteurs = 10, assassins = 6,  spawnInterval = 1.1f }, // Wave 8
        new WaveConfig { grunts = 24, saboteurs = 12, assassins = 8,  spawnInterval = 1.0f }, // Wave 9
        new WaveConfig { grunts = 30, saboteurs = 15, assassins = 10, spawnInterval = 0.8f }  // Wave 10
    };
}