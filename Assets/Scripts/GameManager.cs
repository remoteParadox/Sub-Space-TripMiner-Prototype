using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Owns the rules of Sub-Space TripMiner: Hull Integrity, Volatile Mode,
/// scoring, HUD text, and the Dimension Lost / Victory screens.
/// PlayerController reports board events here; this script decides what they mean.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Victory,
        GameOver
    }

    [Header("References")]
    [Tooltip("Drag the object that has the PlayerController component here.")]
    public PlayerController playerController;

    [Header("Rules")]
    [Min(1)] public int startingHull = 3;
    [Tooltip("When on, shields are bypassed and a single wrong click ends the game. " +
             "Normally set from the main screen toggle.")]
    public bool isVolatileMode = false;

    [Header("Scoring")]
    public int pointsPerSafeBlock = 10;
    [Tooltip("Victory bonus per remaining hull point (not awarded in Volatile Mode).")]
    public int hullBonusPerPoint = 100;
    [Tooltip("Multiplier applied to block points while in Volatile Mode.")]
    public int volatileScoreMultiplier = 2;

    [Header("UI Panels (all optional)")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject dimensionLostPanel;
    public GameObject victoryPanel;

    [Header("Main Screen")]
    [Tooltip("The 'Volatile Mode' toggle on the main screen.")]
    public Toggle volatileModeToggle;

    [Header("HUD Text (legacy UI Text, all optional)")]
    public Text hullText;
    public Image[] hullSegments;
    public Text statusText;
    public Text scoreText;
    public Text progressText;
    public GameObject mistakeBanner;
    public Text mistakeText;

    [Header("End Screen Text (optional)")]
    public Text dimensionLostScoreText;
    public Text victoryScoreText;

    [Header("Warning Flash")]
    [Tooltip("A full-screen Image, disabled by default. It pulses while shields are at 0.")]
    public Image warningOverlay;
    public Color warningColor = new Color(1f, 0.1f, 0.1f, 1f);
    public float warningFlashSpeed = 2f;
    [Range(0f, 1f)] public float warningMaxAlpha = 0.35f;

    [Header("Events (hook up sound, camera shake, etc.)")]
    public UnityEvent onHullDamaged;
    public UnityEvent onWarningState;
    public UnityEvent onDimensionLost;
    public UnityEvent onVictory;

    private GameState state = GameState.MainMenu;
    private int hullIntegrity;
    private bool isWarningState;
    private int score;
    private int blocksHarvested;
    private int safeBlocksTotal;
    private Color statusBaseColor = Color.white;
    private float mistakeBannerTimer;

    // ------------------------------------------------------------------
    // Read-only state for other scripts
    // ------------------------------------------------------------------

    public GameState State { get { return state; } }
    public bool IsPlaying { get { return state == GameState.Playing; } }
    public bool IsWarningState { get { return isWarningState; } }
    public bool IsVolatileMode { get { return isVolatileMode; } }
    public int HullIntegrity { get { return hullIntegrity; } }
    public int Score { get { return score; } }

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        // Full-screen or HUD graphics must never swallow clicks meant for the grid,
        // because PlayerController ignores clicks while the pointer is over UI.
        if (warningOverlay != null)
        {
            warningOverlay.raycastTarget = false;
            warningOverlay.gameObject.SetActive(false);
        }

        if (hullText != null) hullText.raycastTarget = false;
        if (statusText != null)
        {
            statusText.raycastTarget = false;
            statusBaseColor = statusText.color;
        }
        if (scoreText != null) scoreText.raycastTarget = false;
        if (progressText != null) progressText.raycastTarget = false;
    }

    private void Start()
    {
        if (mainMenuPanel != null)
        {
            ShowMainMenu();
        }
        else
        {
            // No main screen set up: just start playing (handy for quick testing).
            BeginRun();
        }
    }

    private void Update()
    {
        if (mistakeBannerTimer > 0f)
        {
            mistakeBannerTimer -= Time.unscaledDeltaTime;
            if (mistakeBannerTimer <= 0f)
            {
                SetActive(mistakeBanner, false);
            }
        }

        if (isWarningState && state == GameState.Playing)
        {
            AnimateWarning();
        }
    }

    private void OnValidate()
    {
        startingHull = Mathf.Max(1, startingHull);
        pointsPerSafeBlock = Mathf.Max(0, pointsPerSafeBlock);
        hullBonusPerPoint = Mathf.Max(0, hullBonusPerPoint);
        volatileScoreMultiplier = Mathf.Max(1, volatileScoreMultiplier);
        warningFlashSpeed = Mathf.Max(0.1f, warningFlashSpeed);
    }

    // ------------------------------------------------------------------
    // Menu / flow (wire these to UI Buttons)
    // ------------------------------------------------------------------

    /// <summary>Start button on the main screen. Reads the Volatile Mode toggle.</summary>
    public void StartGame()
    {
        if (volatileModeToggle != null)
        {
            isVolatileMode = volatileModeToggle.isOn;
        }

        BeginRun();
    }

    /// <summary>Retry with the same mode and a freshly generated board.</summary>
    public void RestartGame()
    {
        BeginRun();
    }

    /// <summary>Back to the main screen so the mode can be changed.</summary>
    public void ReturnToMainMenu()
    {
        if (mainMenuPanel == null)
        {
            BeginRun();
            return;
        }

        if (playerController != null)
        {
            playerController.ResetGame();
        }

        ShowMainMenu();
    }

    /// <summary>Can be wired to the toggle's On Value Changed; only applies on the main screen.</summary>
    public void SetVolatileMode(bool value)
    {
        if (state != GameState.MainMenu) return;

        isVolatileMode = value;
        UpdateHud();
    }

    private void ShowMainMenu()
    {
        state = GameState.MainMenu;
        ClearWarning();

        SetActive(mainMenuPanel, true);
        SetActive(hudPanel, false);
        SetActive(dimensionLostPanel, false);
        SetActive(victoryPanel, false);

        if (volatileModeToggle != null)
        {
            volatileModeToggle.SetIsOnWithoutNotify(isVolatileMode);
        }

        UpdateHud();
    }

    private void BeginRun()
    {
        hullIntegrity = Mathf.Max(1, startingHull);
        score = 0;
        blocksHarvested = 0;
        safeBlocksTotal = 0;

        ClearWarning();
        mistakeBannerTimer = 0f;
        SetActive(mistakeBanner, false);

        if (playerController != null)
        {
            playerController.ResetGame();
        }

        state = GameState.Playing;

        SetActive(mainMenuPanel, false);
        SetActive(hudPanel, true);
        SetActive(dimensionLostPanel, false);
        SetActive(victoryPanel, false);

        UpdateHud();
    }

    // ------------------------------------------------------------------
    // Calls from PlayerController
    // ------------------------------------------------------------------

    /// <summary>
    /// Called when the player left-clicks a singularity.
    /// Returns true if the hit was fatal (the game is now over),
    /// or false if the shields absorbed it and play continues.
    /// </summary>
    public bool RegisterSingularityHit()
    {
        if (state != GameState.Playing)
        {
            return false;
        }

        // Volatile Mode: shields are bypassed entirely.
        if (isVolatileMode)
        {
            TriggerGameOver();
            return true;
        }

        // Shields still up: lose one point and keep playing.
        if (hullIntegrity > 0)
        {
            hullIntegrity--;
            string feedback = hullIntegrity <= 0
                ? "HULL CRITICAL  •  0 / " + startingHull + "  •  NEXT HIT COLLAPSES REALITY"
                : "SINGULARITY HIT  •  HULL -1  •  " + hullIntegrity + " / " + startingHull + " SHIELDS";
            ShowMistakeFeedback(feedback);
            onHullDamaged.Invoke();

            if (hullIntegrity == 0)
            {
                EnterWarningState();
            }

            UpdateHud();
            return false;
        }

        // Shields were already at 0, so this error collapses reality.
        TriggerGameOver();
        return true;
    }

    /// <summary>Called with the number of safe blocks stabilized by one left-click.</summary>
    public void RegisterBlocksHarvested(int count)
    {
        if (state != GameState.Playing || count <= 0) return;

        blocksHarvested += count;

        int multiplier = isVolatileMode ? volatileScoreMultiplier : 1;
        score += count * pointsPerSafeBlock * multiplier;

        UpdateHud();
    }

    /// <summary>Called once the board is generated so the HUD can show progress.</summary>
    public void SetSafeBlockTotal(int total)
    {
        safeBlocksTotal = Mathf.Max(0, total);
        UpdateHud();
    }

    // ------------------------------------------------------------------
    // End states
    // ------------------------------------------------------------------

    /// <summary>Full reality collapse: shows the 'Dimension Lost' screen.</summary>
    public void TriggerGameOver()
    {
        if (state != GameState.Playing) return;

        state = GameState.GameOver;
        ClearWarning();

        SetActive(hudPanel, false);
        SetActive(dimensionLostPanel, true);

        if (dimensionLostScoreText != null)
        {
            dimensionLostScoreText.text = "FINAL SCORE: " + score;
        }

        onDimensionLost.Invoke();
    }

    /// <summary>All safe blocks harvested: awards the hull bonus and shows the victory screen.</summary>
    public void TriggerVictory()
    {
        if (state != GameState.Playing) return;

        if (!isVolatileMode)
        {
            score += hullIntegrity * hullBonusPerPoint;
        }

        state = GameState.Victory;
        ClearWarning();

        SetActive(hudPanel, false);
        SetActive(victoryPanel, true);

        if (victoryScoreText != null)
        {
            victoryScoreText.text = "FINAL SCORE: " + score;
        }

        onVictory.Invoke();
    }

    // ------------------------------------------------------------------
    // Warning state
    // ------------------------------------------------------------------

    private void EnterWarningState()
    {
        isWarningState = true;

        if (warningOverlay != null)
        {
            warningOverlay.gameObject.SetActive(true);
        }

        onWarningState.Invoke();
    }

    private void ClearWarning()
    {
        isWarningState = false;

        if (warningOverlay != null)
        {
            warningOverlay.gameObject.SetActive(false);
        }

        if (statusText != null)
        {
            statusText.color = statusBaseColor;
        }
    }

    private void AnimateWarning()
    {
        float pulse = Mathf.PingPong(Time.unscaledTime * warningFlashSpeed, 1f);

        if (warningOverlay != null)
        {
            Color c = warningColor;
            c.a = pulse * warningMaxAlpha;
            warningOverlay.color = c;
        }

        if (statusText != null)
        {
            statusText.color = Color.Lerp(statusBaseColor, warningColor, pulse);
        }
    }

    // ------------------------------------------------------------------
    // HUD
    // ------------------------------------------------------------------

    private void ShowMistakeFeedback(string message)
    {
        if (mistakeText != null)
        {
            mistakeText.text = message;
        }
        SetActive(mistakeBanner, true);
        mistakeBannerTimer = 2.6f;
    }

    private void UpdateHullSegments()
    {
        if (hullSegments == null) return;
        Color activeColor = hullIntegrity <= 1 ? new Color(1f, 0.25f, 0.20f, 1f) : new Color(0.16f, 0.88f, 1f, 1f);
        Color depletedColor = new Color(0.15f, 0.22f, 0.28f, 1f);
        for (int i = 0; i < hullSegments.Length; i++)
        {
            if (hullSegments[i] == null) continue;
            bool active = !isVolatileMode && i < hullIntegrity;
            hullSegments[i].color = active ? activeColor : depletedColor;
        }
    }

    private void UpdateHud()
    {
        if (hullText != null)
        {
            hullText.text = isVolatileMode
                ? "HULL: OFFLINE (VOLATILE MODE)"
                : "HULL  " + hullIntegrity + " / " + startingHull;
        }

        UpdateHullSegments();
        if (statusText != null)
        {
            string status = "";

            if (state == GameState.Playing)
            {
                if (isVolatileMode)
                {
                    status = "VOLATILE MODE: ONE ERROR COLLAPSES THE DIMENSION";
                }
                else if (isWarningState)
                {
                    status = "WARNING: SHIELDS DOWN - NEXT ERROR IS FATAL";
                }
                else
                {
                    status = "SUB-SPACE STABLE";
                }
            }

            statusText.text = status;
        }

        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + score;
        }

        if (progressText != null)
        {
            progressText.text = safeBlocksTotal > 0
                ? "HARVESTED: " + blocksHarvested + " / " + safeBlocksTotal
                : "HARVESTED: " + blocksHarvested;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
