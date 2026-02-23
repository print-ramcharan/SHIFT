using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// ─── Firebase using directives ─────────────────────────────────────────────────
// These are conditional — they only compile when the Firebase SDK is present.
// Import Firebase Unity SDK from: https://firebase.google.com/docs/unity/setup
#if FIREBASE_ENABLED
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
#endif

/// <summary>
/// Central Firebase manager for SHIFT. Singleton.
///
/// Handles:
///   • Anonymous authentication (no login required on first launch)
///   • Firestore reads/writes (user profile: shards, streak, lastPlayDate)
///   • Daily leaderboard: submit score, fetch top-N
///
/// SETUP:
///   1. Import Firebase Unity SDK (Auth + Firestore packages).
///   2. Place google-services.json (Android) and GoogleService-Info.plist (iOS)
///      in Assets/ root.
///   3. In Player Settings → Scripting Define Symbols add: FIREBASE_ENABLED
///   4. Attach this script to a persistent GameObject.
///
/// Without FIREBASE_ENABLED defined, all methods log warnings and return
/// safe defaults — so the game compiles and runs without Firebase installed.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    // ─── Singleton ──────────────────────────────────────────────────────────────
    public static FirebaseManager Instance { get; private set; }

    // ─── State ───────────────────────────────────────────────────────────────────
    public bool IsInitialised { get; private set; }
    public string UserId      { get; private set; }

    // ─── Events ──────────────────────────────────────────────────────────────────
    public static event Action<bool> OnFirebaseReady; // true = success

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitialiseAsync();
    }

    // ─── Initialisation ──────────────────────────────────────────────────────────

    private async Task InitialiseAsync()
    {
#if FIREBASE_ENABLED
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus != DependencyStatus.Available)
            {
                Debug.LogError($"[Firebase] Dependency check failed: {dependencyStatus}");
                OnFirebaseReady?.Invoke(false);
                return;
            }

            await SignInAnonymouslyAsync();
            IsInitialised = true;
            Debug.Log($"[Firebase] Ready. User: {UserId}");
            OnFirebaseReady?.Invoke(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] Init failed: {e.Message}");
            OnFirebaseReady?.Invoke(false);
        }
#else
        Debug.LogWarning("[Firebase] FIREBASE_ENABLED not defined — running in offline mode.");
        UserId = "offline_" + SystemInfo.deviceUniqueIdentifier;
        IsInitialised = false;
        OnFirebaseReady?.Invoke(false);
        await Task.CompletedTask;
#endif
    }

    // ─── Auth ────────────────────────────────────────────────────────────────────

    private async Task SignInAnonymouslyAsync()
    {
#if FIREBASE_ENABLED
        var auth = FirebaseAuth.DefaultInstance;

        // Re-use existing session if available
        if (auth.CurrentUser != null)
        {
            UserId = auth.CurrentUser.UserId;
            return;
        }

        var result = await auth.SignInAnonymouslyAsync();
        UserId = result.User.UserId;
        Debug.Log($"[Firebase] Signed in anonymously: {UserId}");
#else
        await Task.CompletedTask;
#endif
    }

    // ─── User Profile ────────────────────────────────────────────────────────────

    /// <summary>Writes shard count + streak to Firestore.</summary>
    public async Task SaveUserProfileAsync(int shards, int streak)
    {
#if FIREBASE_ENABLED
        if (!IsInitialised) return;
        try
        {
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection(Constants.FS_COLLECTION_USERS).Document(UserId);
            var data = new Dictionary<string, object>
            {
                { Constants.FS_FIELD_SHARD_COUNT, shards },
                { Constants.FS_FIELD_STREAK,      streak },
                { Constants.FS_FIELD_LAST_PLAY,   DateTime.UtcNow.ToString("yyyy-MM-dd") }
            };
            await doc.SetAsync(data, SetOptions.MergeAll);
            Debug.Log($"[Firebase] Profile saved — Shards: {shards}, Streak: {streak}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] SaveUserProfile failed: {e.Message}");
        }
#else
        Debug.LogWarning("[Firebase] Offline — profile not saved to Firestore.");
        await Task.CompletedTask;
#endif
    }

    /// <summary>Reads user profile from Firestore. Returns (shards, streak) or (-1,-1) on failure.</summary>
    public async Task<(int shards, int streak)> LoadUserProfileAsync()
    {
#if FIREBASE_ENABLED
        if (!IsInitialised) return (-1, -1);
        try
        {
            var db       = FirebaseFirestore.DefaultInstance;
            var snapshot = await db.Collection(Constants.FS_COLLECTION_USERS).Document(UserId).GetSnapshotAsync();
            if (!snapshot.Exists) return (0, 0);

            int shards = snapshot.TryGetValue<int>(Constants.FS_FIELD_SHARD_COUNT, out var s) ? s : 0;
            int streak = snapshot.TryGetValue<int>(Constants.FS_FIELD_STREAK,      out var st) ? st : 0;
            return (shards, streak);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] LoadUserProfile failed: {e.Message}");
            return (-1, -1);
        }
