using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the Main Menu screen.
/// Handles Play, Settings, and displays current streak + shard count.
///
/// Wire UI buttons in the Inspector.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button archiveButton;

    [Header("Stats Display")]
    public TextMeshProUGUI streakText;
    public TextMeshProUGUI shardCountText;
    public TextMeshProUGUI dailyDateText;

    [Header("Settings Panel")]
    public GameObject settingsPanel;

    [Header("Audio Toggles")]
    public Slider musicSlider;
    public Slider sfxSlider;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Start()
    {
        SetupButtons();
        RefreshStats();
        LoadAudioPrefs();
    }

    // ─── Button Setup ────────────────────────────────────────────────────────────

    private void SetupButtons()
    {
        if (playButton     != null) playButton.onClick.AddListener(OnPlayPressed);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsPressed);
        if (archiveButton  != null) archiveButton.onClick.AddListener(OnArchivePressed);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetMusicVolume(v));

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSFXVolume(v));
    }

    // ─── Stats ───────────────────────────────────────────────────────────────────

    private void RefreshStats()
    {
        // Streak
        int streak = PlayerPrefs.GetInt(Constants.PREF_LAST_STREAK, 0);
        if (streakText != null)
            streakText.text = streak > 0 ? $"🔥 {streak} day streak" : "Start your streak!";

        // Shards
        int shards = PlayerPrefs.GetInt(Constants.PREF_SHARD_COUNT, 0);
        if (shardCountText != null)
            shardCountText.text = $"💎 {shards}";

        // Date
        if (dailyDateText != null)
            dailyDateText.text = System.DateTime.UtcNow.ToString("MMMM dd, yyyy") + " — Daily Puzzle";
    }

    // ─── Audio Prefs ─────────────────────────────────────────────────────────────

    private void LoadAudioPrefs()
    {
        float vol = PlayerPrefs.GetFloat(Constants.PREF_SOUND_VOLUME, 1f);
        if (sfxSlider   != null) sfxSlider.value   = vol;
        if (musicSlider != null) musicSlider.value  = 0.4f;
    }

    // ─── Button Handlers ─────────────────────────────────────────────────────────

    private void OnPlayPressed()
    {
        AudioManager.Instance?.Play(AudioManager.SFX.UIClick);
        GameManager.Instance?.StartGame();
        SceneManager.LoadScene(Constants.SCENE_GAME);
    }

    private void OnSettingsPressed()
    {
        AudioManager.Instance?.Play(AudioManager.SFX.UIClick);
        bool isOpen = settingsPanel != null && settingsPanel.activeSelf;
        settingsPanel?.SetActive(!isOpen);
    }

    private void OnArchivePressed()
    {
        AudioManager.Instance?.Play(AudioManager.SFX.UIClick);
        // TODO Phase 5: open archive puzzle list
        Debug.Log("[MainMenu] Archive coming in Phase 5.");
    }
}
