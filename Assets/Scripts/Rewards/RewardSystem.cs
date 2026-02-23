using System;
using UnityEngine;

/// <summary>
/// Central reward system for SHIFT.
/// Grants Shards on win, opens Chests on streak milestones.
///
/// Subscribe to StreakManager.OnChestEarned and GameManager.OnGameWon
/// in the Inspector or via code. Singleton.
/// </summary>
public class RewardSystem : MonoBehaviour
{
    public static RewardSystem Instance { get; private set; }

    // ─── Events ──────────────────────────────────────────────────────────────────
    public static event Action<int> OnShardsGranted;   // total shards after grant
    public static event Action<int, int> OnChestOpened; // (tier, shardsGranted)

    // ─── Inspector ───────────────────────────────────────────────────────────────
    [Header("Chest Animations (assign in Inspector)")]
    public Animator bronzeChestAnimator;
    public Animator silverChestAnimator;
    public Animator goldChestAnimator;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        GameManager.Instance?.OnGameWon.AddListener(OnGameWon);
        StreakManager.OnChestEarned += OpenChest;
    }

    private void OnDisable()
    {
        GameManager.Instance?.OnGameWon.RemoveListener(OnGameWon);
        StreakManager.OnChestEarned -= OpenChest;
    }

    // ─── Win Reward ──────────────────────────────────────────────────────────────

    private void OnGameWon()
    {
        // Check weekend bonus (Remote Config flag from Phase 4)
        bool weekendBonus = PlayerPrefs.GetInt("weekend_bonus", 0) == 1;
        int shards = Constants.SHARDS_DAILY_WIN;

        if (weekendBonus)
        {
            shards = Mathf.RoundToInt(shards * 1.5f);
            Debug.Log($"[Rewards] Weekend bonus active! Shards ×1.5 → {shards}");
        }

        GrantShards(shards);

        // Update streak AFTER granting base win shards
        StreakManager.Instance?.CheckAndUpdateStreak();

        // Submit completion time to leaderboard
        SubmitScore();
    }

    // ─── Shard Granting ──────────────────────────────────────────────────────────

    public void GrantShards(int amount)
    {
        int current = PlayerPrefs.GetInt(Constants.PREF_SHARD_COUNT, 0);
        int newTotal = current + amount;

        PlayerPrefs.SetInt(Constants.PREF_SHARD_COUNT, newTotal);
        PlayerPrefs.Save();

        Debug.Log($"[Rewards] +{amount} Shards granted. Total: {newTotal}");
        OnShardsGranted?.Invoke(newTotal);
        AudioManager.Instance?.Play(AudioManager.SFX.ChestOpen);
    }

    // ─── Chest Opening ───────────────────────────────────────────────────────────

    public void OpenChest(int tier)
    {
        int shardsGranted;
        Animator anim;

        switch (tier)
        {
            case 3:  // Gold
                shardsGranted = Constants.SHARDS_GOLD_CHEST;
                anim = goldChestAnimator;
                Debug.Log("[Rewards] 🏆 Gold Chest opened!");
                break;
            case 2:  // Silver
                shardsGranted = Constants.SHARDS_SILVER_CHEST;
                anim = silverChestAnimator;
                Debug.Log("[Rewards] 🥈 Silver Chest opened!");
                break;
            default: // Bronze (tier 1)
                shardsGranted = Constants.SHARDS_BRONZE_CHEST;
                anim = bronzeChestAnimator;
                Debug.Log("[Rewards] 📦 Bronze Chest opened!");
                break;
        }

        // Play chest animation
        anim?.SetTrigger("Open");
        AudioManager.Instance?.Play(AudioManager.SFX.ChestOpen);

        GrantShards(shardsGranted);
        OnChestOpened?.Invoke(tier, shardsGranted);
    }

    // ─── Leaderboard Submission ──────────────────────────────────────────────────

    private async void SubmitScore()
    {
        if (GameManager.Instance == null) return;
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialised) return;

        await FirebaseManager.Instance.SubmitScoreAsync(GameManager.Instance.ElapsedTime);
    }
}
