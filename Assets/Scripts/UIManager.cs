using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    #region Injected Dependencies
    [Header("Injected Managers")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BuildPlacementController buildController;
    private Camera mainCam;
    #endregion

    #region UI Panel References
    [Header("Screen Panels")]
    [SerializeField] private GameObject titleScreenPanel;
    [SerializeField] private GameObject gameplayHUDPanel;
    [SerializeField] private GameObject pauseScreenPanel;
    [SerializeField] private GameObject winScreenPanel;
    [SerializeField] private GameObject gameOverScreenPanel;
    #endregion

    #region HUD Element References
    [Header("Top HUD Elements")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Image hpMainBarFill;
    [SerializeField] private Image hpGhostBarFill;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private RectTransform goldHUDTargetRect;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text expLevelText;
    [SerializeField] private Image expBarFill;

    [Header("Bottom HUD Elements")]
    [SerializeField] private Button[] buildButtons = new Button[3];
    [SerializeField] private TMP_Text[] buildButtonLabels = new TMP_Text[3];
    [SerializeField] private TMP_Text overclockStatusText;
    #endregion

    #region State Variables
    private float displayedGold;
    private float displayedXPFill = 0f;
    private float displayedGhostHPFill = 1f;
    #endregion

    #region Initialization
    private void Awake()
    {
        mainCam = Camera.main;
    }

    public void ResetHUDDisplays(int startingGold)
    {
        displayedGold = startingGold;
        displayedGhostHPFill = 1f;
        displayedXPFill = 0f;
    }

    public void ResetXPBarDisplayOnLevelUp() => displayedXPFill = 0f;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        if (gameManager == null || !gameManager.IsGameActive || gameManager.IsPaused) return;

        UpdateTopHUDAnimations();
        UpdateBottomHUDStatus();
    }
    #endregion

    #region HUD Update Logic
    private void UpdateTopHUDAnimations()
    {
        GameConfigSO cfg = gameManager.config;

        // 1. Base HP & Ghost Bar (Mathf.InverseLerp + Mathf.Lerp) // hp & ghostbar 
        float targetHPFill = Mathf.InverseLerp(0f, gameManager.MaxBaseHP, gameManager.CurrentBaseHP);
        if (hpMainBarFill != null) hpMainBarFill.fillAmount = targetHPFill;

        displayedGhostHPFill = Mathf.Lerp(displayedGhostHPFill, targetHPFill, cfg.ghostBarLerpSpeed * Time.deltaTime);
        if (hpGhostBarFill != null) hpGhostBarFill.fillAmount = displayedGhostHPFill;
        if (hpText != null) hpText.text = $"HP: {Mathf.CeilToInt(gameManager.CurrentBaseHP)}/{gameManager.MaxBaseHP}";

        // 2. Gold Counter
        displayedGold = Mathf.Lerp(displayedGold, gameManager.CurrentGold, cfg.goldTextLerpSpeed * Time.deltaTime);
        if (goldText != null) goldText.text = $"Gold: {Mathf.RoundToInt(displayedGold)}g";

        // 3. EXP Bar
        float targetXPFill = Mathf.InverseLerp(0f, gameManager.XPToNextLevel, gameManager.CurrentXP);
        displayedXPFill = Mathf.Lerp(displayedXPFill, targetXPFill, cfg.expBarLerpSpeed * Time.deltaTime);
        if (expBarFill != null) expBarFill.fillAmount = displayedXPFill;
        if (expLevelText != null) expLevelText.text = $"LVL {gameManager.CurrentLevel}";

        // 4. Wave Counter
        int totalWaves = cfg.waves != null ? cfg.waves.Length : 10;
        if (waveText != null) waveText.text = $"Wave: {Mathf.Min(gameManager.CurrentWaveIndex + 1, totalWaves)}/{totalWaves}";
    }

    private void UpdateBottomHUDStatus()
    {
        // Guard Clause
        if (buildController == null) return;

        // Update Build Buttons & Sabotage Locks
        for (int i = 0; i < 3; i++)
        {
            if (i >= buildButtonLabels.Length || buildButtonLabels[i] == null) continue;
            
            var opt = buildController.towerOptions[i];
            float lockTimer = buildController.buildButtonDisableTimers[i];

            if (lockTimer > 0f)
            {
                buildButtonLabels[i].text = $"LOCKED ({lockTimer:F1}s)";
                if (buildButtons[i] != null) buildButtons[i].interactable = false;
            }
            else
            {
                buildButtonLabels[i].text = $"[{i + 1}] {opt.towerName} ({opt.level1Cost}g)";
                if (buildButtons[i] != null) buildButtons[i].interactable = (gameManager.CurrentGold >= opt.level1Cost);
            }
        }

        // Update Overclock Status
        if (overclockStatusText != null)
        {
            TowerController oc = TowerController.CurrentlyOverclockedTower;
            if (oc != null)
            {
                int upgCost = oc.GetNextUpgradeCost();
                string upgStr = upgCost > 0 ? $"[U] Upgrade ({upgCost}g)" : "MAX LVL";
                overclockStatusText.text = $"OVERCLOCKED (Lvl {oc.level}) | {upgStr} | [X] Sell (+{oc.GetSellRefundGold()}g) | [R / Right-Click] Auto";
            }
            else
            {
                overclockStatusText.text = "Left-Click any placed turret to OVERCLOCK (+25% Fire Rate, 2x Saboteur Dmg, Hits Assassins)";
            }
        }
    }
    #endregion

    #region World Space Helpers
    public Vector3 GetGoldHUDWorldPos()
    {
        if (goldHUDTargetRect != null && mainCam != null)
        {
            Vector3 world = mainCam.ScreenToWorldPoint(goldHUDTargetRect.position);
            world.z = 0f;
            return world;
        }
        return new Vector3(0f, 5.2f, 0f);
    }

    public Vector3 GetBuildButtonWorldPos(int index)
    {
        if (index >= 0 && index < buildButtons.Length && buildButtons[index] != null && mainCam != null)
        {
            Vector3 world = mainCam.ScreenToWorldPoint(buildButtons[index].transform.position);
            world.z = 0f;
            return world;
        }
        return new Vector3((index - 1) * 2.5f, -5.2f, 0f);
    }
    #endregion

    #region Panel State Management
    public void ShowTitlePanel() => SetActivePanel(titleScreenPanel);
    public void ShowGameplayHUDPanel() => SetActivePanel(gameplayHUDPanel);
    public void ShowWinPanel() => SetActivePanel(winScreenPanel);
    public void ShowGameOverPanel() => SetActivePanel(gameOverScreenPanel);
    
    public void SetPausePanelActive(bool paused)
    {
        if (pauseScreenPanel != null) pauseScreenPanel.SetActive(paused);
    }

    private void SetActivePanel(GameObject activePanel)
    {
        if (titleScreenPanel != null)    titleScreenPanel.SetActive(activePanel == titleScreenPanel);
        if (gameplayHUDPanel != null)    gameplayHUDPanel.SetActive(activePanel == gameplayHUDPanel || activePanel == pauseScreenPanel);
        if (pauseScreenPanel != null)    pauseScreenPanel.SetActive(activePanel == pauseScreenPanel);
        if (winScreenPanel != null)      winScreenPanel.SetActive(activePanel == winScreenPanel);
        if (gameOverScreenPanel != null) gameOverScreenPanel.SetActive(activePanel == gameOverScreenPanel);
    }
    #endregion
}