using UnityEngine;
using System.Collections;

/// <summary>
/// Haptic feedback helper for SHIFT (iOS and Android).
/// Call HapticsManager.Instance.Play(HapticType.Light) from anywhere.
///
/// Respects the player's haptics preference stored in PlayerPrefs.
/// Does nothing on platforms that don't support haptics.
/// </summary>
public class HapticsManager : MonoBehaviour
{
    public static HapticsManager Instance { get; private set; }

    public enum HapticType { Light, Medium, Heavy, Success, Warning, Error }

    private bool _hapticsEnabled;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _hapticsEnabled = PlayerPrefs.GetInt(Constants.PREF_HAPTICS, 1) == 1;
    }

    // ─── Public API ──────────────────────────────────────────────────────────────

    public void Play(HapticType type)
    {
        if (!_hapticsEnabled) return;

#if UNITY_IOS && !UNITY_EDITOR
        PlayiOS(type);
#elif UNITY_ANDROID && !UNITY_EDITOR
        PlayAndroid(type);
#else
        Debug.Log($"[Haptics] {type} (editor — no haptic)");
#endif
    }

    public void SetEnabled(bool enabled)
    {
        _hapticsEnabled = enabled;
        PlayerPrefs.SetInt(Constants.PREF_HAPTICS, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ─── iOS ─────────────────────────────────────────────────────────────────────

    private void PlayiOS(HapticType type)
    {
#if UNITY_IOS && !UNITY_EDITOR
        switch (type)
        {
            case HapticType.Light:
            case HapticType.Medium:
            case HapticType.Heavy:
                Handheld.Vibrate();  // Basic — upgrade to iOS native plugin for full haptics
                break;
            case HapticType.Success:
            case HapticType.Warning:
            case HapticType.Error:
                Handheld.Vibrate();
                break;
        }
#endif
    }

    // ─── Android ─────────────────────────────────────────────────────────────────

    private void PlayAndroid(HapticType type)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        long durationMs = type switch
        {
            HapticType.Light   => 20,
            HapticType.Medium  => 40,
            HapticType.Heavy   => 80,
            HapticType.Success => 30,
            HapticType.Warning => 60,
            HapticType.Error   => 100,
            _                  => 30
        };

        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity   = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject vibrator   = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        vibrator?.Call("vibrate", durationMs);
#endif
    }
}
