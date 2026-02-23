using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Runtime helper for URP post-processing in SHIFT.
/// Drives bloom intensity based on object scale (bigger = more glow)
/// and applies vignette pulse on win.
///
/// Requires: a Volume component on the camera with Bloom and Vignette overrides.
/// Assign the Volume in the Inspector.
/// </summary>
public class URPPostProcessHelper : MonoBehaviour
{
    [Header("Volume Reference")]
    public Volume globalVolume;

    [Header("Bloom Settings")]
    [Tooltip("Base bloom intensity when no object is held.")]
    public float bloomBase      = 0.3f;

    [Tooltip("Max bloom intensity when object is at max scale.")]
    public float bloomMax       = 1.2f;

    [Tooltip("How smoothly bloom transitions.")]
    public float bloomSmoothing = 4f;

    [Header("Vignette Settings")]
    public float vignetteIdle    = 0.2f;
    public float vignetteWin     = 0.6f;
    public float vignetteWinTime = 1.5f;   // seconds to hold win vignette

    // ─── Internal ────────────────────────────────────────────────────────────────
    private Bloom    _bloom;
    private Vignette _vignette;
    private float    _targetBloom;
    private float    _vignetteTimer;

    // ─── Lifecycle ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (globalVolume == null)
            globalVolume = FindObjectOfType<Volume>();

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out _bloom);
            globalVolume.profile.TryGet(out _vignette);
        }

        _targetBloom = bloomBase;
    }

    private void OnEnable()
    {
        GameManager.Instance?.OnGameWon.AddListener(OnWin);
        GameManager.Instance?.OnGameReset.AddListener(OnReset);
    }

    private void OnDisable()
    {
        GameManager.Instance?.OnGameWon.RemoveListener(OnWin);
        GameManager.Instance?.OnGameReset.RemoveListener(OnReset);
    }

    private void Update()
    {
        UpdateBloom();
        UpdateVignette();
    }

    // ─── Bloom ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from PerspectivePickup every frame while an object is held.
    /// scaleMultiplier: current scale / base scale of the held object.
    /// </summary>
    public void SetHeldScaleMultiplier(float scaleMultiplier)
    {
        float t = Mathf.InverseLerp(1f, Constants.MAX_SCALE_MULT, scaleMultiplier);
        _targetBloom = Mathf.Lerp(bloomBase, bloomMax, t);
    }

    public void ClearHeldScale() => _targetBloom = bloomBase;

    private void UpdateBloom()
    {
        if (_bloom == null) return;

        _bloom.intensity.value = Mathf.Lerp(
            _bloom.intensity.value,
            _targetBloom,
            Time.deltaTime * bloomSmoothing);
    }

    // ─── Vignette ────────────────────────────────────────────────────────────────

    private void OnWin()
    {
        if (_vignette == null) return;
        _vignette.intensity.value = vignetteWin;
        _vignetteTimer = vignetteWinTime;
    }

    private void OnReset()
    {
        if (_vignette == null) return;
        _vignette.intensity.value = vignetteIdle;
        _vignetteTimer = 0f;
        _targetBloom   = bloomBase;
    }

    private void UpdateVignette()
    {
        if (_vignette == null || _vignetteTimer <= 0f) return;

        _vignetteTimer -= Time.deltaTime;

        if (_vignetteTimer <= 0f)
        {
            // Smoothly ease back to idle vignette
            _vignette.intensity.value = Mathf.Lerp(
                _vignette.intensity.value,
                vignetteIdle,
                Time.deltaTime * 2f);
        }
    }
}
