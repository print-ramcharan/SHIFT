using System;
using UnityEngine;

/// <summary>
/// Tracks the player's daily puzzle streak.
/// Persists locally via PlayerPrefs, synced to Firestore in Phase 4.
///
/// Rules:
///   • +1 streak if the player played yesterday (UTC)
///   • Streak resets to 1 if they skipped a day
///   • Playing again on the same day does NOT double-count
///
/// Attach to a persistent GameObject and call CheckStreak() after a win.
/// </summary>
public class StreakManager : MonoBehaviour
{
    public static StreakManager Instance { get; private set; }

    // ─── Events ──────────────────────────────────────────────────────────────────
    public static event Action<int> OnStreakUpdated;   // new streak count
    public static event Action<int> OnChestEarned;     // chest tier: 1=bronze 2=silver 3=gold

    // ─── State ───────────────────────────────────────────────────────────────────
    public int CurrentStreak { get; private set; }

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadStreak();
    }

    // ─── Public API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this immediately after a win. Updates streak, saves, fires events.
    /// </summary>
    public void CheckAndUpdateStreak()
    {
        string todayUTC = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string lastPlay = PlayerPrefs.GetString(Constants.PREF_STREAK_DATE, "");

        // Already played today — don't double-count
        if (lastPlay == todayUTC)
        {
            Debug.Log($"[Streak] Already played today. Streak: {CurrentStreak}");
            return;
        }

        string yesterdayUTC = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

        if (lastPlay == yesterdayUTC)
        {
            // Consecutive day — extend streak
            CurrentStreak++;
            Debug.Log($"[Streak] Consecutive! New streak: {CurrentStreak}");
        }
        else
        {
            // Skipped one or more days — reset
            CurrentStreak = 1;
            Debug.Log("[Streak] Streak broken — reset to 1.");
        }

        // Save locally
        PlayerPrefs.SetInt(Constants.PREF_LAST_STREAK, CurrentStreak);
        PlayerPrefs.SetString(Constants.PREF_STREAK_DATE, todayUTC);
        PlayerPrefs.Save();

        OnStreakUpdated?.Invoke(CurrentStreak);

        // Check chest milestones
        CheckChestMilestone();

        // Sync to Firestore if Firebase is ready
        SyncToFirestore();
    }

    // ─── Chest Milestones ────────────────────────────────────────────────────────

    private void CheckChestMilestone()
    {
        // Trigger on exact milestone days (NOT every day after)
        if (CurrentStreak == Constants.STREAK_SILVER_CHEST)
        {
            Debug.Log("[Streak] 🎉 Silver Chest earned!");
            OnChestEarned?.Invoke(2);   // tier 2 = silver
        }
        else if (CurrentStreak == Constants.STREAK_GOLD_CHEST)
        {
            Debug.Log("[Streak] 🏆 Gold Chest earned!");
            OnChestEarned?.Invoke(3);   // tier 3 = gold
        }
        else if (CurrentStreak % 3 == 0)
        {
            // Bronze chest every 3 days
            Debug.Log("[Streak] 📦 Bronze Chest earned!");
            OnChestEarned?.Invoke(1);   // tier 1 = bronze
        }
    }

    // ─── Load ────────────────────────────────────────────────────────────────────

    private void LoadStreak()
    {
        CurrentStreak = PlayerPrefs.GetInt(Constants.PREF_LAST_STREAK, 0);
        Debug.Log($"[Streak] Loaded streak: {CurrentStreak}");
    }

    // ─── Firestore Sync ──────────────────────────────────────────────────────────

    private async void SyncToFirestore()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialised) return;

        int shards = PlayerPrefs.GetInt(Constants.PREF_SHARD_COUNT, 0);
        await FirebaseManager.Instance.SaveUserProfileAsync(shards, CurrentStreak);
    }
}
