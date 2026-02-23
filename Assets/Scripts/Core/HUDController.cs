using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// In-game HUD: displays the live timer, a pause button, and a hint counter.
/// Listens to GameManager events to update its state automatically.
///
/// Requires TextMeshPro. Assign all UI references in the Inspector.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Timer")]
    public TextMeshProUGUI timerText;

    [Header("Pause")]
    public Button pauseButton;
    public GameObject pauseMenuPanel;

    [Header("Hints")]
    public TextMeshProUGUI hintCountText;
    public Button hintButton;

    [Header("Settings")]
    [Tooltip("Colour when the timer is under the daily average.")]
    public Color timerFastColor  = new Color(0.2f, 0.9f, 0.4f);

    [Tooltip("Colour when the timer is over 2× the daily average.")]
    public Color timerSlowColor  = new Color(0.9f, 0.3f, 0.2f);

    [Tooltip("Neutral timer colour.")]
    public Color timerNeutralColor = Color.white;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused.AddListener(ShowPauseMenu);
            GameManager.Instance.OnGameResumed.AddListener(HidePauseMenu);
            GameManager.Instance.OnGameWon.AddListener(HideHUD);
        }

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPausePressed);

        if (hintButton != null)
            hintButton.onClick.AddListener(OnHintPressed);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused.RemoveListener(ShowPauseMenu);
            GameManager.Instance.OnGameResumed.RemoveListener(HidePauseMenu);
            GameManager.Instance.OnGameWon.RemoveListener(HideHUD);
        }

        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPausePressed);

        if (hintButton != null)
            hintButton.onClick.RemoveListener(OnHintPressed);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        UpdateTimer();
    }

    // ─── Timer ───────────────────────────────────────────────────────────────────

    private void UpdateTimer()
    {
        if (timerText == null || GameManager.Instance == null) return;

        timerText.text = GameManager.Instance.GetFormattedTime();

        // Simple colour feedback based on elapsed time
        float elapsed = GameManager.Instance.ElapsedTime;
        if (elapsed < 60f)
            timerText.color = timerFastColor;
        else if (elapsed > 180f)
            timerText.color = timerSlowColor;
        else
            timerText.color = timerNeutralColor;
    }

    // ─── Pause ───────────────────────────────────────────────────────────────────

    private void OnPausePressed()
    {
        AudioManager.Instance?.Play(AudioManager.SFX.UIClick);

        if (GameManager.Instance?.CurrentState == GameManager.GameState.Playing)
            GameManager.Instance.PauseGame();
        else if (GameManager.Instance?.CurrentState == GameManager.GameState.Paused)
            GameManager.Instance.ResumeGame();
    }

    public void ShowPauseMenu() { if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true); }
    public void HidePauseMenu() { if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false); }

    // ─── Hints ───────────────────────────────────────────────────────────────────

    private int _hintsUsed = 0;
    private const int MAX_HINTS = 3;

    private void OnHintPressed()
    {
        if (_hintsUsed >= MAX_HINTS)
        {
            Debug.Log("[HUD] No hints remaining.");
            return;
        }
        _hintsUsed++;
        AudioManager.Instance?.Play(AudioManager.SFX.UIClick);
        UpdateHintCount();
        // TODO Phase 5: animate hint highlight on GoalZone
    }

    private void UpdateHintCount()
    {
        if (hintCountText != null)
            hintCountText.text = $"{MAX_HINTS - _hintsUsed}";
    }

    // ─── Show / Hide ─────────────────────────────────────────────────────────────

    private void HideHUD() => gameObject.SetActive(false);
}
