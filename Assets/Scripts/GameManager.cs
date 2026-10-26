using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region Dependencies
    [Header("Injected Managers")]
    [SerializeField] private WaveSpawner waveSpawner;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioShakeManager audioManager;
    #endregion

    #region Data & Configuration
    [Header("Data Asset (Auto-Creates Default if Empty)")]
    public GameConfigSO config;
    #endregion

    #region Public Properties (Read-Only State)
    public float MaxBaseHP => config.maxBaseHP;
    public float CurrentBaseHP { get; private set; }
    public int CurrentGold { get; private set; }
    public bool IsPrepPhase => waveSpawner != null && waveSpawner.IsPrepPhase;
    public float PrepTimer => waveSpawner != null ? waveSpawner.CurrentPrepTimer : 0f;
    
    public bool IsGameActive { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;
    public int CurrentWaveIndex => waveSpawner != null ? waveSpawner.CurrentWaveIndex : 0;
    #endregion

    #region Initialization
    private void Awake()
    {
        if (config == null) 
        {
            config = ScriptableObject.CreateInstance<GameConfigSO>();
        }
    }

    private void Start()
    {
        ShowTitleScreen();
    }
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        CoinPickupEffect.GlobalCoinLerpSpeed = config.coinFlyLerpSpeed;

        if (IsGameActive && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    #endregion

    #region Economy & Progression
    public void AddGold(int amount)
    {
        CurrentGold += amount;
        if (audioManager != null) audioManager.PlayUIClick(0.4f);
    }

    public bool SpendGold(int amount)
    {
        if (CurrentGold < amount) return false;
        
        CurrentGold -= amount;
        if (audioManager != null) audioManager.PlayUIClick(0.6f);
        
        return true;
    }

    public void DamageBase(int dmg)
    {
        if (!IsGameActive) return;

        CurrentBaseHP = Mathf.Max(0f, CurrentBaseHP - dmg);
        
        if (audioManager != null)
        {
            audioManager.TriggerScreenShake(0.25f, 0.22f);
            audioManager.PlayBaseDamage();
        }

        if (CurrentBaseHP <= 0f)
        {
            TriggerGameOver();
        }
    }
    #endregion

    #region Game Flow Transitions (Hooked to UI Buttons)
    public void ShowTitleScreen()
    {
        Time.timeScale = 0f;
        IsGameActive = false;
        if (uiManager != null) uiManager.ShowTitlePanel();
    }

    public void StartGameButton()
    {
        if (audioManager != null) audioManager.PlayUIClick();
        
        ClearAllEntities();

        CurrentBaseHP = config.maxBaseHP;
        CurrentGold = config.startingGold;

        IsGameActive = true;
        IsPaused = false;
        Time.timeScale = 1f;

        if (uiManager != null)
        {
            uiManager.ResetHUDDisplays(config.startingGold);
            uiManager.ShowGameplayHUDPanel();
        }
        
        if (waveSpawner != null) waveSpawner.BeginWaves();
    }

    public void TogglePause()
    {
        if (audioManager != null) audioManager.PlayUIClick();
        
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        
        if (uiManager != null) uiManager.SetPausePanelActive(IsPaused);
    }

    public void RestartGameButton()
    {
        if (audioManager != null) audioManager.PlayUIClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToTitleButton()
    {
        if (audioManager != null) audioManager.PlayUIClick();
        ClearAllEntities();
        ShowTitleScreen();
    }

    public void TriggerWinGame()
    {
        IsGameActive = false;
        Time.timeScale = 0f;
        
        if (audioManager != null) audioManager.PlayWin();
        if (uiManager != null) uiManager.ShowWinPanel();
    }

    private void TriggerGameOver()
    {
        IsGameActive = false;
        Time.timeScale = 0f;
        
        if (audioManager != null) audioManager.PlayLose();
        if (uiManager != null) uiManager.ShowGameOverPanel();
    }

    public void OpenSettingsButton()
    {
        if (audioManager != null) audioManager.PlayUIClick();
        if (uiManager != null) uiManager.ShowSettingsPanel();
    }

    public void CloseSettingsButton()
    {
        if (audioManager != null) audioManager.PlayUIClick();
        if (uiManager != null) uiManager.HideSettingsPanel();
    }

    #endregion

    #region Utility
    private void ClearAllEntities()
    {
        if (waveSpawner != null) waveSpawner.StopWaves();
        
        foreach (var e in EnemyController.ActiveEnemies.ToArray())
        {
            if (e != null) Destroy(e.gameObject);
        }
            
        foreach (var t in TowerController.ActiveTowers.ToArray())
        {
            if (t != null) Destroy(t.gameObject);
        }
    }

    public Vector3 GetGoldHUDWorldPos() => uiManager != null ? uiManager.GetGoldHUDWorldPos() : new Vector3(0f, 5.2f, 0f);
    public Vector3 GetBuildButtonWorldPos(int index) => uiManager != null ? uiManager.GetBuildButtonWorldPos(index) : new Vector3((index - 1) * 2.5f, -5.2f, 0f);
    #endregion
}