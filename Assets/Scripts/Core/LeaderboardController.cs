using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fetches and displays today's leaderboard on the WinScreen.
/// Attach to the WinScreen GameObject. Assign the row prefab and scroll container.
///
/// The row prefab needs: [rank TMP] [name TMP] [time TMP].
/// The player's own entry is highlighted in gold.
/// </summary>
public class LeaderboardController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject leaderboardPanel;
    public Transform  rowContainer;    // ScrollView → Viewport → Content
    public GameObject rowPrefab;       // Prefab with rank, name, time labels

    [Header("Loading")]
    public GameObject loadingSpinner;
    public TextMeshProUGUI emptyStateText;

    [Header("Style")]
    public Color ownEntryColor  = new Color(1f, 0.85f, 0.2f);   // gold
    public Color topThreeColor  = new Color(0.9f, 0.9f, 1f);     // silver-white
    public Color defaultColor   = Color.white;

    private bool _isLoading;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    // ─── Public API ──────────────────────────────────────────────────────────────

    /// <summary>Call this from WinScreen's leaderboard button.</summary>
    public async void ShowLeaderboard()
    {
        if (_isLoading) return;

        leaderboardPanel?.SetActive(true);
        SetLoading(true);
        ClearRows();

        List<FirebaseManager.LeaderboardEntry> entries = new List<FirebaseManager.LeaderboardEntry>();

        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsInitialised)
        {
            entries = await FirebaseManager.Instance.GetLeaderboardAsync(topN: 50);
        }
        else
        {
            Debug.LogWarning("[Leaderboard] Firebase not ready — showing empty state.");
        }

        SetLoading(false);

        if (entries.Count == 0)
        {
            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(true);
                emptyStateText.text = "No scores yet — be the first!";
            }
            return;
        }

        if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);

        string myUserId = FirebaseManager.Instance?.UserId ?? "";
        PopulateRows(entries, myUserId);
    }

    public void CloseLeaderboard()
    {
        leaderboardPanel?.SetActive(false);
    }

    // ─── Row Building ────────────────────────────────────────────────────────────

    private void PopulateRows(List<FirebaseManager.LeaderboardEntry> entries, string myUserId)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            int rank  = i + 1;

            GameObject row = Instantiate(rowPrefab, rowContainer);

            // Find child labels by name (set up in the prefab)
            SetLabel(row, "RankText",  FormatRank(rank));
            SetLabel(row, "NameText",  TruncateName(entry.displayName, 18));
            SetLabel(row, "TimeText",  FormatTime(entry.completionTime));

            // Colour highlight
            Color colour = defaultColor;
            if (entry.userId == myUserId)            colour = ownEntryColor;
            else if (rank <= 3)                      colour = topThreeColor;

            ApplyColourToLabels(row, colour);
        }
    }

    private void SetLabel(GameObject row, string childName, string value)
    {
        Transform child = row.transform.Find(childName);
        if (child == null) return;

        var tmp = child.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = value;
    }

    private void ApplyColourToLabels(GameObject row, Color colour)
    {
        foreach (var tmp in row.GetComponentsInChildren<TextMeshProUGUI>())
            tmp.color = colour;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private void ClearRows()
    {
        foreach (Transform child in rowContainer)
            Destroy(child.gameObject);
    }

    private void SetLoading(bool loading)
    {
        _isLoading = loading;
        if (loadingSpinner != null) loadingSpinner.SetActive(loading);
    }

    private string FormatRank(int rank)
    {
        return rank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => $"#{rank}"
        };
    }

    private string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    private string TruncateName(string name, int maxLength)
    {
        if (name.Length <= maxLength) return name;
        return name[..maxLength] + "…";
    }
}
