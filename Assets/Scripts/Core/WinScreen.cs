using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Win Screen controller. Shown when the player solves the daily puzzle.
/// Displays time, share card button, and next-day countdown.
///
/// Assign all UI references in the Inspector.
/// Wire GameManager.OnGameWon → WinScreen.Show() in the Inspector or via code.
/// </summary>
public class WinScreen : MonoBehaviour
{
    [Header("Panel")]
    public GameObject winPanel;

    [Header("Results")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI headlineText;
    public TextMeshProUGUI shardGrantText;

    [Header("Buttons")]
    public Button shareButton;
    public Button playAgainButton;   // Returns to Main Menu
    public Button leaderboardButton;

    [Header("Particles")]
    public ParticleSystem confettiParticles;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (winPanel != null) winPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.Instance?.OnGameWon.AddListener(Show);

        if (shareButton      != null) shareButton.onClick.AddListener(OnSharePressed);
        if (playAgainButton  != null) playAgainButton.onClick.AddListener(OnPlayAgainPressed);
        if (leaderboardButton!= null) leaderboardButton.onClick.AddListener(OnLeaderboardPressed);
    }

    private void OnDisable()
    {
        GameManager.Instance?.OnGameWon.RemoveListener(Show);

        if (shareButton      != null) shareButton.onClick.RemoveListener(OnSharePressed);
        if (playAgainButton  != null) playAgainButton.onClick.RemoveListener(OnPlayAgainPressed);
        if (leaderboardButton!= null) leaderboardButton.onClick.RemoveListener(OnLeaderboardPressed);
    }

    // ─── Show ────────────────────────────────────────────────────────────────────

    public void Show()
    {
        if (winPanel != null) winPanel.SetActive(true);

        AudioManager.Instance?.Play(AudioManager.SFX.Win);
        confettiParticles?.Play();

        PopulateResults();
    }

    private void PopulateResults()
    {
        if (GameManager.Instance == null) return;

        float elapsed = GameManager.Instance.ElapsedTime;

        if (timeText != null)
            timeText.text = GameManager.Instance.GetFormattedTime();

        if (headlineText != null)
            headlineText.text = GetHeadline(elapsed);

        // Grant shards — Phase 6 will do this properly via RewardSystem
        int shards = Constants.SHARDS_DAILY_WIN;
        if (shardGrantText != null)
            shardGrantText.text = $"+{shards} Shards";

        // Save to PlayerPrefs (cross-device Firestore in Phase 4)
        int current = PlayerPrefs.GetInt(Constants.PREF_SHARD_COUNT, 0);
        PlayerPrefs.SetInt(Constants.PREF_SHARD_COUNT, current + shards);
        PlayerPrefs.Save();
    }

    private string GetHeadline(float seconds)
    {
        if (seconds < 30f)  return "⚡ Lightning Fast!";
        if (seconds < 60f)  return "🔥 Brilliant!";
        if (seconds < 120f) return "✅ Solved!";
        return "💪 Well Done!";
    }

    // ─── Buttons ─────────────────────────────────────────────────────────────────

    private void OnSharePressed()
    {
        AudioManager.Instance?.Play(AudioManager.SFX.UIClick);

        string shareText = BuildShareText();

        // Copy to clipboard — works on all platforms without plugins
        GUIUtility.systemCopyBuffer = shareText;
        Debug.Log($"[WinScreen] Share text copied to clipboard: {shareText}");

        // TODO: Add NativeShare plugin from Asset Store for true mobile share sheet
        // new NativeShare().SetText(shareText).SetTitle("SHIFT").Share();
    }

    private string BuildShareText()
    {
        string time = GameManager.Instance?.GetFormattedTime() ?? "--:--";
        return $"I solved today's SHIFT puzzle in {time}! 🔷\nCan you beat it? #SHIFTgame";
    }

    private void OnPlayAgainPressed()
    {
        AudioManager.Instance?.Play(AudioManager.SFX.UIBack);
        GameManager.Instance?.ResetGame();
        SceneManager.LoadScene(Constants.SCENE_MAIN_MENU);
    }

    private void OnLeaderboardPressed()
    {
        AudioManager.Instance?.Play(AudioManager.SFX.UIClick);
        // TODO Phase 8: open leaderboard panel
        Debug.Log("[WinScreen] Leaderboard coming in Phase 8.");
    }
}