#else
        await Task.CompletedTask;
        return (-1, -1);
#endif
    }

    // ─── Daily Seed ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches (or writes) the daily seed document from Firestore.
    /// Falls back to the local SHA256 seed if unavailable.
    /// </summary>
    public async Task<int> GetOrCreateDailySeedAsync(string dateUTC)
    {
#if FIREBASE_ENABLED
        if (!IsInitialised) return DailySeed.ComputeLocalSeed(dateUTC);
        try
        {
            var db  = FirebaseFirestore.DefaultInstance;
            var doc = db.Collection(Constants.FS_COLLECTION_DAILY_SEED).Document(dateUTC);
            var snap = await doc.GetSnapshotAsync();

            if (snap.Exists && snap.TryGetValue<int>("seed", out int remoteSeed))
            {
                Debug.Log($"[Firebase] Remote seed for {dateUTC}: {remoteSeed}");
                return remoteSeed;
            }

            // Seed doesn't exist yet — compute locally and write it
            int localSeed = DailySeed.ComputeLocalSeed(dateUTC);
            await doc.SetAsync(new Dictionary<string, object> { { "seed", localSeed } });
            Debug.Log($"[Firebase] Wrote new seed for {dateUTC}: {localSeed}");
            return localSeed;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Firebase] Seed fetch failed, using local. {e.Message}");
            return DailySeed.ComputeLocalSeed(dateUTC);
        }
#else
        await Task.CompletedTask;
        return DailySeed.ComputeLocalSeed(dateUTC);
#endif
    }

    // ─── Leaderboard ─────────────────────────────────────────────────────────────

    [Serializable]
    public class LeaderboardEntry
    {
        public string userId;
        public string displayName;
        public float  completionTime;  // seconds
        public string dateUTC;
    }

    /// <summary>Submits the player's time to today's leaderboard.</summary>
    public async Task SubmitScoreAsync(float completionTimeSeconds)
    {
#if FIREBASE_ENABLED
        if (!IsInitialised) return;
        try
        {
            string dateUTC = DateTime.UtcNow.ToString(Constants.SEED_DATE_FORMAT);
            var db  = FirebaseFirestore.DefaultInstance;
            var col = db.Collection(Constants.FS_COLLECTION_LEADERBOARD)
                        .Document(dateUTC)
                        .Collection("entries");

            var entry = new Dictionary<string, object>
            {
                { "userId",         UserId },
                { "displayName",    "Player" },   // Phase 5: pull from profile
                { "completionTime", completionTimeSeconds },
                { "submittedAt",    FieldValue.ServerTimestamp }
            };

            // Use SetAsync with UserId as doc ID so each player has one entry per day
            await col.Document(UserId).SetAsync(entry, SetOptions.MergeAll);
            Debug.Log($"[Firebase] Score submitted: {completionTimeSeconds:F1}s");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] SubmitScore failed: {e.Message}");
        }
#else
        Debug.LogWarning("[Firebase] Offline — score not submitted.");
        await Task.CompletedTask;
#endif
    }

    /// <summary>Returns the top N entries for today's leaderboard, ordered by time ascending.</summary>
    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int topN = 50)
    {
        var results = new List<LeaderboardEntry>();
#if FIREBASE_ENABLED
        if (!IsInitialised) return results;
        try
        {
            string dateUTC = DateTime.UtcNow.ToString(Constants.SEED_DATE_FORMAT);
            var db    = FirebaseFirestore.DefaultInstance;
            var query = db.Collection(Constants.FS_COLLECTION_LEADERBOARD)
                          .Document(dateUTC)
                          .Collection("entries")
                          .OrderBy("completionTime")
                          .Limit(topN);

            var snap = await query.GetSnapshotAsync();
            foreach (var doc in snap.Documents)
            {
                results.Add(new LeaderboardEntry
                {
                    userId         = doc.TryGetValue<string>("userId",         out var uid)   ? uid   : "",
                    displayName    = doc.TryGetValue<string>("displayName",    out var dn)    ? dn    : "Player",
                    completionTime = doc.TryGetValue<float>("completionTime",  out var ct)    ? ct    : 0f,
                    dateUTC        = dateUTC
                });
            }
            Debug.Log($"[Firebase] Leaderboard fetched — {results.Count} entries.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] GetLeaderboard failed: {e.Message}");
        }
#else
        Debug.LogWarning("[Firebase] Offline — returning empty leaderboard.");
        await Task.CompletedTask;
#endif
        return results;
    }
}
