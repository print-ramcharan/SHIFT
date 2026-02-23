using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Computes the daily puzzle seed using the formula from the GDD:
///   seed = SHA256("SHIFT_" + YYYYMMDD_UTC) → first 4 bytes → abs(int)
///
/// Also provides a stub for fetching the seed from Firebase (Phase 4).
/// Attach to any persistent GameObject (e.g. GameManager GO).
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

        Debug.Log($"[DailySeed] Date: {CurrentDateUTC} | Seed: {CurrentSeed}");

        // TODO Phase 4: replace with Firebase fetch to override local seed
        // StartCoroutine(FetchSeedFromFirebase(CurrentDateUTC));

        OnSeedReady?.Invoke(CurrentSeed);
    }

    // ─── Seed Formula ────────────────────────────────────────────────────────────

    /// <summary>
    /// GDD formula: SHA256("SHIFT_" + YYYYMMDD_UTC) → first 4 bytes → abs(int)
    /// Same seed = same room, on any device, on the same UTC day.
    /// </summary>
    public static int ComputeLocalSeed(string dateUTC)
    {
        string input      = Constants.SEED_PREFIX + dateUTC;
        byte[] hashBytes  = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(input));

        // Take first 4 bytes and convert to int, force positive with Math.Abs
        int rawSeed = BitConverter.ToInt32(hashBytes, 0);
        return Math.Abs(rawSeed);
    }

    /// <summary>
    /// Convenience: compute seed for a specific date string (YYYYMMDD).
    /// Used in editor tools and testing.
    /// </summary>
    public static int ComputeSeedForDate(string yyyymmdd) => ComputeLocalSeed(yyyymmdd);

    // ─── Firebase Stub (Phase 4) ──────────────────────────────────────────────────
    // Uncomment and implement in Phase 4 when Firebase is integrated.
    //
    // private async System.Threading.Tasks.Task FetchSeedFromFirebase(string dateUTC)
    // {
    //     try
    //     {
    //         var doc = await FirebaseManager.Instance.GetDailySeedDocument(dateUTC);
    //         if (doc.Exists)
    //         {
    //             int firebaseSeed = int.Parse(doc.GetValue<string>("seed"));
    //             CurrentSeed = firebaseSeed;
    //             Debug.Log($"[DailySeed] Firebase seed: {firebaseSeed}");
    //             OnSeedReady?.Invoke(CurrentSeed);
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.LogWarning($"[DailySeed] Firebase fetch failed, using local seed. {e.Message}");
    //     }
    // }
}
