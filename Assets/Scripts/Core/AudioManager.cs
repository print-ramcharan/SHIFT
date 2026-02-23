using UnityEngine;

/// <summary>
/// Central audio manager for SHIFT. Singleton.
/// Call AudioManager.Instance.Play(SFX.PickUp) from anywhere.
/// Sounds are assigned in the Inspector via the AudioClip array.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─── SFX Enum ────────────────────────────────────────────────────────────────
    public enum SFX
    {
        PickUp,
        Drop,
        Scale,
        GoalEnter,
        Win,
        ChestOpen,
        UIClick,
        UIBack
    }

    // ─── Inspector ───────────────────────────────────────────────────────────────
    [System.Serializable]
    public struct SFXClip
    {
        public SFX type;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    [Header("SFX Library")]
    public SFXClip[] sfxLibrary;

    [Header("Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.4f;

    // ─── Sources ─────────────────────────────────────────────────────────────────
    private AudioSource _sfxSource;
    private AudioSource _musicSource;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sfxSource   = gameObject.AddComponent<AudioSource>();
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.volume = musicVolume;

        LoadVolumePrefs();
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            _musicSource.clip = backgroundMusic;
            _musicSource.Play();
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────────

    public void Play(SFX sfx)
    {
        foreach (var item in sfxLibrary)
        {
            if (item.type == sfx && item.clip != null)
            {
                _sfxSource.PlayOneShot(item.clip, item.volume);
                return;
            }
        }
        Debug.LogWarning($"[AudioManager] No clip found for SFX: {sfx}");
    }

    public void SetSFXVolume(float volume)
    {
        _sfxSource.volume = volume;
        PlayerPrefs.SetFloat(Constants.PREF_SOUND_VOLUME, volume);
    }

    public void SetMusicVolume(float volume)
    {
        _musicSource.volume = volume;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private void LoadVolumePrefs()
    {
        float savedVolume = PlayerPrefs.GetFloat(Constants.PREF_SOUND_VOLUME, 1f);
        _sfxSource.volume = savedVolume;
    }
}
