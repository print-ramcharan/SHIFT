using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Computes the daily puzzle seed using the formula from the GDD:
///   seed = SHA256("SHIFT_" + YYYYMMDD_UTC) → first 4 bytes → abs(int)
///
/// Phase 4: also fetches the remote seed from Firebase and re-fires
/// OnSeedReady if the remote value differs (so LevelGenerator rebuilds).
/// Attach to any persistent GameObject (e.g. GameManager object).
/// </summary>
public class DailySeed : MonoBehaviour
{
    // ─── Events ──────────────────────────────────────────────────────────────────
    public static event Action<int> OnSeedReady;

    // ─── State ───────────────────────────────────────────────────────────────────
    public static int CurrentSeed { get; private set; }
    public static string CurrentDateUTC { get; private set; }

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Start()
    {
        InitialiseSeed();
    }

    // ─── Public API ──────────────────────────────────────────────────────────────

    public void InitialiseSeed()
    {
        CurrentDateUTC = DateTime.UtcNow.ToString(Constants.SEED_DATE_FORMAT);
        CurrentSeed    = ComputeLocalSeed(CurrentDateUTC);

        Debug.Log($"[DailySeed] Local seed: {CurrentSeed} for {CurrentDateUTC}");

        // Fire immediately with local seed so LevelGenerator starts without waiting
        OnSeedReady?.Invoke(CurrentSeed);

        // Phase 4: also fetch from Firebase — re-fires if remote seed differs
        FetchSeedFromFirebase(CurrentDateUTC);
    }

    // ─── Firebase Fetch ──────────────────────────────────────────────────────────

    private async void FetchSeedFromFirebase(string dateUTC)
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialised)
        {
            Debug.LogWarning("[DailySeed] Firebase not ready — using local seed.");
            return;
        }

        int remoteSeed = await FirebaseManager.Instance.GetOrCreateDailySeedAsync(dateUTC);

        if (remoteSeed != CurrentSeed)
        {
            Debug.Log($"[DailySeed] Remote seed ({remoteSeed}) differs — regenerating room.");
            CurrentSeed = remoteSeed;
            OnSeedReady?.Invoke(CurrentSeed);
        }
        else
        {
            Debug.Log("[DailySeed] Remote seed matches local — room is correct.");
        }
    }

    // ─── Seed Formula ────────────────────────────────────────────────────────────

    /// <summary>
    /// GDD formula: SHA256("SHIFT_" + YYYYMMDD_UTC) → first 4 bytes → abs(int)
    /// Same seed = same room, on any device, on the same UTC day.
    /// </summary>
    public static int ComputeLocalSeed(string dateUTC)
    {
        string input     = Constants.SEED_PREFIX + dateUTC;
        byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(input));

        int rawSeed = BitConverter.ToInt32(hashBytes, 0);
        return Math.Abs(rawSeed);
    }

    /// <summary>Convenience: compute seed for a specific YYYYMMDD string.</summary>
    public static int ComputeSeedForDate(string yyyymmdd) => ComputeLocalSeed(yyyymmdd);
}
